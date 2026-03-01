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
        var b = new SExpressionBuilder(component.RootToken).AddValue(component.Name);

        // Standalone .kicad_mod file metadata (only emitted when present)
        if (component.Version is not null)
            b.AddChild("version", v => v.AddValue((double)component.Version.Value));
        if (component.Generator is not null)
        {
            b.AddChild("generator", g =>
            {
                if (component.GeneratorIsSymbol)
                    g.AddSymbol(component.Generator);
                else
                    g.AddValue(component.Generator);
            });
        }
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

        var uuidTok = component.UuidToken;
        var uuidSym = component.UuidIsSymbol;

        if (component.Uuid is not null)
            b.AddChild(WriterHelper.BuildUuidToken(component.Uuid, uuidTok, uuidSym));

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

        // Component classes (KiCad 9+) — must come after properties, before path
        if (component.ComponentClasses.Count > 0)
        {
            b.AddChild("component_classes", cc =>
            {
                foreach (var cls in component.ComponentClasses)
                    cc.AddChild("class", c => c.AddValue(cls));
            });
        }

        if (component.Path is not null)
            b.AddChild("path", p => p.AddValue(component.Path));

        if (component.SheetName is not null)
            b.AddChild("sheetname", s => s.AddValue(component.SheetName));

        if (component.SheetFile is not null)
            b.AddChild("sheetfile", s => s.AddValue(component.SheetFile));

        // Clearances
        if (component.HasClearance || component.Clearance != Coord.Zero)
            b.AddChild("clearance", c => c.AddMm(component.Clearance));
        if (component.HasSolderMaskMargin || component.SolderMaskMargin != Coord.Zero)
            b.AddChild("solder_mask_margin", c => c.AddMm(component.SolderMaskMargin));
        if (component.HasSolderPasteMargin || component.SolderPasteMargin != Coord.Zero)
            b.AddChild("solder_paste_margin", c => c.AddMm(component.SolderPasteMargin));
        if (component.SolderPasteRatio != 0)
            b.AddChild("solder_paste_ratio", c => c.AddValue(component.SolderPasteRatio));
        if (component.SolderPasteMarginRatio.HasValue)
            b.AddChild("solder_paste_margin_ratio", c => c.AddValue(component.SolderPasteMarginRatio.Value));
        if (component.HasThermalWidth || component.ThermalWidth != Coord.Zero)
            b.AddChild("thermal_width", c => c.AddMm(component.ThermalWidth));
        if (component.HasThermalGap || component.ThermalGap != Coord.Zero)
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

        // Net tie pad groups — must come after attr, before graphical items
        if (component.NetTiePadGroups.Count > 0)
        {
            b.AddChild("net_tie_pad_groups", n =>
            {
                foreach (var group in component.NetTiePadGroups)
                    n.AddValue(group);
            });
        }

        // Graphical items (lines, rects, circles, arcs, polygons, curves, texts) — use original order if available
        if (component.GraphicalItemOrder.Count > 0)
        {
            foreach (var item in component.GraphicalItemOrder)
            {
                switch (item)
                {
                    case KiCadPcbTrack track: b.AddChild(BuildFpLine(track, uuidTok, uuidSym)); break;
                    case KiCadPcbRectangle rect: b.AddChild(BuildFpRect(rect, uuidTok, uuidSym)); break;
                    case KiCadPcbCircle circle: b.AddChild(BuildFpCircle(circle, uuidTok, uuidSym)); break;
                    case KiCadPcbArc arc: b.AddChild(BuildFpArc(arc, uuidTok, uuidSym)); break;
                    case KiCadPcbPolygon poly: b.AddChild(BuildFpPoly(poly, uuidTok, uuidSym)); break;
                    case KiCadPcbCurve curve: b.AddChild(BuildFpCurve(curve, uuidTok, uuidSym)); break;
                    case KiCadPcbText text: b.AddChild(BuildFpText(text, uuidTok, uuidSym)); break;
                }
            }
        }
        else
        {
            foreach (var text in component.Texts.OfType<KiCadPcbText>())
                b.AddChild(BuildFpText(text, uuidTok, uuidSym));
            foreach (var track in component.Tracks.OfType<KiCadPcbTrack>())
                b.AddChild(BuildFpLine(track, uuidTok, uuidSym));
            foreach (var rect in component.Rectangles)
                b.AddChild(BuildFpRect(rect, uuidTok, uuidSym));
            foreach (var circle in component.Circles)
                b.AddChild(BuildFpCircle(circle, uuidTok, uuidSym));
            foreach (var arc in component.Arcs.OfType<KiCadPcbArc>())
                b.AddChild(BuildFpArc(arc, uuidTok, uuidSym));
            foreach (var poly in component.Polygons)
                b.AddChild(BuildFpPoly(poly, uuidTok, uuidSym));
            foreach (var curve in component.Curves)
                b.AddChild(BuildFpCurve(curve, uuidTok, uuidSym));
        }

        // Pads
        foreach (var pad in component.Pads.OfType<KiCadPcbPad>())
        {
            b.AddChild(BuildPad(pad, uuidTok, uuidSym));
        }

        // Private layers
        if (component.PrivateLayers.Count > 0)
        {
            b.AddChild("private_layers", p =>
            {
                foreach (var layer in component.PrivateLayers)
                    p.AddValue(layer);
            });
        }

        // Zones
        foreach (var zone in component.Zones)
            b.AddChild(PcbWriter.BuildZoneStructured(zone));

        // Groups
        foreach (var group in component.Groups)
            b.AddChild(PcbWriter.BuildGroup(group));

        // Dimensions
        foreach (var dim in component.Dimensions)
            b.AddChild(PcbWriter.BuildDimension(dim));

        // Embedded fonts (KiCad 8+)
        if (component.EmbeddedFonts.HasValue)
            b.AddChild("embedded_fonts", e => e.AddBool(component.EmbeddedFonts.Value));

        // Embedded files
        if (component.EmbeddedFiles is not null)
            b.AddChild(PcbWriter.BuildEmbeddedFiles(component.EmbeddedFiles));

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
                    xyz.AddValue(component.Model3DScaleX);
                    xyz.AddValue(component.Model3DScaleY);
                    xyz.AddValue(component.Model3DScaleZ);
                }));
                m.AddChild("rotate", r => r.AddChild("xyz", xyz =>
                {
                    xyz.AddValue(component.Model3DRotationX);
                    xyz.AddValue(component.Model3DRotationY);
                    xyz.AddValue(component.Model3DRotationZ);
                }));
            });
        }

        return b.Build();
    }

    private static SExpr BuildFpText(KiCadPcbText text, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var tb = new SExpressionBuilder("fp_text")
            .AddSymbol(text.TextType ?? "user")
            .AddValue(text.Text);

        if (text.IsHidden && !text.HideIsChildNode)
            tb.AddSymbol("hide");

        if (text.IsUnlocked && !text.UnlockedIsChildNode && !text.UnlockedInAtNode)
            tb.AddSymbol("unlocked");

        // Use compact position (omit angle when 0) if the original didn't include it
        if (text.UnlockedInAtNode)
        {
            // KiCad 7+ format: unlocked keyword inside at node
            var atb = new SExpressionBuilder("at")
                .AddMm(text.Location.X)
                .AddMm(text.Location.Y);
            if (text.PositionIncludesAngle)
                atb.AddValue(text.Rotation);
            atb.AddSymbol("unlocked");
            tb.AddChild(atb.Build());
        }
        else if (text.PositionIncludesAngle)
            tb.AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));
        else
            tb.AddChild(WriterHelper.BuildPositionCompact(text.Location, text.Rotation));

        if (text.IsUnlocked && !text.UnlockedInAtNode && text.UnlockedIsChildNode)
            tb.AddChild("unlocked", u => u.AddBool(true));

        if (text.LayerName is not null)
            tb.AddChild("layer", l =>
            {
                l.AddValue(text.LayerName);
                if (text.IsKnockout)
                    l.AddSymbol("knockout");
            });

        if (text.IsHidden && text.HideIsChildNode)
            tb.AddChild("hide", h => h.AddBool(true));

        // UUID/tstamp before or after effects depending on original format
        if (text.Uuid is not null && !text.UuidAfterEffects)
            tb.AddChild(WriterHelper.BuildUuidToken(text.Uuid, uuidToken, uuidIsSymbol));

        var fontW = text.FontWidth != Coord.Zero ? text.FontWidth : text.Height;
        tb.AddChild(WriterHelper.BuildTextEffects(
            text.Height, fontW,
            justification: text.Justification,
            isMirrored: text.IsMirrored,
            isBold: text.FontBold,
            isItalic: text.FontItalic,
            fontFace: text.FontName,
            fontThickness: text.FontThickness,
            fontColor: text.FontColor,
            boldIsSymbol: text.BoldIsSymbol,
            italicIsSymbol: text.ItalicIsSymbol));

        if (text.Uuid is not null && text.UuidAfterEffects)
            tb.AddChild(WriterHelper.BuildUuidToken(text.Uuid, uuidToken, uuidIsSymbol));

        if (text.RenderCache is not null)
            tb.AddChild(PcbWriter.BuildRenderCache(text.RenderCache));

        return tb.Build();
    }

    private static SExpr BuildFpLine(KiCadPcbTrack track, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var lb = new SExpressionBuilder("fp_line");
        if (track.IsLocked && !track.LockedIsChildNode)
            lb.AddSymbol("locked");
        lb.AddChild("start", s => { s.AddMm(track.Start.X); s.AddMm(track.Start.Y); })
            .AddChild("end", e => { e.AddMm(track.End.X); e.AddMm(track.End.Y); })
            .AddChild(WriterHelper.BuildStroke(track.Width, track.StrokeStyle, track.StrokeColor));

        if (track.UsePcbFillFormat)
            lb.AddChild(WriterHelper.BuildPcbFill(track.FillType));
        else if (track.FillType != SchFillType.None)
            lb.AddChild(WriterHelper.BuildFill(track.FillType, track.FillColor));

        if (track.IsLocked && track.LockedIsChildNode)
            lb.AddChild("locked", l => l.AddBool(true));

        if (track.LayerName is not null)
            lb.AddChild("layer", l => l.AddValue(track.LayerName));

        if (track.Uuid is not null)
            lb.AddChild(WriterHelper.BuildUuidToken(track.Uuid, uuidToken, uuidIsSymbol));

        return lb.Build();
    }

    private static SExpr BuildFpRect(KiCadPcbRectangle rect, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var rb = new SExpressionBuilder("fp_rect");
        if (rect.IsLocked && !rect.LockedIsChildNode)
            rb.AddSymbol("locked");
        rb.AddChild("start", s => { s.AddMm(rect.Start.X); s.AddMm(rect.Start.Y); })
            .AddChild("end", e => { e.AddMm(rect.End.X); e.AddMm(rect.End.Y); })
            .AddChild(WriterHelper.BuildStroke(rect.Width, rect.StrokeStyle, rect.StrokeColor));

        if (rect.UsePcbFillFormat)
            rb.AddChild(WriterHelper.BuildPcbFill(rect.FillType));
        else if (rect.FillType != SchFillType.None)
            rb.AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor));

        if (rect.IsLocked && rect.LockedIsChildNode)
            rb.AddChild("locked", l => l.AddBool(true));

        if (rect.LayerName is not null)
            rb.AddChild("layer", l => l.AddValue(rect.LayerName));

        if (rect.Uuid is not null)
            rb.AddChild(WriterHelper.BuildUuidToken(rect.Uuid, uuidToken, uuidIsSymbol));

        return rb.Build();
    }

    private static SExpr BuildFpCircle(KiCadPcbCircle circle, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var cb = new SExpressionBuilder("fp_circle");
        if (circle.IsLocked && !circle.LockedIsChildNode)
            cb.AddSymbol("locked");
        cb.AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
            .AddChild("end", e => { e.AddMm(circle.End.X); e.AddMm(circle.End.Y); })
            .AddChild(WriterHelper.BuildStroke(circle.Width, circle.StrokeStyle, circle.StrokeColor));

        if (circle.UsePcbFillFormat)
            cb.AddChild(WriterHelper.BuildPcbFill(circle.FillType));
        else if (circle.FillType != SchFillType.None)
            cb.AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor));

        if (circle.IsLocked && circle.LockedIsChildNode)
            cb.AddChild("locked", l => l.AddBool(true));

        if (circle.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(circle.LayerName));

        if (circle.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuidToken(circle.Uuid, uuidToken, uuidIsSymbol));

        return cb.Build();
    }

    private static SExpr BuildFpArc(KiCadPcbArc arc, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var ab = new SExpressionBuilder("fp_arc");
        if (arc.IsLocked && !arc.LockedIsChildNode)
            ab.AddSymbol("locked");
        ab.AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
            .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
            .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
            .AddChild(WriterHelper.BuildStroke(arc.Width, arc.StrokeStyle, arc.StrokeColor));

        if (arc.IsLocked && arc.LockedIsChildNode)
            ab.AddChild("locked", l => l.AddBool(true));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddValue(arc.LayerName));

        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuidToken(arc.Uuid, uuidToken, uuidIsSymbol));

        return ab.Build();
    }

    internal static SExpr BuildPad(KiCadPcbPad pad, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var pb = new SExpressionBuilder("pad")
            .AddValue(pad.Designator ?? "")
            .AddSymbol(SExpressionHelper.PadTypeToString(pad.PadType))
            .AddSymbol(SExpressionHelper.PadShapeToString(pad.Shape));

        if (pad.IsLocked && !pad.LockedIsChildNode)
            pb.AddSymbol("locked");

        pb.AddChild(WriterHelper.BuildPositionCompact(pad.Location, pad.Rotation));
        pb.AddChild("size", s =>
        {
            s.AddMm(pad.Size.X);
            s.AddMm(pad.Size.Y);
        });

        // Rect delta (trapezoidal pads) — must come after size, before drill
        if (pad.RectDelta.HasValue)
            pb.AddChild("rect_delta", r => { r.AddMm(pad.RectDelta.Value.X); r.AddMm(pad.RectDelta.Value.Y); });

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

        // Custom pad options
        if (pad.CustomClearanceType is not null || pad.CustomAnchorShape is not null)
        {
            pb.AddChild("options", o =>
            {
                if (pad.CustomClearanceType is not null)
                    o.AddChild("clearance", c => c.AddSymbol(pad.CustomClearanceType));
                if (pad.CustomAnchorShape is not null)
                    o.AddChild("anchor", a => a.AddSymbol(pad.CustomAnchorShape));
            });
        }

        // Custom pad primitives
        if (pad.Primitives.Count > 0)
        {
            pb.AddChild("primitives", p =>
            {
                foreach (var prim in pad.Primitives)
                {
                    switch (prim)
                    {
                        case KiCadPcbTrack track: p.AddChild(BuildGrLine(track, uuidToken, uuidIsSymbol)); break;
                        case KiCadPcbRectangle rect: p.AddChild(BuildGrRect(rect, uuidToken, uuidIsSymbol)); break;
                        case KiCadPcbCircle circle: p.AddChild(BuildGrCircle(circle, uuidToken, uuidIsSymbol)); break;
                        case KiCadPcbArc arc: p.AddChild(BuildGrArc(arc, uuidToken, uuidIsSymbol)); break;
                        case KiCadPcbPolygon poly: p.AddChild(BuildGrPoly(poly, uuidToken, uuidIsSymbol)); break;
                        case KiCadPcbCurve curve: p.AddChild(BuildGrCurve(curve, uuidToken, uuidIsSymbol)); break;
                    }
                }
            });
        }

        if (pad.IsLocked && pad.LockedIsChildNode)
            pb.AddChild("locked", l => l.AddBool(true));

        // Tenting
        if (pad.HasTenting)
        {
            pb.AddChild("tenting", t =>
            {
                if (pad.TentingFront is not null)
                {
                    if (pad.TentingFront == "yes")
                        t.AddSymbol("front");
                    else
                        t.AddChild("front", f => f.AddSymbol(pad.TentingFront));
                }
                if (pad.TentingBack is not null)
                {
                    if (pad.TentingBack == "yes")
                        t.AddSymbol("back");
                    else
                        t.AddChild("back", bk => bk.AddSymbol(pad.TentingBack));
                }
            });
        }

        // Teardrops
        if (pad.HasTeardrops)
        {
            pb.AddChild("teardrops", td =>
            {
                if (pad.TeardropBestLengthRatio.HasValue) td.AddChild("best_length_ratio", c => c.AddValue(pad.TeardropBestLengthRatio.Value));
                if (pad.TeardropMaxLength.HasValue) td.AddChild("max_length", c => c.AddMm(pad.TeardropMaxLength.Value));
                if (pad.TeardropBestWidthRatio.HasValue) td.AddChild("best_width_ratio", c => c.AddValue(pad.TeardropBestWidthRatio.Value));
                if (pad.TeardropMaxWidth.HasValue) td.AddChild("max_width", c => c.AddMm(pad.TeardropMaxWidth.Value));
                if (pad.TeardropCurvedEdges.HasValue) td.AddChild("curved_edges", c => c.AddBool(pad.TeardropCurvedEdges.Value));
                if (pad.TeardropFilterRatio.HasValue) td.AddChild("filter_ratio", c => c.AddValue(pad.TeardropFilterRatio.Value));
                if (pad.TeardropEnabled.HasValue) td.AddChild("enabled", c => c.AddBool(pad.TeardropEnabled.Value));
                if (pad.TeardropAllowTwoSegments.HasValue) td.AddChild("allow_two_segments", c => c.AddBool(pad.TeardropAllowTwoSegments.Value));
                if (pad.TeardropPreferZoneConnections.HasValue) td.AddChild("prefer_zone_connections", c => c.AddBool(pad.TeardropPreferZoneConnections.Value));
            });
        }

        if (pad.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuidToken(pad.Uuid, uuidToken, uuidIsSymbol));

        return pb.Build();
    }

    private static SExpr BuildFpPoly(KiCadPcbPolygon poly, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var pb = new SExpressionBuilder("fp_poly");
        if (poly.IsLocked && !poly.LockedIsChildNode)
            pb.AddSymbol("locked");

        pb.AddChild(WriterHelper.BuildPoints(poly.Points));
        pb.AddChild(WriterHelper.BuildStroke(poly.Width, poly.StrokeStyle, poly.StrokeColor));

        if (poly.UsePcbFillFormat)
            pb.AddChild(WriterHelper.BuildPcbFill(poly.FillType));
        else if (poly.FillType != SchFillType.None)
            pb.AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor));

        if (poly.IsLocked && poly.LockedIsChildNode)
            pb.AddChild("locked", l => l.AddBool(true));

        if (poly.LayerName is not null)
            pb.AddChild("layer", l => l.AddValue(poly.LayerName));

        if (poly.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuidToken(poly.Uuid, uuidToken, uuidIsSymbol));

        return pb.Build();
    }

    private static SExpr BuildFpCurve(KiCadPcbCurve curve, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var cb = new SExpressionBuilder("fp_curve");
        if (curve.IsLocked && !curve.LockedIsChildNode)
            cb.AddSymbol("locked");

        cb.AddChild(WriterHelper.BuildPoints(curve.Points));
        cb.AddChild(WriterHelper.BuildStroke(curve.Width, curve.StrokeStyle, curve.StrokeColor));

        if (curve.IsLocked && curve.LockedIsChildNode)
            cb.AddChild("locked", l => l.AddBool(true));

        if (curve.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(curve.LayerName));

        if (curve.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuidToken(curve.Uuid, uuidToken, uuidIsSymbol));

        return cb.Build();
    }

    // Gr_* builders for custom pad primitives — use legacy (width N) format, not (stroke ...)
    private static SExpr BuildGrLine(KiCadPcbTrack track, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var lb = new SExpressionBuilder("gr_line");
        lb.AddChild("start", s => { s.AddMm(track.Start.X); s.AddMm(track.Start.Y); })
            .AddChild("end", e => { e.AddMm(track.End.X); e.AddMm(track.End.Y); })
            .AddChild("width", w => w.AddMm(track.Width));
        if (track.LayerName is not null)
            lb.AddChild("layer", l => l.AddValue(track.LayerName));
        if (track.Uuid is not null)
            lb.AddChild(WriterHelper.BuildUuidToken(track.Uuid, uuidToken, uuidIsSymbol));
        return lb.Build();
    }

    private static SExpr BuildGrRect(KiCadPcbRectangle rect, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var rb = new SExpressionBuilder("gr_rect");
        rb.AddChild("start", s => { s.AddMm(rect.Start.X); s.AddMm(rect.Start.Y); })
            .AddChild("end", e => { e.AddMm(rect.End.X); e.AddMm(rect.End.Y); })
            .AddChild("width", w => w.AddMm(rect.Width));
        if (rect.UsePcbFillFormat)
            rb.AddChild(WriterHelper.BuildPcbFill(rect.FillType));
        else if (rect.FillType != SchFillType.None)
            rb.AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor));
        if (rect.LayerName is not null)
            rb.AddChild("layer", l => l.AddValue(rect.LayerName));
        if (rect.Uuid is not null)
            rb.AddChild(WriterHelper.BuildUuidToken(rect.Uuid, uuidToken, uuidIsSymbol));
        return rb.Build();
    }

    private static SExpr BuildGrCircle(KiCadPcbCircle circle, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var cb = new SExpressionBuilder("gr_circle");
        cb.AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
            .AddChild("end", e => { e.AddMm(circle.End.X); e.AddMm(circle.End.Y); })
            .AddChild("width", w => w.AddMm(circle.Width));
        if (circle.UsePcbFillFormat)
            cb.AddChild(WriterHelper.BuildPcbFill(circle.FillType));
        else if (circle.FillType != SchFillType.None)
            cb.AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor));
        if (circle.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(circle.LayerName));
        if (circle.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuidToken(circle.Uuid, uuidToken, uuidIsSymbol));
        return cb.Build();
    }

    private static SExpr BuildGrArc(KiCadPcbArc arc, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var ab = new SExpressionBuilder("gr_arc");
        ab.AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
            .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
            .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
            .AddChild("width", w => w.AddMm(arc.Width));
        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddValue(arc.LayerName));
        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuidToken(arc.Uuid, uuidToken, uuidIsSymbol));
        return ab.Build();
    }

    private static SExpr BuildGrPoly(KiCadPcbPolygon poly, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var pb = new SExpressionBuilder("gr_poly");
        pb.AddChild(WriterHelper.BuildPoints(poly.Points));
        pb.AddChild("width", w => w.AddMm(poly.Width));
        if (poly.UsePcbFillFormat)
            pb.AddChild(WriterHelper.BuildPcbFill(poly.FillType));
        else if (poly.FillType != SchFillType.None)
            pb.AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor));
        if (poly.LayerName is not null)
            pb.AddChild("layer", l => l.AddValue(poly.LayerName));
        if (poly.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuidToken(poly.Uuid, uuidToken, uuidIsSymbol));
        return pb.Build();
    }

    private static SExpr BuildGrCurve(KiCadPcbCurve curve, string uuidToken = "uuid", bool uuidIsSymbol = false)
    {
        var cb = new SExpressionBuilder("gr_curve");
        cb.AddChild(WriterHelper.BuildPoints(curve.Points));
        cb.AddChild("width", w => w.AddMm(curve.Width));
        if (curve.LayerName is not null)
            cb.AddChild("layer", l => l.AddValue(curve.LayerName));
        if (curve.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuidToken(curve.Uuid, uuidToken, uuidIsSymbol));
        return cb.Build();
    }

    private static SExpr Build3DModel(KiCadPcb3DModel model)
    {
        var mb = new SExpressionBuilder("model").AddValue(model.Path);
        if (model.IsHidden)
            mb.AddChild("hide", h => h.AddBool(true));
        if (model.Opacity.HasValue)
            mb.AddChild("opacity", o => o.AddValue(model.Opacity.Value));
        mb.AddChild("offset", o => o.AddChild("xyz", xyz =>
        {
            xyz.AddMm(model.Offset.X);
            xyz.AddMm(model.Offset.Y);
            xyz.AddValue(model.OffsetZ);
        }));
        mb.AddChild("scale", s => s.AddChild("xyz", xyz =>
        {
            xyz.AddValue(model.ScaleX);
            xyz.AddValue(model.ScaleY);
            xyz.AddValue(model.ScaleZ);
        }));
        mb.AddChild("rotate", r => r.AddChild("xyz", xyz =>
        {
            xyz.AddValue(model.RotationX);
            xyz.AddValue(model.RotationY);
            xyz.AddValue(model.RotationZ);
        }));
        return mb.Build();
    }
}
