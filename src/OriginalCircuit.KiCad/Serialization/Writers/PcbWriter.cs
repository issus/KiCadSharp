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
            .AddChild("generator", g => g.AddValue(pcb.Generator ?? "kicadsharp"))
            .AddChild("generator_version", g => g.AddValue(pcb.GeneratorVersion ?? "1.0"));

        // General (prefer raw subtree for round-trip, fallback to constructed)
        if (pcb.GeneralRaw is not null)
            b.AddChild(pcb.GeneralRaw);
        else
            b.AddChild("general", gen =>
            {
                gen.AddChild("thickness", t => t.AddValue(pcb.BoardThickness.ToMm()));
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

        // Graphic lines
        foreach (var line in pcb.GraphicLines)
        {
            b.AddChild(BuildGrLine(line));
        }

        // Graphic arcs
        foreach (var arc in pcb.GraphicArcs)
        {
            b.AddChild(BuildGrArc(arc));
        }

        // Graphic circles
        foreach (var circle in pcb.GraphicCircles)
        {
            b.AddChild(BuildGrCircle(circle));
        }

        // Graphic rectangles
        foreach (var rect in pcb.GraphicRects)
        {
            b.AddChild(BuildGrRect(rect));
        }

        // Graphic polygons
        foreach (var poly in pcb.GraphicPolys)
        {
            b.AddChild(BuildGrPoly(poly));
        }

        // Graphic bezier curves
        foreach (var bezier in pcb.GraphicBeziers)
        {
            b.AddChild(BuildGrBezier(bezier));
        }

        // Texts
        foreach (var text in pcb.Texts.OfType<KiCadPcbText>())
        {
            b.AddChild(BuildGrText(text));
        }

        // Tracks
        foreach (var track in pcb.Tracks.OfType<KiCadPcbTrack>())
        {
            b.AddChild(BuildSegment(track));
        }

        // Vias
        foreach (var via in pcb.Vias.OfType<KiCadPcbVia>())
        {
            b.AddChild(BuildVia(via));
        }

        // Arcs
        foreach (var arc in pcb.Arcs.OfType<KiCadPcbArc>())
        {
            b.AddChild(BuildArc(arc));
        }

        // Zones (structured)
        foreach (var zone in pcb.Zones)
        {
            b.AddChild(BuildZoneStructured(zone));
        }

        // Zones (from legacy regions)
        foreach (var region in pcb.Regions.OfType<KiCadPcbRegion>())
        {
            b.AddChild(BuildZoneLegacy(region));
        }

        // Raw elements (dimensions, targets, groups)
        foreach (var raw in pcb.RawElementList)
        {
            b.AddChild(raw);
        }

        return b.Build();
    }

    private static SExpr BuildSegment(KiCadPcbTrack track)
    {
        var sb = new SExpressionBuilder("segment");

        if (track.IsLocked)
            sb.AddSymbol("locked");

        sb.AddChild("start", s => { s.AddValue(track.Start.X.ToMm()); s.AddValue(track.Start.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(track.End.X.ToMm()); e.AddValue(track.End.Y.ToMm()); })
          .AddChild("width", w => w.AddValue(track.Width.ToMm()));

        if (track.LayerName is not null)
            sb.AddChild("layer", l => l.AddSymbol(track.LayerName));

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

        if (via.IsLocked)
            vb.AddSymbol("locked");

        if (via.IsFree)
            vb.AddChild("free", _ => { });
        if (via.RemoveUnusedLayers)
            vb.AddChild("remove_unused_layers", _ => { });
        if (via.KeepEndLayers)
            vb.AddChild("keep_end_layers", _ => { });

        vb.AddChild(WriterHelper.BuildPosition(via.Location))
          .AddChild("size", s => s.AddValue(via.Diameter.ToMm()))
          .AddChild("drill", d => d.AddValue(via.HoleSize.ToMm()));

        if (via.StartLayerName is not null && via.EndLayerName is not null)
        {
            vb.AddChild("layers", l =>
            {
                l.AddSymbol(via.StartLayerName);
                l.AddSymbol(via.EndLayerName);
            });
        }

        vb.AddChild("net", n => n.AddValue(via.Net));

        if (via.Uuid is not null)
            vb.AddChild(WriterHelper.BuildUuid(via.Uuid));

        if (via.Status.HasValue)
            vb.AddChild("status", s => s.AddValue(via.Status.Value));

        if (via.TeardropRaw is not null)
            vb.AddChild(via.TeardropRaw);

        return vb.Build();
    }

    private static SExpr BuildArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("arc");

        if (arc.IsLocked)
            ab.AddSymbol("locked");

        ab.AddChild("start", s => { s.AddValue(arc.ArcStart.X.ToMm()); s.AddValue(arc.ArcStart.Y.ToMm()); })
          .AddChild("mid", m => { m.AddValue(arc.ArcMid.X.ToMm()); m.AddValue(arc.ArcMid.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(arc.ArcEnd.X.ToMm()); e.AddValue(arc.ArcEnd.Y.ToMm()); })
          .AddChild("width", w => w.AddValue(arc.Width.ToMm()));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddSymbol(arc.LayerName));

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
            .AddValue(text.Text)
            .AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));

        if (text.IsHidden)
            tb.AddSymbol("hide");

        if (text.LayerName is not null)
            tb.AddChild("layer", l => l.AddSymbol(text.LayerName));

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

        if (text.Uuid is not null)
            tb.AddChild(WriterHelper.BuildUuid(text.Uuid));

        return tb.Build();
    }

    private static SExpr BuildZoneLegacy(KiCadPcbRegion region)
    {
        var zb = new SExpressionBuilder("zone")
            .AddChild("net", n => n.AddValue(region.Net));

        if (region.NetName is not null)
            zb.AddChild("net_name", n => n.AddValue(region.NetName));

        if (region.LayerName is not null)
            zb.AddChild("layer", l => l.AddSymbol(region.LayerName));

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
        if (graphic.LayerName is not null)
            gb.AddChild("layer", l => l.AddSymbol(graphic.LayerName));

        if (graphic.StrokeWidth != Coord.Zero || graphic.StrokeStyle != LineStyle.Solid)
            gb.AddChild(WriterHelper.BuildStroke(graphic.StrokeWidth, graphic.StrokeStyle, graphic.StrokeColor));

        if (graphic.FillType != SchFillType.None)
            gb.AddChild(WriterHelper.BuildFill(graphic.FillType, graphic.FillColor));

        if (graphic.Uuid is not null)
            gb.AddChild(WriterHelper.BuildUuid(graphic.Uuid));
    }

    private static SExpr BuildGrLine(KiCadPcbGraphicLine line)
    {
        var gb = new SExpressionBuilder("gr_line");
        if (line.IsLocked) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddValue(line.Start.X.ToMm()); s.AddValue(line.Start.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(line.End.X.ToMm()); e.AddValue(line.End.Y.ToMm()); });
        AddGraphicCommon(gb, line);
        return gb.Build();
    }

    private static SExpr BuildGrArc(KiCadPcbGraphicArc arc)
    {
        var gb = new SExpressionBuilder("gr_arc");
        if (arc.IsLocked) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddValue(arc.Start.X.ToMm()); s.AddValue(arc.Start.Y.ToMm()); })
          .AddChild("mid", m => { m.AddValue(arc.Mid.X.ToMm()); m.AddValue(arc.Mid.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(arc.End.X.ToMm()); e.AddValue(arc.End.Y.ToMm()); });
        AddGraphicCommon(gb, arc);
        return gb.Build();
    }

    private static SExpr BuildGrCircle(KiCadPcbGraphicCircle circle)
    {
        var gb = new SExpressionBuilder("gr_circle");
        if (circle.IsLocked) gb.AddSymbol("locked");
        gb.AddChild("center", c => { c.AddValue(circle.Center.X.ToMm()); c.AddValue(circle.Center.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(circle.End.X.ToMm()); e.AddValue(circle.End.Y.ToMm()); });
        AddGraphicCommon(gb, circle);
        return gb.Build();
    }

    private static SExpr BuildGrRect(KiCadPcbGraphicRect rect)
    {
        var gb = new SExpressionBuilder("gr_rect");
        if (rect.IsLocked) gb.AddSymbol("locked");
        gb.AddChild("start", s => { s.AddValue(rect.Start.X.ToMm()); s.AddValue(rect.Start.Y.ToMm()); })
          .AddChild("end", e => { e.AddValue(rect.End.X.ToMm()); e.AddValue(rect.End.Y.ToMm()); });
        AddGraphicCommon(gb, rect);
        return gb.Build();
    }

    private static SExpr BuildGrPoly(KiCadPcbGraphicPoly poly)
    {
        var gb = new SExpressionBuilder("gr_poly");
        if (poly.IsLocked) gb.AddSymbol("locked");
        gb.AddChild(WriterHelper.BuildPoints(poly.Points));
        AddGraphicCommon(gb, poly);
        return gb.Build();
    }

    private static SExpr BuildGrBezier(KiCadPcbGraphicBezier bezier)
    {
        var gb = new SExpressionBuilder("gr_curve");
        if (bezier.IsLocked) gb.AddSymbol("locked");
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
            nb.AddChild("clearance", c => c.AddValue(nc.Clearance.ToMm()));
        if (nc.TraceWidth != Coord.Zero)
            nb.AddChild("trace_width", c => c.AddValue(nc.TraceWidth.ToMm()));
        if (nc.ViaDia != Coord.Zero)
            nb.AddChild("via_dia", c => c.AddValue(nc.ViaDia.ToMm()));
        if (nc.ViaDrill != Coord.Zero)
            nb.AddChild("via_drill", c => c.AddValue(nc.ViaDrill.ToMm()));
        if (nc.UViaDia != Coord.Zero)
            nb.AddChild("uvia_dia", c => c.AddValue(nc.UViaDia.ToMm()));
        if (nc.UViaDrill != Coord.Zero)
            nb.AddChild("uvia_drill", c => c.AddValue(nc.UViaDrill.ToMm()));

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
            zb.AddChild("layer", l => l.AddSymbol(zone.LayerName));
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

        // Connect pads (prefer raw)
        if (zone.ConnectPadsRaw is not null)
            zb.AddChild(zone.ConnectPadsRaw);

        if (zone.MinThickness != Coord.Zero)
            zb.AddChild("min_thickness", m => m.AddValue(zone.MinThickness.ToMm()));

        // Keepout (prefer raw)
        if (zone.KeepoutRaw is not null)
            zb.AddChild(zone.KeepoutRaw);

        // Fill (prefer raw)
        if (zone.FillRaw is not null)
            zb.AddChild(zone.FillRaw);

        // Outline polygon
        if (zone.Outline.Count > 0)
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
