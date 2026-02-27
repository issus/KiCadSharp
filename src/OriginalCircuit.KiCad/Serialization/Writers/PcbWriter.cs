using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Writes KiCad PCB files (<c>.kicad_pcb</c>) from <see cref="KiCadPcb"/> objects.
/// </summary>
public static class PcbWriter
{
    /// <summary>
    /// Writes a PCB document to a file path.
    /// </summary>
    /// <param name="pcb">The PCB document to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcb pcb, string path, CancellationToken ct = default)
    {
        var expr = pcb.SourceTree ?? Build(pcb);
        await SExpressionWriter.WriteAsync(expr, path, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PCB document to a stream.
    /// </summary>
    /// <param name="pcb">The PCB document to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcb pcb, Stream stream, CancellationToken ct = default)
    {
        var expr = pcb.SourceTree ?? Build(pcb);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadPcb pcb)
    {
        var b = new SExpressionBuilder("kicad_pcb")
            .AddChild("version", v => v.AddValue(pcb.Version == 0 ? 20231120 : pcb.Version))
            .AddChild("generator", g => g.AddValue(pcb.Generator ?? "kicadsharp"));

        if (pcb.GeneratorVersion is not null)
            b.AddChild("generator_version", g => g.AddValue(pcb.GeneratorVersion));

        // General (prefer raw subtree for round-trip, fallback to constructed)
        if (pcb.GeneralRaw is not null)
            b.AddChild(pcb.GeneralRaw);
        else
            b.AddChild("general", gen =>
            {
                gen.AddChild("thickness", t => t.AddMm(pcb.BoardThickness));
            });

        // Paper (raw)
        if (pcb.PaperRaw is not null)
            b.AddChild(pcb.PaperRaw);

        // Title block (raw)
        if (pcb.TitleBlockRaw is not null)
            b.AddChild(pcb.TitleBlockRaw);

        // Layers (raw)
        if (pcb.LayersRaw is not null)
            b.AddChild(pcb.LayersRaw);

        // Setup (raw)
        if (pcb.SetupRaw is not null)
            b.AddChild(pcb.SetupRaw);

        // Nets
        foreach (var (num, name) in pcb.Nets)
        {
            b.AddChild("net", n =>
            {
                n.AddValue(num);
                n.AddValue(name);
            });
        }

        // Net classes
        foreach (var nc in pcb.NetClasses)
        {
            b.AddChild(BuildNetClass(nc));
        }

        // Footprints
        foreach (var comp in pcb.Components.OfType<KiCadPcbComponent>())
        {
            b.AddChild(FootprintWriter.BuildFootprint(comp));
        }

        // Board-level elements — use original order if available
        if (pcb.BoardElementOrder.Count > 0)
        {
            foreach (var element in pcb.BoardElementOrder)
            {
                switch (element)
                {
                    case KiCadPcbGraphicLine line: b.AddChild(BuildGrLine(line)); break;
                    case KiCadPcbGraphicArc arc: b.AddChild(BuildGrArc(arc)); break;
                    case KiCadPcbGraphicCircle circle: b.AddChild(BuildGrCircle(circle)); break;
                    case KiCadPcbGraphicRect rect: b.AddChild(BuildGrRect(rect)); break;
                    case KiCadPcbGraphicPoly poly: b.AddChild(BuildGrPoly(poly)); break;
                    case KiCadPcbGraphicBezier bezier: b.AddChild(BuildGrBezier(bezier)); break;
                    case KiCadPcbText text: b.AddChild(BuildGrText(text)); break;
                    case KiCadPcbTrack track: b.AddChild(BuildSegment(track)); break;
                    case KiCadPcbVia via: b.AddChild(BuildVia(via)); break;
                    case KiCadPcbArc pcbArc: b.AddChild(BuildArc(pcbArc)); break;
                    case KiCadPcbZone zone: b.AddChild(BuildZoneStructured(zone)); break;
                    case SExpr raw: b.AddChild(raw); break;
                }
            }

            // Zones (from legacy regions) — these don't participate in ordered list
            foreach (var region in pcb.Regions.OfType<KiCadPcbRegion>())
            {
                b.AddChild(BuildZoneLegacy(region));
            }
        }
        else
        {
            // Fallback: emit by type when no ordering data (e.g., programmatically created)
            foreach (var line in pcb.GraphicLines)
                b.AddChild(BuildGrLine(line));
            foreach (var arc in pcb.GraphicArcs)
                b.AddChild(BuildGrArc(arc));
            foreach (var circle in pcb.GraphicCircles)
                b.AddChild(BuildGrCircle(circle));
            foreach (var rect in pcb.GraphicRects)
                b.AddChild(BuildGrRect(rect));
            foreach (var poly in pcb.GraphicPolys)
                b.AddChild(BuildGrPoly(poly));
            foreach (var bezier in pcb.GraphicBeziers)
                b.AddChild(BuildGrBezier(bezier));
            foreach (var text in pcb.Texts.OfType<KiCadPcbText>())
                b.AddChild(BuildGrText(text));
            foreach (var track in pcb.Tracks.OfType<KiCadPcbTrack>())
                b.AddChild(BuildSegment(track));
            foreach (var via in pcb.Vias.OfType<KiCadPcbVia>())
                b.AddChild(BuildVia(via));
            foreach (var arc in pcb.Arcs.OfType<KiCadPcbArc>())
                b.AddChild(BuildArc(arc));
            foreach (var zone in pcb.Zones)
                b.AddChild(BuildZoneStructured(zone));
            foreach (var region in pcb.Regions.OfType<KiCadPcbRegion>())
                b.AddChild(BuildZoneLegacy(region));
            foreach (var raw in pcb.RawElementList)
                b.AddChild(raw);
            foreach (var gen in pcb.GeneratedElements)
                b.AddChild(gen);
        }

        // Embedded fonts (KiCad 8+) — emit before embedded_files to match KiCad ordering
        if (pcb.EmbeddedFonts.HasValue)
            b.AddChild("embedded_fonts", ef => ef.AddBool(pcb.EmbeddedFonts.Value));

        // Embedded files (raw S-expression pass-through)
        if (pcb.EmbeddedFilesRaw is not null)
        {
            b.AddChild(pcb.EmbeddedFilesRaw);
        }

        return b.Build();
    }

    private static SExpr BuildSegment(KiCadPcbTrack track)
    {
        var sb = new SExpressionBuilder("segment");

        if (track.IsLocked && !track.LockedIsChildNode)
            sb.AddSymbol("locked");

        sb.AddChild("start", s => { s.AddMm(track.Start.X); s.AddMm(track.Start.Y); })
          .AddChild("end", e => { e.AddMm(track.End.X); e.AddMm(track.End.Y); })
          .AddChild("width", w => w.AddMm(track.Width));

        if (track.IsLocked && track.LockedIsChildNode)
            sb.AddChild("locked", l => l.AddBool(true));

        if (track.LayerName is not null)
            sb.AddChild("layer", l => l.AddValue(track.LayerName));

        sb.AddChild("net", n => n.AddValue(track.Net));

        if (track.Uuid is not null)
            sb.AddChild(WriterHelper.BuildUuid(track.Uuid));

        if (track.Status.HasValue)
            sb.AddChild("status", s => s.AddValue(track.Status.Value));

        return sb.Build();
    }

    private static SExpr BuildVia(KiCadPcbVia via)
    {
        var vb = new SExpressionBuilder("via");

        if (via.ViaType == ViaType.BlindBuried)
            vb.AddSymbol("blind");
        else if (via.ViaType == ViaType.Micro)
            vb.AddSymbol("micro");

        if (via.IsLocked && !via.LockedIsChildNode)
            vb.AddSymbol("locked");

        vb.AddChild(WriterHelper.BuildPositionCompact(via.Location))
          .AddChild("size", s => s.AddMm(via.Diameter))
          .AddChild("drill", d => d.AddMm(via.HoleSize));

        if (via.StartLayerName is not null && via.EndLayerName is not null)
        {
            vb.AddChild("layers", l =>
            {
                l.AddValue(via.StartLayerName);
                l.AddValue(via.EndLayerName);
            });
        }

        if (via.RemoveUnusedLayers is not null)
            vb.AddChild("remove_unused_layers", r => r.AddBool(via.RemoveUnusedLayers.Value));
        if (via.KeepEndLayers is not null)
            vb.AddChild("keep_end_layers", k => k.AddBool(via.KeepEndLayers.Value));
        if (via.IsLocked && via.LockedIsChildNode)
            vb.AddChild("locked", l => l.AddBool(true));
        if (via.IsFree)
            vb.AddChild("free", f => f.AddBool(true));

        // KiCad 9+ via tenting/covering/plugging/filling/capping (raw)
        if (via.TentingRaw is not null) vb.AddChild(via.TentingRaw);
        if (via.CappingRaw is not null) vb.AddChild(via.CappingRaw);
        if (via.CoveringRaw is not null) vb.AddChild(via.CoveringRaw);
        if (via.PluggingRaw is not null) vb.AddChild(via.PluggingRaw);
        if (via.FillingRaw is not null) vb.AddChild(via.FillingRaw);
        if (via.ZoneLayerConnectionsRaw is not null) vb.AddChild(via.ZoneLayerConnectionsRaw);

        // Teardrops come before net/uuid in KiCad format
        if (via.TeardropRaw is not null)
            vb.AddChild(via.TeardropRaw);

        vb.AddChild("net", n => n.AddValue(via.Net));

        if (via.Uuid is not null)
            vb.AddChild(WriterHelper.BuildUuid(via.Uuid));

        if (via.Status.HasValue)
            vb.AddChild("status", s => s.AddValue(via.Status.Value));

        return vb.Build();
    }

    private static SExpr BuildArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("arc");

        if (arc.IsLocked)
            ab.AddSymbol("locked");

        ab.AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
          .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
          .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
          .AddChild("width", w => w.AddMm(arc.Width));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddValue(arc.LayerName));

        ab.AddChild("net", n => n.AddValue(arc.Net));

        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuid(arc.Uuid));

        if (arc.Status.HasValue)
            ab.AddChild("status", s => s.AddValue(arc.Status.Value));

        return ab.Build();
    }

    private static SExpr BuildGrText(KiCadPcbText text)
    {
        var tb = new SExpressionBuilder("gr_text")
            .AddValue(text.Text);

        tb.AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));

        if (text.IsHidden)
            tb.AddSymbol("hide");

        if (text.LayerName is not null)
            tb.AddChild("layer", l =>
            {
                l.AddValue(text.LayerName);
                if (text.IsKnockout)
                    l.AddSymbol("knockout");
            });

        if (text.Uuid is not null)
            tb.AddChild(WriterHelper.BuildUuid(text.Uuid));

        var fontW = text.FontWidth != Coord.Zero ? text.FontWidth : text.Height;
        tb.AddChild(WriterHelper.BuildPcbTextEffects(
            text.Height, fontW,
            justification: text.Justification,
            isBold: text.FontBold,
            isItalic: text.FontItalic,
            isMirrored: text.IsMirrored,
            thickness: text.FontThickness,
            fontFace: text.FontName,
            fontColor: text.FontColor));

        // Render cache (raw)
        if (text.RenderCache is not null)
            tb.AddChild(text.RenderCache);

        return tb.Build();
    }

    private static SExpr BuildZoneLegacy(KiCadPcbRegion region)
    {
        var zb = new SExpressionBuilder("zone")
            .AddChild("net", n => n.AddValue(region.Net));

        if (region.NetName is not null)
            zb.AddChild("net_name", n => n.AddValue(region.NetName));

        if (region.LayerName is not null)
            zb.AddChild("layer", l => l.AddValue(region.LayerName));

        if (region.Priority > 0)
            zb.AddChild("priority", p => p.AddValue(region.Priority));

        if (region.Uuid is not null)
            zb.AddChild(WriterHelper.BuildUuid(region.Uuid));

        zb.AddChild("polygon", p =>
        {
            p.AddChild(WriterHelper.BuildPoints(region.Outline));
        });

        return zb.Build();
    }

    // -- Board-level graphic builders --

    private static void AddGraphicCommon(SExpressionBuilder gb, KiCadPcbGraphic graphic)
    {
        if (graphic.HasStroke || graphic.StrokeWidth != Coord.Zero || graphic.StrokeStyle != LineStyle.Solid)
            gb.AddChild(WriterHelper.BuildStroke(graphic.StrokeWidth, graphic.StrokeStyle, graphic.StrokeColor));

        if (graphic.UsePcbFillFormat)
            gb.AddChild(WriterHelper.BuildPcbFill(graphic.FillType));
        else if (graphic.FillType != SchFillType.None)
            gb.AddChild(WriterHelper.BuildFill(graphic.FillType, graphic.FillColor));

        // Locked as child node (KiCad 9+) — between stroke/fill and layer
        if (graphic.IsLocked && graphic.LockedIsChildNode)
            gb.AddChild("locked", l => l.AddBool(true));

        if (graphic.LayerName is not null)
            gb.AddChild("layer", l => l.AddValue(graphic.LayerName));

        if (graphic.Uuid is not null)
            gb.AddChild(WriterHelper.BuildUuid(graphic.Uuid));
    }

    private static SExpr BuildGrLine(KiCadPcbGraphicLine line)
    {
        var gb = new SExpressionBuilder("gr_line");
        if (line.IsLocked && !line.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddMm(line.Start.X); s.AddMm(line.Start.Y); })
          .AddChild("end", e => { e.AddMm(line.End.X); e.AddMm(line.End.Y); });
        AddGraphicCommon(gb, line);
        return gb.Build();
    }

    private static SExpr BuildGrArc(KiCadPcbGraphicArc arc)
    {
        var gb = new SExpressionBuilder("gr_arc");
        if (arc.IsLocked && !arc.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddMm(arc.Start.X); s.AddMm(arc.Start.Y); })
          .AddChild("mid", m => { m.AddMm(arc.Mid.X); m.AddMm(arc.Mid.Y); })
          .AddChild("end", e => { e.AddMm(arc.End.X); e.AddMm(arc.End.Y); });
        AddGraphicCommon(gb, arc);
        return gb.Build();
    }

    private static SExpr BuildGrCircle(KiCadPcbGraphicCircle circle)
    {
        var gb = new SExpressionBuilder("gr_circle");
        if (circle.IsLocked && !circle.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
          .AddChild("end", e => { e.AddMm(circle.End.X); e.AddMm(circle.End.Y); });
        AddGraphicCommon(gb, circle);
        return gb.Build();
    }

    private static SExpr BuildGrRect(KiCadPcbGraphicRect rect)
    {
        var gb = new SExpressionBuilder("gr_rect");
        if (rect.IsLocked && !rect.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddMm(rect.Start.X); s.AddMm(rect.Start.Y); })
          .AddChild("end", e => { e.AddMm(rect.End.X); e.AddMm(rect.End.Y); });
        AddGraphicCommon(gb, rect);
        return gb.Build();
    }

    private static SExpr BuildGrPoly(KiCadPcbGraphicPoly poly)
    {
        var gb = new SExpressionBuilder("gr_poly");
        if (poly.IsLocked && !poly.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild(WriterHelper.BuildPoints(poly.Points));
        AddGraphicCommon(gb, poly);
        return gb.Build();
    }

    private static SExpr BuildGrBezier(KiCadPcbGraphicBezier bezier)
    {
        var gb = new SExpressionBuilder("gr_curve");
        if (bezier.IsLocked && !bezier.LockedIsChildNode) gb.AddSymbol("locked");
        gb.AddChild(WriterHelper.BuildPoints(bezier.Points));
        AddGraphicCommon(gb, bezier);
        return gb.Build();
    }

    // -- Net class builder --

    private static SExpr BuildNetClass(KiCadPcbNetClass nc)
    {
        var nb = new SExpressionBuilder("net_class")
            .AddValue(nc.Name)
            .AddValue(nc.Description);

        if (nc.Clearance != Coord.Zero)
            nb.AddChild("clearance", c => c.AddMm(nc.Clearance));
        if (nc.TraceWidth != Coord.Zero)
            nb.AddChild("trace_width", c => c.AddMm(nc.TraceWidth));
        if (nc.ViaDia != Coord.Zero)
            nb.AddChild("via_dia", c => c.AddMm(nc.ViaDia));
        if (nc.ViaDrill != Coord.Zero)
            nb.AddChild("via_drill", c => c.AddMm(nc.ViaDrill));
        if (nc.UViaDia != Coord.Zero)
            nb.AddChild("uvia_dia", c => c.AddMm(nc.UViaDia));
        if (nc.UViaDrill != Coord.Zero)
            nb.AddChild("uvia_drill", c => c.AddMm(nc.UViaDrill));

        foreach (var netName in nc.NetNames)
        {
            nb.AddChild("add_net", n => n.AddValue(netName));
        }

        return nb.Build();
    }

    // -- Zone structured builder --

    private static SExpr BuildZoneStructured(KiCadPcbZone zone)
    {
        var zb = new SExpressionBuilder("zone");

        if (zone.IsLocked)
            zb.AddSymbol("locked");

        zb.AddChild("net", n => n.AddValue(zone.Net));

        if (zone.NetName is not null)
            zb.AddChild("net_name", n => n.AddValue(zone.NetName));

        if (zone.LayerNames is not null && zone.LayerNames.Count > 0)
        {
            zb.AddChild("layers", l =>
            {
                foreach (var layer in zone.LayerNames)
                    l.AddValue(layer);
            });
        }
        else if (zone.LayerName is not null)
        {
            zb.AddChild("layer", l => l.AddValue(zone.LayerName));
        }

        if (zone.Uuid is not null)
            zb.AddChild(WriterHelper.BuildUuid(zone.Uuid));

        if (zone.Name is not null)
            zb.AddChild("name", n => n.AddValue(zone.Name));

        // Hatch
        if (zone.HatchStyle is not null)
        {
            zb.AddChild("hatch", h =>
            {
                h.AddSymbol(zone.HatchStyle);
                h.AddValue(zone.HatchPitch);
            });
        }

        if (zone.Priority > 0)
            zb.AddChild("priority", p => p.AddValue(zone.Priority));

        // attr (raw) - must come after priority, before connect_pads
        if (zone.AttrRaw is not null)
            zb.AddChild(zone.AttrRaw);

        // Connect pads (prefer raw)
        if (zone.ConnectPadsRaw is not null)
            zb.AddChild(zone.ConnectPadsRaw);

        if (zone.MinThickness != Coord.Zero)
            zb.AddChild("min_thickness", m => m.AddMm(zone.MinThickness));

        // filled_areas_thickness (raw)
        if (zone.FilledAreasThicknessRaw is not null)
            zb.AddChild(zone.FilledAreasThicknessRaw);

        // Keepout (prefer raw)
        if (zone.KeepoutRaw is not null)
            zb.AddChild(zone.KeepoutRaw);

        // Placement (raw, between keepout and fill)
        if (zone.PlacementRaw is not null)
            zb.AddChild(zone.PlacementRaw);

        // Fill (prefer raw)
        if (zone.FillRaw is not null)
            zb.AddChild(zone.FillRaw);

        // Outline polygon (prefer raw for arcs/complex polygons)
        if (zone.PolygonRaw is not null)
        {
            zb.AddChild(zone.PolygonRaw);
        }
        else if (zone.Outline.Count > 0)
        {
            zb.AddChild("polygon", p =>
            {
                p.AddChild(WriterHelper.BuildPoints(zone.Outline));
            });
        }

        // Filled polygons (raw)
        foreach (var fp in zone.FilledPolygonsRaw)
        {
            zb.AddChild(fp);
        }

        // Fill segments (raw)
        foreach (var fs in zone.FillSegmentsRaw)
        {
            zb.AddChild(fs);
        }

        return zb.Build();
    }
}
