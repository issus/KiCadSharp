using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models;
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
        var expr = Build(pcb);
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
        var expr = Build(pcb);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadPcb pcb)
    {
        var b = new SExpressionBuilder("kicad_pcb")
            .AddChild("version", v => v.AddValue(pcb.Version == 0 ? 20231120 : pcb.Version))
            .AddChild("generator", g =>
            {
                if (pcb.GeneratorIsSymbol)
                    g.AddSymbol(pcb.Generator ?? "pcbnew");
                else
                    g.AddValue(pcb.Generator ?? "pcbnew");
            });

        if (pcb.GeneratorVersion is not null)
            b.AddChild("generator_version", g => g.AddValue(pcb.GeneratorVersion));

        // General
        b.AddChild("general", gen =>
        {
            gen.AddChild("thickness", t => t.AddMm(pcb.Setup?.HasBoardThickness == true ? pcb.Setup.BoardThickness : pcb.BoardThickness));
            if (pcb.HasLegacyTeardrops)
                gen.AddChild("legacy_teardrops", lt => lt.AddBool(pcb.LegacyTeardrops));
        });

        // Paper (comes before layers in KiCad format)
        if (pcb.PaperWidth.HasValue && pcb.PaperHeight.HasValue)
        {
            b.AddChild("paper", p =>
            {
                if (pcb.Paper is not null) p.AddValue(pcb.Paper);
                p.AddValue(pcb.PaperWidth.Value);
                p.AddValue(pcb.PaperHeight.Value);
                if (pcb.PaperPortrait) p.AddSymbol("portrait");
            });
        }
        else if (pcb.Paper is not null)
        {
            b.AddChild("paper", p =>
            {
                p.AddValue(pcb.Paper);
                if (pcb.PaperPortrait) p.AddSymbol("portrait");
            });
        }

        // Title block (between paper and layers)
        if (pcb.TitleBlock is not null)
            b.AddChild(BuildTitleBlock(pcb.TitleBlock));

        // Layers
        if (pcb.LayerDefinitions.Count > 0)
        {
            b.AddChild("layers", layers =>
            {
                foreach (var def in pcb.LayerDefinitions)
                {
                    layers.AddChild(def.Ordinal.ToString(), lb =>
                    {
                        lb.AddValue(def.CanonicalName);
                        lb.AddSymbol(def.LayerType);
                        if (def.UserName is not null)
                            lb.AddValue(def.UserName);
                    });
                }
            });
        }

        // Setup
        if (pcb.Setup is not null)
            b.AddChild(BuildSetup(pcb.Setup));

        // Board-level properties
        foreach (var prop in pcb.Properties)
            b.AddChild("property", p => { p.AddValue(prop.Key); p.AddValue(prop.Value); });

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
                    case KiCadPcbGroup group: b.AddChild(BuildGroup(group)); break;
                    case KiCadPcbDimension dim: b.AddChild(BuildDimension(dim)); break;
                    case KiCadPcbGeneratedElement gen: b.AddChild(BuildGeneratedElement(gen)); break;
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
            foreach (var group in pcb.Groups)
            {
                // Only emit if not already emitted via order list
                if (!pcb.BoardElementOrder.Contains(group))
                    b.AddChild(BuildGroup(group));
            }
            foreach (var dim in pcb.Dimensions)
                b.AddChild(BuildDimension(dim));
            foreach (var gen in pcb.GeneratedElements)
                b.AddChild(BuildGeneratedElement(gen));
        }

        // Embedded fonts (KiCad 8+) — emit before embedded_files to match KiCad ordering
        if (pcb.EmbeddedFonts.HasValue)
            b.AddChild("embedded_fonts", ef => ef.AddBool(pcb.EmbeddedFonts.Value));

        // Embedded files
        if (pcb.EmbeddedFiles is not null)
            b.AddChild(BuildEmbeddedFiles(pcb.EmbeddedFiles));

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
            sb.AddChild(WriterHelper.BuildUuidToken(track.Uuid, track.UuidToken ?? "uuid", track.UuidIsSymbol));

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

        // Teardrops (before net/uuid in KiCad format)
        if (via.TeardropEnabled || via.TeardropBestLengthRatio.HasValue)
        {
            vb.AddChild("teardrops", td =>
            {
                if (via.TeardropBestLengthRatio.HasValue) td.AddChild("best_length_ratio", c => c.AddValue(via.TeardropBestLengthRatio.Value));
                if (via.TeardropMaxLength.HasValue) td.AddChild("max_length", c => c.AddMm(via.TeardropMaxLength.Value));
                if (via.TeardropBestWidthRatio.HasValue) td.AddChild("best_width_ratio", c => c.AddValue(via.TeardropBestWidthRatio.Value));
                if (via.TeardropMaxWidth.HasValue) td.AddChild("max_width", c => c.AddMm(via.TeardropMaxWidth.Value));
                if (via.TeardropCurvedEdges.HasValue) td.AddChild("curved_edges", c => c.AddBool(via.TeardropCurvedEdges.Value));
                if (via.TeardropFilterRatio.HasValue) td.AddChild("filter_ratio", c => c.AddValue(via.TeardropFilterRatio.Value));
                td.AddChild("enabled", c => c.AddBool(via.TeardropEnabled));
                if (via.TeardropAllowTwoSegments.HasValue) td.AddChild("allow_two_segments", c => c.AddBool(via.TeardropAllowTwoSegments.Value));
                if (via.TeardropPreferZoneConnections.HasValue) td.AddChild("prefer_zone_connections", c => c.AddBool(via.TeardropPreferZoneConnections.Value));
            });
        }

        // Tenting (before net/uuid in KiCad format)
        if (via.HasTenting)
        {
            if (via.TentingIsChildNode)
            {
                vb.AddChild("tenting", t =>
                {
                    if (via.TentingFrontValue is not null) t.AddChild("front", f => f.AddSymbol(via.TentingFrontValue));
                    if (via.TentingBackValue is not null) t.AddChild("back", bk => bk.AddSymbol(via.TentingBackValue));
                });
            }
            else
            {
                vb.AddChild("tenting", t =>
                {
                    if (via.TentingFront == true) t.AddSymbol("front");
                    if (via.TentingBack == true) t.AddSymbol("back");
                });
            }
        }

        // Capping (before net/uuid in KiCad format)
        if (via.Capping is not null)
            vb.AddChild("capping", c => c.AddSymbol(via.Capping));

        // Covering (before net/uuid in KiCad format)
        if (via.HasCovering)
        {
            vb.AddChild("covering", c =>
            {
                if (via.CoveringFront is not null) c.AddChild("front", f => f.AddSymbol(via.CoveringFront));
                if (via.CoveringBack is not null) c.AddChild("back", bk => bk.AddSymbol(via.CoveringBack));
            });
        }

        // Plugging (before net/uuid in KiCad format)
        if (via.HasPlugging)
        {
            vb.AddChild("plugging", p =>
            {
                if (via.PluggingFront is not null) p.AddChild("front", f => f.AddSymbol(via.PluggingFront));
                if (via.PluggingBack is not null) p.AddChild("back", bk => bk.AddSymbol(via.PluggingBack));
            });
        }

        // Filling (before net/uuid in KiCad format)
        if (via.Filling is not null)
            vb.AddChild("filling", f => f.AddSymbol(via.Filling));

        vb.AddChild("net", n => n.AddValue(via.Net));

        if (via.Uuid is not null)
            vb.AddChild(WriterHelper.BuildUuidToken(via.Uuid, via.UuidToken ?? "uuid", via.UuidIsSymbol));

        if (via.Status.HasValue)
            vb.AddChild("status", s => s.AddValue(via.Status.Value));

        // Zone layer connections
        if (via.ZoneLayerConnections is { Count: > 0 })
        {
            vb.AddChild("zone_layer_connections", z =>
            {
                foreach (var layer in via.ZoneLayerConnections)
                    z.AddValue(layer);
            });
        }

        return vb.Build();
    }

    private static SExpr BuildArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("arc");

        if (arc.IsLocked && !arc.LockedIsChildNode)
            ab.AddSymbol("locked");

        ab.AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
          .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
          .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
          .AddChild("width", w => w.AddMm(arc.Width));

        if (arc.IsLocked && arc.LockedIsChildNode)
            ab.AddChild("locked", l => l.AddBool(true));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddValue(arc.LayerName));

        ab.AddChild("net", n => n.AddValue(arc.Net));

        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuidToken(arc.Uuid, arc.UuidToken ?? "uuid", arc.UuidIsSymbol));

        if (arc.Status.HasValue)
            ab.AddChild("status", s => s.AddValue(arc.Status.Value));

        return ab.Build();
    }

    private static SExpr BuildGrText(KiCadPcbText text)
    {
        var tb = new SExpressionBuilder("gr_text")
            .AddValue(text.Text);

        if (text.PositionIncludesAngle)
            tb.AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));
        else
            tb.AddChild(WriterHelper.BuildPositionCompact(text.Location, text.Rotation));

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
            tb.AddChild(WriterHelper.BuildUuidToken(text.Uuid, text.UuidToken ?? "uuid", text.UuidIsSymbol));

        var fontW = text.FontWidth != Coord.Zero ? text.FontWidth : text.Height;
        tb.AddChild(WriterHelper.BuildPcbTextEffects(
            text.Height, fontW,
            justification: text.Justification,
            isBold: text.FontBold,
            isItalic: text.FontItalic,
            isMirrored: text.IsMirrored,
            thickness: text.FontThickness,
            fontFace: text.FontName,
            fontColor: text.FontColor,
            boldIsSymbol: text.BoldIsSymbol,
            italicIsSymbol: text.ItalicIsSymbol));

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
            gb.AddChild(WriterHelper.BuildStroke(graphic.StrokeWidth, graphic.StrokeStyle, graphic.StrokeColor, emitColor: graphic.HasStrokeColor));

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
            gb.AddChild(WriterHelper.BuildUuidToken(graphic.Uuid, graphic.UuidToken ?? "uuid", graphic.UuidIsSymbol));
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

        if (nc.HasClearance || nc.Clearance != Coord.Zero)
            nb.AddChild("clearance", c => c.AddMm(nc.Clearance));
        if (nc.HasTraceWidth || nc.TraceWidth != Coord.Zero)
            nb.AddChild("trace_width", c => c.AddMm(nc.TraceWidth));
        if (nc.HasViaDia || nc.ViaDia != Coord.Zero)
            nb.AddChild("via_dia", c => c.AddMm(nc.ViaDia));
        if (nc.HasViaDrill || nc.ViaDrill != Coord.Zero)
            nb.AddChild("via_drill", c => c.AddMm(nc.ViaDrill));
        if (nc.HasUViaDia || nc.UViaDia != Coord.Zero)
            nb.AddChild("uvia_dia", c => c.AddMm(nc.UViaDia));
        if (nc.HasUViaDrill || nc.UViaDrill != Coord.Zero)
            nb.AddChild("uvia_drill", c => c.AddMm(nc.UViaDrill));
        if (nc.HasDiffPairWidth || nc.DiffPairWidth != Coord.Zero)
            nb.AddChild("diff_pair_width", c => c.AddMm(nc.DiffPairWidth));
        if (nc.HasDiffPairGap || nc.DiffPairGap != Coord.Zero)
            nb.AddChild("diff_pair_gap", c => c.AddMm(nc.DiffPairGap));
        if (nc.HasBusWidth || nc.BusWidth != Coord.Zero)
            nb.AddChild("bus_width", c => c.AddMm(nc.BusWidth));

        foreach (var netName in nc.NetNames)
        {
            nb.AddChild("add_net", n => n.AddValue(netName));
        }

        return nb.Build();
    }

    // -- Zone structured builder --

    internal static SExpr BuildZoneStructured(KiCadPcbZone zone)
    {
        var zb = new SExpressionBuilder("zone");

        if (zone.IsLocked && !zone.LockedIsChildNode)
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

        if (zone.IsLocked && zone.LockedIsChildNode)
            zb.AddChild("locked", l => l.AddBool(true));

        if (zone.Uuid is not null)
            zb.AddChild(WriterHelper.BuildUuidToken(zone.Uuid, zone.UuidToken ?? "uuid", zone.UuidIsSymbol));

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

        // Attr (must come after hatch/priority, before connect_pads)
        if (zone.HasAttr)
        {
            zb.AddChild("attr", a =>
            {
                if (zone.AttrTeardropType is not null)
                {
                    a.AddChild("teardrop", t =>
                    {
                        t.AddChild("type", ty => ty.AddSymbol(zone.AttrTeardropType));
                    });
                }
            });
        }

        // Connect pads
        if (zone.HasConnectPads || zone.ConnectPadsMode is not null || zone.ConnectPadsClearance != Coord.Zero)
        {
            zb.AddChild("connect_pads", cp =>
            {
                if (zone.ConnectPadsMode is not null)
                    cp.AddSymbol(zone.ConnectPadsMode);
                cp.AddChild("clearance", c => c.AddMm(zone.ConnectPadsClearance));
            });
        }

        if (zone.MinThickness != Coord.Zero)
            zb.AddChild("min_thickness", m => m.AddMm(zone.MinThickness));

        // Filled areas thickness (must come before keepout)
        if (zone.HasFilledAreasThickness)
            zb.AddChild("filled_areas_thickness", f => f.AddBool(zone.FilledAreasThickness));

        // Keepout
        if (zone.IsKeepout)
        {
            zb.AddChild("keepout", k =>
            {
                if (zone.KeepoutTracks is not null)
                    k.AddChild("tracks", t => t.AddSymbol(zone.KeepoutTracks));
                if (zone.KeepoutVias is not null)
                    k.AddChild("vias", v => v.AddSymbol(zone.KeepoutVias));
                if (zone.KeepoutPads is not null)
                    k.AddChild("pads", p => p.AddSymbol(zone.KeepoutPads));
                if (zone.KeepoutCopperpour is not null)
                    k.AddChild("copperpour", c => c.AddSymbol(zone.KeepoutCopperpour));
                if (zone.KeepoutFootprints is not null)
                    k.AddChild("footprints", f => f.AddSymbol(zone.KeepoutFootprints));
            });
        }

        // Placement
        if (zone.HasPlacement)
        {
            zb.AddChild("placement", p =>
            {
                p.AddChild("enabled", e => e.AddBool(zone.PlacementEnabled));
                if (zone.PlacementSheetName is not null)
                    p.AddChild("sheetname", s => s.AddValue(zone.PlacementSheetName));
            });
        }

        // Fill
        {
            zb.AddChild("fill", f =>
            {
                f.AddBool(zone.IsFilled);
                if (zone.FillMode is not null)
                    f.AddChild("mode", m => m.AddSymbol(zone.FillMode));
                if (zone.ThermalGap != Coord.Zero)
                    f.AddChild("thermal_gap", t => t.AddMm(zone.ThermalGap));
                if (zone.ThermalBridgeWidth != Coord.Zero)
                    f.AddChild("thermal_bridge_width", t => t.AddMm(zone.ThermalBridgeWidth));
                if (zone.SmoothingType is not null)
                    f.AddChild("smoothing", s => s.AddSymbol(zone.SmoothingType));
                if (zone.SmoothingRadius != Coord.Zero)
                    f.AddChild("radius", r => r.AddMm(zone.SmoothingRadius));
                if (zone.IslandRemovalMode is not null)
                    f.AddChild("island_removal_mode", i => i.AddValue(zone.IslandRemovalMode.Value));
                if (zone.IslandAreaMin is not null)
                    f.AddChild("island_area_min", i => i.AddValue(zone.IslandAreaMin.Value));
                if (zone.HatchThickness != Coord.Zero)
                    f.AddChild("hatch_thickness", h => h.AddMm(zone.HatchThickness));
                if (zone.HatchGap != Coord.Zero)
                    f.AddChild("hatch_gap", h => h.AddMm(zone.HatchGap));
                if (zone.HatchOrientation != 0)
                    f.AddChild("hatch_orientation", h => h.AddValue(zone.HatchOrientation));
                if (zone.HatchSmoothingLevel != 0)
                    f.AddChild("hatch_smoothing_level", h => h.AddValue(zone.HatchSmoothingLevel));
                if (zone.HatchSmoothingValue != 0)
                    f.AddChild("hatch_smoothing_value", h => h.AddValue(zone.HatchSmoothingValue));
                if (zone.HatchBorderAlgorithm != 0)
                    f.AddChild("hatch_border_algorithm", h => h.AddValue(zone.HatchBorderAlgorithm));
                if (zone.HatchMinHoleArea != 0)
                    f.AddChild("hatch_min_hole_area", h => h.AddValue(zone.HatchMinHoleArea));
            });
        }

        // Outline polygon
        if (zone.Outline.Count > 0)
        {
            zb.AddChild("polygon", p =>
            {
                p.AddChild(WriterHelper.BuildPoints(zone.Outline));
            });
        }

        // Filled polygons
        foreach (var fp in zone.FilledPolygons)
        {
            zb.AddChild("filled_polygon", fpb =>
            {
                fpb.AddChild("layer", l => l.AddValue(fp.LayerName));
                if (fp.IslandIndex.HasValue)
                    fpb.AddChild("island", i => i.AddValue(fp.IslandIndex.Value));
                fpb.AddChild(WriterHelper.BuildPoints(fp.Points));
            });
        }

        // Fill segments (legacy)
        foreach (var fs in zone.FillSegments)
        {
            zb.AddChild("fill_segments", fsb =>
            {
                fsb.AddChild("layer", l => l.AddValue(fs.LayerName));
                fsb.AddChild(WriterHelper.BuildPoints(fs.Points));
            });
        }

        return zb.Build();
    }

    private static SExpr BuildSetup(KiCadPcbSetup setup)
    {
        var sb = new SExpressionBuilder("setup");

        // Stackup
        if (setup.Stackup is not null)
        {
            sb.AddChild("stackup", st =>
            {
                foreach (var layer in setup.Stackup.Layers)
                {
                    st.AddChild("layer", l =>
                    {
                        l.AddValue(layer.Name);
                        if (layer.Sublayers.Count > 0) l.AddSymbol("addsublayer");
                        if (layer.Type is not null) l.AddChild("type", t => t.AddValue(layer.Type));
                        if (layer.Color is not null) l.AddChild("color", c => c.AddValue(layer.Color));
                        if (layer.Thickness.HasValue) l.AddChild("thickness", t => t.AddValue(layer.Thickness.Value));
                        if (layer.Material is not null) l.AddChild("material", m => m.AddValue(layer.Material));
                        if (layer.EpsilonR.HasValue) l.AddChild("epsilon_r", e => e.AddValue(layer.EpsilonR.Value));
                        if (layer.LossTangent.HasValue) l.AddChild("loss_tangent", lt => lt.AddValue(layer.LossTangent.Value));
                        // Emit sublayer properties
                        foreach (var sub in layer.Sublayers)
                        {
                            if (sub.Color is not null) l.AddChild("color", c => c.AddValue(sub.Color));
                            if (sub.Thickness.HasValue) l.AddChild("thickness", t => t.AddValue(sub.Thickness.Value));
                            if (sub.Material is not null) l.AddChild("material", m => m.AddValue(sub.Material));
                            if (sub.EpsilonR.HasValue) l.AddChild("epsilon_r", e => e.AddValue(sub.EpsilonR.Value));
                            if (sub.LossTangent.HasValue) l.AddChild("loss_tangent", lt => lt.AddValue(sub.LossTangent.Value));
                        }
                    });
                }
                if (setup.Stackup.CopperFinish is not null)
                    st.AddChild("copper_finish", c => c.AddValue(setup.Stackup.CopperFinish));
                if (setup.Stackup.DielectricConstraints.HasValue)
                    st.AddChild("dielectric_constraints", d => d.AddBool(setup.Stackup.DielectricConstraints.Value));
                if (setup.Stackup.EdgeConnector is not null)
                    st.AddChild("edge_connector", e => e.AddSymbol(setup.Stackup.EdgeConnector));
                if (setup.Stackup.CastellatedPads.HasValue)
                    st.AddChild("castellated_pads", c => c.AddBool(setup.Stackup.CastellatedPads.Value));
                if (setup.Stackup.EdgePlating.HasValue)
                    st.AddChild("edge_plating", e => e.AddBool(setup.Stackup.EdgePlating.Value));
            });
        }

        if (setup.HasPadToMaskClearance || setup.PadToMaskClearance != Coord.Zero)
            sb.AddChild("pad_to_mask_clearance", c => c.AddMm(setup.PadToMaskClearance));
        if (setup.HasSolderMaskMinWidth || setup.SolderMaskMinWidth != Coord.Zero)
            sb.AddChild("solder_mask_min_width", c => c.AddMm(setup.SolderMaskMinWidth));
        if (setup.HasPadToPasteClearance || setup.PadToPasteClearance != Coord.Zero)
            sb.AddChild("pad_to_paste_clearance", c => c.AddMm(setup.PadToPasteClearance));
        if (setup.PadToPasteClearanceRatio.HasValue)
            sb.AddChild("pad_to_paste_clearance_ratio", c => c.AddValue(setup.PadToPasteClearanceRatio.Value));
        if (setup.AllowSolderMaskBridgesInFootprints.HasValue)
            sb.AddChild("allow_soldermask_bridges_in_footprints", a => a.AddBool(setup.AllowSolderMaskBridgesInFootprints.Value));

        // Tenting
        if (setup.HasTenting)
        {
            if (setup.TentingIsChildNode)
            {
                sb.AddChild("tenting", t =>
                {
                    if (setup.TentingFront) t.AddChild("front", f => f.AddBool(true));
                    if (setup.TentingBack) t.AddChild("back", bk => bk.AddBool(true));
                });
            }
            else
            {
                sb.AddChild("tenting", t =>
                {
                    if (setup.TentingFront) t.AddSymbol("front");
                    if (setup.TentingBack) t.AddSymbol("back");
                });
            }
        }

        // Covering
        if (setup.HasCovering)
        {
            sb.AddChild("covering", c =>
            {
                if (setup.CoveringFront is not null) c.AddChild("front", f => f.AddSymbol(setup.CoveringFront));
                if (setup.CoveringBack is not null) c.AddChild("back", bk => bk.AddSymbol(setup.CoveringBack));
            });
        }

        // Plugging
        if (setup.HasPlugging)
        {
            sb.AddChild("plugging", p =>
            {
                if (setup.PluggingFront is not null) p.AddChild("front", f => f.AddSymbol(setup.PluggingFront));
                if (setup.PluggingBack is not null) p.AddChild("back", bk => bk.AddSymbol(setup.PluggingBack));
            });
        }

        // Capping
        if (setup.Capping is not null)
            sb.AddChild("capping", c => c.AddSymbol(setup.Capping));

        // Filling
        if (setup.Filling is not null)
            sb.AddChild("filling", f => f.AddSymbol(setup.Filling));

        if (setup.HasBoardThickness)
            sb.AddChild("board_thickness", t => t.AddMm(setup.BoardThickness));
        if (setup.AuxAxisOrigin.HasValue)
            sb.AddChild("aux_axis_origin", a => { a.AddMm(setup.AuxAxisOrigin.Value.X); a.AddMm(setup.AuxAxisOrigin.Value.Y); });
        if (setup.GridOrigin.HasValue)
            sb.AddChild("grid_origin", g => { g.AddMm(setup.GridOrigin.Value.X); g.AddMm(setup.GridOrigin.Value.Y); });

        // Plot params
        if (setup.PlotParams is not null)
        {
            sb.AddChild("pcbplotparams", pp =>
            {
                foreach (var (key, value, isSymbol) in setup.PlotParams.Parameters)
                {
                    pp.AddChild(key, p =>
                    {
                        if (isSymbol)
                            p.AddSymbol(value);
                        else
                            p.AddValue(value);
                    });
                }
            });
        }

        return sb.Build();
    }

    internal static SExpr BuildGroup(KiCadPcbGroup group)
    {
        var gb = new SExpressionBuilder("group").AddValue(group.Name);
        if (group.IsLocked && !group.LockedIsChildNode)
            gb.AddSymbol("locked");
        if (group.Id is not null)
            gb.AddChild("uuid", id => id.AddValue(group.Id));
        gb.AddChild("members", m =>
        {
            foreach (var member in group.Members)
                m.AddValue(member);
        });
        if (group.IsLocked && group.LockedIsChildNode)
            gb.AddChild("locked", l => l.AddBool(true));
        return gb.Build();
    }

    internal static SExpr BuildTitleBlock(KiCadTitleBlock tb)
    {
        var b = new SExpressionBuilder("title_block");
        if (tb.Title is not null) b.AddChild("title", t => t.AddValue(tb.Title));
        if (tb.Date is not null) b.AddChild("date", d => d.AddValue(tb.Date));
        if (tb.Revision is not null) b.AddChild("rev", r => r.AddValue(tb.Revision));
        if (tb.Company is not null) b.AddChild("company", c => c.AddValue(tb.Company));
        foreach (var (num, text) in tb.Comments.OrderBy(kv => kv.Key))
            b.AddChild("comment", c => { c.AddValue(num); c.AddValue(text); });
        return b.Build();
    }

    internal static SExpr BuildEmbeddedFiles(KiCadEmbeddedFiles ef)
    {
        var b = new SExpressionBuilder("embedded_files");
        foreach (var file in ef.Files)
        {
            b.AddChild("file", f =>
            {
                f.AddChild("name", n => n.AddValue(file.Name));
                f.AddChild("type", t => t.AddSymbol(file.Type));
                if (file.Checksum is not null)
                    f.AddChild("checksum", c => c.AddValue(file.Checksum));
                if (file.Data.Length > 0)
                    f.AddChild("data", d => d.AddValue(file.Data));
            });
        }
        return b.Build();
    }

    internal static SExpr BuildDimension(KiCadPcbDimension dim)
    {
        var db = new SExpressionBuilder("dimension");

        if (dim.IsLocked && !dim.LockedIsChildNode)
            db.AddSymbol("locked");

        if (dim.DimensionType is not null)
            db.AddChild("type", t => t.AddSymbol(dim.DimensionType));

        if (dim.LayerName is not null)
            db.AddChild("layer", l => l.AddValue(dim.LayerName));

        if (dim.Uuid is not null)
            db.AddChild(WriterHelper.BuildUuid(dim.Uuid, dim.UuidIsSymbol));

        if (dim.Points.Count > 0)
            db.AddChild(WriterHelper.BuildPoints(dim.Points));

        if (dim.Height.HasValue)
            db.AddChild("height", h => h.AddValue(dim.Height.Value));

        if (dim.Orientation.HasValue)
            db.AddChild("orientation", o => o.AddValue(dim.Orientation.Value));

        if (dim.LeaderLength.HasValue)
            db.AddChild("leader_length", l => l.AddValue(dim.LeaderLength.Value));

        // Format
        if (dim.HasFormat)
        {
            db.AddChild("format", f =>
            {
                if (dim.FormatPrefix is not null)
                    f.AddChild("prefix", p => p.AddValue(dim.FormatPrefix));
                if (dim.FormatSuffix is not null)
                    f.AddChild("suffix", s => s.AddValue(dim.FormatSuffix));
                if (dim.FormatUnits.HasValue)
                    f.AddChild("units", u => u.AddValue(dim.FormatUnits.Value));
                if (dim.FormatUnitsFormat.HasValue)
                    f.AddChild("units_format", uf => uf.AddValue(dim.FormatUnitsFormat.Value));
                if (dim.FormatPrecision.HasValue)
                    f.AddChild("precision", p => p.AddValue(dim.FormatPrecision.Value));
                if (dim.FormatOverrideValue is not null)
                    f.AddChild("override_value", ov => ov.AddValue(dim.FormatOverrideValue));
                if (dim.FormatSuppressZeroes.HasValue)
                    f.AddChild("suppress_zeroes", sz => sz.AddBool(dim.FormatSuppressZeroes.Value));
            });
        }

        // Style
        if (dim.HasStyle)
        {
            db.AddChild("style", s =>
            {
                s.AddChild("thickness", t => t.AddMm(dim.StyleThickness));
                if (dim.StyleArrowLength.HasValue)
                    s.AddChild("arrow_length", a => a.AddValue(dim.StyleArrowLength.Value));
                if (dim.StyleTextPositionMode.HasValue)
                    s.AddChild("text_position_mode", m => m.AddValue(dim.StyleTextPositionMode.Value));
                if (dim.StyleArrowDirection is not null)
                    s.AddChild("arrow_direction", d => d.AddSymbol(dim.StyleArrowDirection));
                if (dim.StyleExtensionHeight.HasValue)
                    s.AddChild("extension_height", h => h.AddValue(dim.StyleExtensionHeight.Value));
                if (dim.StyleTextFrame.HasValue)
                    s.AddChild("text_frame", tf => tf.AddValue(dim.StyleTextFrame.Value));
                if (dim.StyleExtensionOffset.HasValue)
                    s.AddChild("extension_offset", eo => eo.AddValue(dim.StyleExtensionOffset.Value));
                if (dim.StyleKeepTextAligned.HasValue)
                    s.AddChild("keep_text_aligned", k => k.AddBool(dim.StyleKeepTextAligned.Value));
            });
        }

        // Embedded gr_text
        if (dim.Text is not null)
            db.AddChild(BuildGrText(dim.Text));

        if (dim.IsLocked && dim.LockedIsChildNode)
            db.AddChild("locked", l => l.AddBool(true));

        return db.Build();
    }

    internal static SExpr BuildGeneratedElement(KiCadPcbGeneratedElement gen)
    {
        var gb = new SExpressionBuilder("generated");

        if (gen.Uuid is not null)
            gb.AddChild(WriterHelper.BuildUuid(gen.Uuid));

        if (gen.GeneratedType is not null)
            gb.AddChild("type", t => t.AddSymbol(gen.GeneratedType));

        if (gen.Name is not null)
            gb.AddChild("name", n => n.AddValue(gen.Name));

        if (gen.LayerName is not null)
            gb.AddChild("layer", l => l.AddValue(gen.LayerName));

        // Base line
        if (gen.BaseLinePoints.Count > 0)
        {
            gb.AddChild("base_line", bl =>
            {
                bl.AddChild(WriterHelper.BuildPoints(gen.BaseLinePoints));
            });
        }

        // Coupled base line
        if (gen.BaseLineCoupledPoints.Count > 0)
        {
            gb.AddChild("base_line_coupled", blc =>
            {
                blc.AddChild(WriterHelper.BuildPoints(gen.BaseLineCoupledPoints));
            });
        }

        // Build sorted list of property-like entries including origin/end
        var entries = new List<(string Key, Action<SExpressionBuilder> Emit)>();

        foreach (var (key, value, isSymbol) in gen.Properties)
        {
            var k = key;
            var v = value;
            var isSym = isSymbol;
            entries.Add((k, p =>
            {
                if (isSym)
                    p.AddSymbol(v);
                else
                    p.AddValue(v);
            }));
        }

        if (gen.End.HasValue)
        {
            var endVal = gen.End.Value;
            entries.Add(("end", e =>
            {
                e.AddChild(WriterHelper.BuildXY(endVal));
            }));
        }

        if (gen.Origin.HasValue)
        {
            var originVal = gen.Origin.Value;
            entries.Add(("origin", o =>
            {
                o.AddChild(WriterHelper.BuildXY(originVal));
            }));
        }

        // Emit all entries in alphabetical order (KiCad sorts them)
        foreach (var (key, emit) in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            gb.AddChild(key, emit);
        }

        // Members
        if (gen.Members.Count > 0)
        {
            gb.AddChild("members", m =>
            {
                foreach (var member in gen.Members)
                    m.AddValue(member);
            });
        }

        return gb.Build();
    }
}
