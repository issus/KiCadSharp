using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Writes KiCad footprint files (<c>.kicad_mod</c>) from <see cref="KiCadPcbComponent"/> objects.
/// </summary>
public static class FootprintWriter
{
    /// <summary>
    /// Writes a footprint to a file path.
    /// </summary>
    /// <param name="component">The footprint component to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcbComponent component, string path, CancellationToken ct = default)
    {
        var expr = BuildFootprint(component);
        await SExpressionWriter.WriteAsync(expr, path, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a footprint to a stream.
    /// </summary>
    /// <param name="component">The footprint component to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcbComponent component, Stream stream, CancellationToken ct = default)
    {
        var expr = BuildFootprint(component);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    internal static SExpr BuildFootprint(KiCadPcbComponent component)
    {
        var b = new SExpressionBuilder("footprint").AddValue(component.Name);

        // Standalone .kicad_mod file metadata (only emitted when present)
        if (component.Version is not null)
            b.AddChild("version", v => v.AddValue((double)component.Version.Value));
        if (component.Generator is not null)
            b.AddChild("generator", g => g.AddValue(component.Generator));
        if (component.GeneratorVersion is not null)
            b.AddChild("generator_version", g => g.AddValue(component.GeneratorVersion));

        if (component.UsesChildNodeFlags)
        {
            if (component.IsLocked)
                b.AddChild("locked", l => l.AddBool(true));
            if (component.IsPlaced)
                b.AddChild("placed", p => p.AddBool(true));
        }
        else
        {
            if (component.IsLocked)
                b.AddSymbol("locked");
            if (component.IsPlaced)
                b.AddSymbol("placed");
        }

        if (component.LayerName is not null)
            b.AddChild("layer", l => l.AddValue(component.LayerName));

        if (component.Tedit is not null)
            b.AddChild("tedit", t => t.AddValue(component.Tedit));

        if (component.Uuid is not null)
            b.AddChild(WriterHelper.BuildUuid(component.Uuid));

        if (component.Location != CoordPoint.Zero || component.Rotation != 0)
            b.AddChild(WriterHelper.BuildPositionCompact(component.Location, component.Rotation));

        if (component.Description is not null)
            b.AddChild("descr", d => d.AddValue(component.Description));

        if (component.Tags is not null)
            b.AddChild("tags", t => t.AddValue(component.Tags));

        // Properties
        foreach (var prop in component.Properties)
        {
            b.AddChild(SymLibWriter.BuildProperty(prop));
        }

        // Component classes (raw)
        if (component.ComponentClassesRaw is not null)
            b.AddChild(component.ComponentClassesRaw);

        if (component.Path is not null)
            b.AddChild("path", p => p.AddValue(component.Path));

        if (component.SheetName is not null)
            b.AddChild("sheetname", s => s.AddValue(component.SheetName));

        if (component.SheetFile is not null)
            b.AddChild("sheetfile", s => s.AddValue(component.SheetFile));

        // Clearances
        if (component.Clearance != Coord.Zero)
            b.AddChild("clearance", c => c.AddMm(component.Clearance));
        if (component.SolderMaskMargin != Coord.Zero)
            b.AddChild("solder_mask_margin", c => c.AddMm(component.SolderMaskMargin));
        if (component.SolderPasteMargin != Coord.Zero)
            b.AddChild("solder_paste_margin", c => c.AddMm(component.SolderPasteMargin));
        if (component.SolderPasteRatio != 0)
            b.AddChild("solder_paste_ratio", c => c.AddValue(component.SolderPasteRatio));
        if (component.SolderPasteMarginRatio.HasValue)
            b.AddChild("solder_paste_margin_ratio", c => c.AddValue(component.SolderPasteMarginRatio.Value));
        if (component.ThermalWidth != Coord.Zero)
            b.AddChild("thermal_width", c => c.AddMm(component.ThermalWidth));
        if (component.ThermalGap != Coord.Zero)
            b.AddChild("thermal_gap", c => c.AddMm(component.ThermalGap));
        if (component.ZoneConnect != ZoneConnectionType.Inherited)
            b.AddChild("zone_connect", c => c.AddValue((int)component.ZoneConnect));
        if (component.AutoplaceCost90 != 0)
            b.AddChild("autoplace_cost90", c => c.AddValue(component.AutoplaceCost90));
        if (component.AutoplaceCost180 != 0)
            b.AddChild("autoplace_cost180", c => c.AddValue(component.AutoplaceCost180));

        // Attributes
        if (component.Attributes != FootprintAttribute.None)
        {
            b.AddChild("attr", a =>
            {
                if (component.Attributes.HasFlag(FootprintAttribute.Smd)) a.AddSymbol("smd");
                if (component.Attributes.HasFlag(FootprintAttribute.ThroughHole)) a.AddSymbol("through_hole");
                if (component.Attributes.HasFlag(FootprintAttribute.BoardOnly)) a.AddSymbol("board_only");
                if (component.Attributes.HasFlag(FootprintAttribute.ExcludeFromPosFiles)) a.AddSymbol("exclude_from_pos_files");
                if (component.Attributes.HasFlag(FootprintAttribute.ExcludeFromBom)) a.AddSymbol("exclude_from_bom");
                if (component.Attributes.HasFlag(FootprintAttribute.AllowMissingCourtyard)) a.AddSymbol("allow_missing_courtyard");
                if (component.Attributes.HasFlag(FootprintAttribute.Dnp)) a.AddSymbol("dnp");
                if (component.Attributes.HasFlag(FootprintAttribute.AllowSoldermaskBridges)) a.AddSymbol("allow_soldermask_bridges");
            });
        }

        if (component.DuplicatePadNumbersAreJumpers.HasValue)
            b.AddChild("duplicate_pad_numbers_are_jumpers", d => d.AddBool(component.DuplicatePadNumbersAreJumpers.Value));

        // Private layers
        if (component.PrivateLayersRaw is not null)
            b.AddChild(component.PrivateLayersRaw);

        // Net tie pad groups
        if (component.NetTiePadGroupsRaw is not null)
            b.AddChild(component.NetTiePadGroupsRaw);

        // Graphical items — use original order if available, otherwise group by type
        if (component.GraphicalItemOrder.Count > 0)
        {
            foreach (var item in component.GraphicalItemOrder)
            {
                switch (item)
                {
                    case KiCadPcbTrack track: b.AddChild(BuildFpLine(track)); break;
                    case KiCadPcbRectangle rect: b.AddChild(BuildFpRect(rect)); break;
                    case KiCadPcbCircle circle: b.AddChild(BuildFpCircle(circle)); break;
                    case KiCadPcbArc arc: b.AddChild(BuildFpArc(arc)); break;
                    case KiCadPcbPolygon poly: b.AddChild(BuildFpPoly(poly)); break;
                    case KiCadPcbCurve curve: b.AddChild(BuildFpCurve(curve)); break;
                }
            }
        }
        else
        {
            foreach (var track in component.Tracks.OfType<KiCadPcbTrack>())
                b.AddChild(BuildFpLine(track));
            foreach (var rect in component.Rectangles)
                b.AddChild(BuildFpRect(rect));
            foreach (var circle in component.Circles)
                b.AddChild(BuildFpCircle(circle));
            foreach (var arc in component.Arcs.OfType<KiCadPcbArc>())
                b.AddChild(BuildFpArc(arc));
            foreach (var poly in component.Polygons)
                b.AddChild(BuildFpPoly(poly));
            foreach (var curve in component.Curves)
                b.AddChild(BuildFpCurve(curve));
        }

        // Texts
        foreach (var text in component.Texts.OfType<KiCadPcbText>())
        {
            b.AddChild(BuildFpText(text));
        }

        // Text private (raw)
        foreach (var tp in component.TextPrivateRaw)
        {
            b.AddChild(tp);
        }

        // Text boxes (raw)
        foreach (var tb2 in component.TextBoxesRaw)
        {
            b.AddChild(tb2);
        }

        // Pads
        foreach (var pad in component.Pads.OfType<KiCadPcbPad>())
        {
            b.AddChild(BuildPad(pad));
        }

        // Teardrop (raw)
        if (component.TeardropRaw is not null)
            b.AddChild(component.TeardropRaw);

        // Dimensions within footprint (raw)
        foreach (var dim in component.DimensionsRaw)
        {
            b.AddChild(dim);
        }

        // Zones within footprint (raw)
        foreach (var zone in component.ZonesRaw)
        {
            b.AddChild(zone);
        }

        // Groups within footprint (raw)
        foreach (var group in component.GroupsRaw)
        {
            b.AddChild(group);
        }

        // Embedded fonts (KiCad 8+) — emit after groups, before embedded_files
        if (component.EmbeddedFonts.HasValue)
            b.AddChild("embedded_fonts", e => e.AddBool(component.EmbeddedFonts.Value));

        // Embedded files (raw)
        if (component.EmbeddedFilesRaw is not null)
            b.AddChild(component.EmbeddedFilesRaw);

        // 3D models (prefer the list, fall back to single model)
        if (component.Models3D.Count > 0)
        {
            foreach (var model in component.Models3D)
            {
                b.AddChild(Build3DModel(model));
            }
        }
        else if (component.Model3D is not null)
        {
            b.AddChild("model", m =>
            {
                m.AddValue(component.Model3D);
                m.AddChild("offset", o => o.AddChild("xyz", xyz =>
                {
                    xyz.AddMm(component.Model3DOffset.X);
                    xyz.AddMm(component.Model3DOffset.Y);
                    xyz.AddValue(component.Model3DOffsetZ);
                }));
                m.AddChild("scale", s => s.AddChild("xyz", xyz =>
                {
                    xyz.AddMm(component.Model3DScale.X);
                    xyz.AddMm(component.Model3DScale.Y);
                    xyz.AddValue(component.Model3DScaleZ);
                }));
                m.AddChild("rotate", r => r.AddChild("xyz", xyz =>
                {
                    xyz.AddMm(component.Model3DRotation.X);
                    xyz.AddMm(component.Model3DRotation.Y);
                    xyz.AddValue(component.Model3DRotationZ);
                }));
            });
        }

        return b.Build();
    }

    private static SExpr BuildFpText(KiCadPcbText text)
    {
        var tb = new SExpressionBuilder("fp_text")
            .AddSymbol(text.TextType ?? "user")
            .AddValue(text.Text);

        if (text.IsHidden && !text.HideIsChildNode)
            tb.AddSymbol("hide");

        if (text.IsUnlocked && !text.UnlockedIsChildNode)
            tb.AddSymbol("unlocked");

        tb.AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));

        if (text.IsUnlocked && text.UnlockedIsChildNode)
            tb.AddChild("unlocked", u => u.AddBool(true));

        if (text.LayerName is not null)
            tb.AddChild("layer", l => l.AddValue(text.LayerName));

        if (text.IsHidden && text.HideIsChildNode)
            tb.AddChild("hide", h => h.AddBool(true));

        if (text.IsKnockout)
            tb.AddSymbol("knockout");

        if (text.Uuid is not null)
            tb.AddChild(WriterHelper.BuildUuid(text.Uuid));

        var fontW = text.FontWidth != Coord.Zero ? text.FontWidth : text.Height;
        tb.AddChild(WriterHelper.BuildTextEffects(
            text.Height, fontW,
            justification: text.Justification,
            isMirrored: text.IsMirrored,
            isBold: text.FontBold,
            isItalic: text.FontItalic,
            fontFace: text.FontName,
            fontThickness: text.FontThickness,
            fontColor: text.FontColor));

        // Render cache (raw)
        if (text.RenderCache is not null)
            tb.AddChild(text.RenderCache);

        return tb.Build();
    }

    private static SExpr BuildFpLine(KiCadPcbTrack track)
    {
        var lb = new SExpressionBuilder("fp_line");
        if (track.IsLocked)
            lb.AddSymbol("locked");
        lb.AddChild("start", s => { s.AddMm(track.Start.X); s.AddMm(track.Start.Y); })
            .AddChild("end", e => { e.AddMm(track.End.X); e.AddMm(track.End.Y); })
            .AddChild(WriterHelper.BuildStroke(track.Width, track.StrokeStyle, track.StrokeColor));

        if (track.UsePcbFillFormat)
            lb.AddChild(WriterHelper.BuildPcbFill(track.FillType));
        else if (track.FillType != SchFillType.None)
            lb.AddChild(WriterHelper.BuildFill(track.FillType, track.FillColor));

        if (track.LayerName is not null)
            lb.AddChild("layer", l => l.AddValue(track.LayerName));

        if (track.Uuid is not null)
            lb.AddChild(WriterHelper.BuildUuid(track.Uuid));

        return lb.Build();
    }

    private static SExpr BuildFpRect(KiCadPcbRectangle rect)
    {
        var rb = new SExpressionBuilder("fp_rect");
        if (rect.IsLocked)
            rb.AddSymbol("locked");
        rb.AddChild("start", s => { s.AddMm(rect.Start.X); s.AddMm(rect.Start.Y); })
            .AddChild("end", e => { e.AddMm(rect.End.X); e.AddMm(rect.End.Y); })
            .AddChild(WriterHelper.BuildStroke(rect.Width, rect.StrokeStyle, rect.StrokeColor));

        if (rect.UsePcbFillFormat)
            rb.AddChild(WriterHelper.BuildPcbFill(rect.FillType));
        else if (rect.FillType != SchFillType.None)
            rb.AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor));

        if (rect.LayerName is not null)
            rb.AddChild("layer", l => l.AddValue(rect.LayerName));

        if (rect.Uuid is not null)
            rb.AddChild(WriterHelper.BuildUuid(rect.Uuid));

        return rb.Build();
    }

    private static SExpr BuildFpCircle(KiCadPcbCircle circle)
    {
        var cb = new SExpressionBuilder("fp_circle");
        if (circle.IsLocked)
            cb.AddSymbol("locked");
        cb.AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
            .AddChild("end", e => { e.AddMm(circle.End.X); e.AddMm(circle.End.Y); })
            .AddChild(WriterHelper.BuildStroke(circle.Width, circle.StrokeStyle, circle.StrokeColor));

        if (circle.UsePcbFillFormat)
            cb.AddChild(WriterHelper.BuildPcbFill(circle.FillType));
        else if (circle.FillType != SchFillType.None)
            cb.AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor));

        if (circle.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(circle.LayerName));

        if (circle.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuid(circle.Uuid));

        return cb.Build();
    }

    private static SExpr BuildFpArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("fp_arc");
        if (arc.IsLocked)
            ab.AddSymbol("locked");
        ab.AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
            .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
            .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
            .AddChild(WriterHelper.BuildStroke(arc.Width, arc.StrokeStyle, arc.StrokeColor));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddValue(arc.LayerName));

        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuid(arc.Uuid));

        return ab.Build();
    }

    internal static SExpr BuildPad(KiCadPcbPad pad)
    {
        var pb = new SExpressionBuilder("pad")
            .AddValue(pad.Designator ?? "")
            .AddSymbol(SExpressionHelper.PadTypeToString(pad.PadType))
            .AddSymbol(SExpressionHelper.PadShapeToString(pad.Shape));

        if (pad.IsLocked)
            pb.AddSymbol("locked");

        pb.AddChild(WriterHelper.BuildPositionCompact(pad.Location, pad.Rotation));
        pb.AddChild("size", s =>
        {
            s.AddMm(pad.Size.X);
            s.AddMm(pad.Size.Y);
        });

        // rect_delta (raw) - must come after size, before drill
        if (pad.RectDeltaRaw is not null)
            pb.AddChild(pad.RectDeltaRaw);

        if (pad.HoleSize != Coord.Zero)
        {
            if (pad.HoleType == PadHoleType.Slot)
            {
                pb.AddChild("drill", d =>
                {
                    d.AddSymbol("oval");
                    d.AddMm(pad.HoleSize);
                    if (pad.DrillSizeY != Coord.Zero)
                        d.AddMm(pad.DrillSizeY);
                    if (pad.DrillOffset != CoordPoint.Zero)
                        d.AddChild("offset", o => { o.AddMm(pad.DrillOffset.X); o.AddMm(pad.DrillOffset.Y); });
                });
            }
            else
            {
                pb.AddChild("drill", d =>
                {
                    d.AddMm(pad.HoleSize);
                    if (pad.DrillOffset != CoordPoint.Zero)
                        d.AddChild("offset", o => { o.AddMm(pad.DrillOffset.X); o.AddMm(pad.DrillOffset.Y); });
                });
            }
        }

        // pad property (e.g. pad_prop_heatsink) - must come after drill, before layers
        if (pad.PadProperty is not null)
            pb.AddChild("property", p => p.AddSymbol(pad.PadProperty));

        if (pad.Layers.Count > 0)
        {
            pb.AddChild("layers", l =>
            {
                foreach (var layer in pad.Layers)
                    l.AddValue(layer);
            });
        }

        if (pad.HasRemoveUnusedLayers)
            pb.AddChild("remove_unused_layers", r => r.AddBool(pad.RemoveUnusedLayers));
        else if (pad.RemoveUnusedLayers)
            pb.AddChild("remove_unused_layers", _ => { });
        if (pad.HasKeepEndLayers)
            pb.AddChild("keep_end_layers", k => k.AddBool(pad.KeepEndLayers));
        else if (pad.KeepEndLayers)
            pb.AddChild("keep_end_layers", _ => { });

        if (pad.HasRoundRectRatio)
            pb.AddChild("roundrect_rratio", r => r.AddValue(pad.RoundRectRatio));
        else if (pad.RoundRectRatio > 0)
            pb.AddChild("roundrect_rratio", r => r.AddValue(pad.RoundRectRatio));
        else if (pad.CornerRadiusPercentage > 0)
            pb.AddChild("roundrect_rratio", r => r.AddValue(pad.CornerRadiusPercentage / 100.0));

        // Chamfer (must come before net/pinfunction/pintype per KiCad ordering)
        if (pad.ChamferRatio > 0)
            pb.AddChild("chamfer_ratio", c => c.AddValue(pad.ChamferRatio));
        if (pad.ChamferCorners.Length > 0)
        {
            pb.AddChild("chamfer", c =>
            {
                foreach (var corner in pad.ChamferCorners)
                    c.AddSymbol(corner);
            });
        }

        if (pad.Net > 0 || pad.NetName is not null)
        {
            pb.AddChild("net", n =>
            {
                n.AddValue(pad.Net);
                if (pad.NetName is not null) n.AddValue(pad.NetName);
            });
        }

        if (pad.PinFunction is not null)
            pb.AddChild("pinfunction", p => p.AddValue(pad.PinFunction));
        if (pad.PinType is not null)
            pb.AddChild("pintype", p => p.AddValue(pad.PinType));

        if (pad.SolderMaskExpansion != Coord.Zero)
            pb.AddChild("solder_mask_margin", c => c.AddMm(pad.SolderMaskExpansion));
        if (pad.Clearance != Coord.Zero)
            pb.AddChild("clearance", c => c.AddMm(pad.Clearance));
        if (pad.SolderPasteMargin != Coord.Zero)
            pb.AddChild("solder_paste_margin", c => c.AddMm(pad.SolderPasteMargin));
        if (pad.SolderPasteRatio != 0)
            pb.AddChild("solder_paste_margin_ratio", c => c.AddValue(pad.SolderPasteRatio));
        if (pad.ThermalWidth != Coord.Zero)
            pb.AddChild("thermal_width", c => c.AddMm(pad.ThermalWidth));
        if (pad.DieLength != Coord.Zero)
            pb.AddChild("die_length", c => c.AddMm(pad.DieLength));
        if (pad.HasZoneConnect || pad.ZoneConnect != ZoneConnectionType.Inherited)
            pb.AddChild("zone_connect", c => c.AddValue((int)pad.ZoneConnect));
        if (pad.ThermalBridgeWidth != Coord.Zero)
            pb.AddChild("thermal_bridge_width", c => c.AddMm(pad.ThermalBridgeWidth));
        if (pad.HasThermalBridgeAngle || pad.ThermalBridgeAngle != 0)
            pb.AddChild("thermal_bridge_angle", c => c.AddValue(pad.ThermalBridgeAngle));
        if (pad.ThermalGap != Coord.Zero)
            pb.AddChild("thermal_gap", c => c.AddMm(pad.ThermalGap));

        // Custom pad options (raw)
        if (pad.OptionsRaw is not null)
            pb.AddChild(pad.OptionsRaw);

        // Custom pad primitives (raw)
        if (pad.PrimitivesRaw is not null)
            pb.AddChild(pad.PrimitivesRaw);

        // Per-pad tenting (raw) - after options/primitives
        if (pad.TentingRaw is not null)
            pb.AddChild(pad.TentingRaw);

        // Per-pad teardrops (raw)
        if (pad.TeardropsRaw is not null)
            pb.AddChild(pad.TeardropsRaw);

        if (pad.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuid(pad.Uuid));

        return pb.Build();
    }

    private static SExpr BuildFpPoly(KiCadPcbPolygon poly)
    {
        var pb = new SExpressionBuilder("fp_poly");
        if (poly.IsLocked)
            pb.AddSymbol("locked");

        pb.AddChild(WriterHelper.BuildPoints(poly.Points));
        pb.AddChild(WriterHelper.BuildStroke(poly.Width, poly.StrokeStyle, poly.StrokeColor));

        if (poly.UsePcbFillFormat)
            pb.AddChild(WriterHelper.BuildPcbFill(poly.FillType));
        else if (poly.FillType != SchFillType.None)
            pb.AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor));

        if (poly.LayerName is not null)
            pb.AddChild("layer", l => l.AddValue(poly.LayerName));

        if (poly.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuid(poly.Uuid));

        return pb.Build();
    }

    private static SExpr BuildFpCurve(KiCadPcbCurve curve)
    {
        var cb = new SExpressionBuilder("fp_curve");
        if (curve.IsLocked)
            cb.AddSymbol("locked");

        cb.AddChild(WriterHelper.BuildPoints(curve.Points));
        cb.AddChild(WriterHelper.BuildStroke(curve.Width, curve.StrokeStyle, curve.StrokeColor));

        if (curve.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(curve.LayerName));

        if (curve.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuid(curve.Uuid));

        return cb.Build();
    }

    private static SExpr Build3DModel(KiCadPcb3DModel model)
    {
        var mb = new SExpressionBuilder("model").AddValue(model.Path);
        if (model.IsHidden)
            mb.AddChild("hide", h => h.AddBool(true));
        mb.AddChild("offset", o => o.AddChild("xyz", xyz =>
        {
            xyz.AddMm(model.Offset.X);
            xyz.AddMm(model.Offset.Y);
            xyz.AddValue(model.OffsetZ);
        }));
        mb.AddChild("scale", s => s.AddChild("xyz", xyz =>
        {
            xyz.AddMm(model.Scale.X);
            xyz.AddMm(model.Scale.Y);
            xyz.AddValue(model.ScaleZ);
        }));
        mb.AddChild("rotate", r => r.AddChild("xyz", xyz =>
        {
            xyz.AddMm(model.Rotation.X);
            xyz.AddMm(model.Rotation.Y);
            xyz.AddValue(model.RotationZ);
        }));
        return mb.Build();
    }
}
