using OriginalCircuit.Eda.Enums;
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

        if (component.IsLocked)
            b.AddSymbol("locked");

        if (component.LayerName is not null)
            b.AddChild("layer", l => l.AddSymbol(component.LayerName));

        if (component.Location != CoordPoint.Zero || component.Rotation != 0)
            b.AddChild(WriterHelper.BuildPosition(component.Location, component.Rotation));

        if (component.Description is not null)
            b.AddChild("descr", d => d.AddValue(component.Description));

        if (component.Tags is not null)
            b.AddChild("tags", t => t.AddValue(component.Tags));

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
            });
        }

        // Properties
        foreach (var prop in component.Properties)
        {
            b.AddChild(SymLibWriter.BuildProperty(prop));
        }

        // Clearances
        if (component.Clearance != Coord.Zero)
            b.AddChild("clearance", c => c.AddValue(component.Clearance.ToMm()));
        if (component.SolderMaskMargin != Coord.Zero)
            b.AddChild("solder_mask_margin", c => c.AddValue(component.SolderMaskMargin.ToMm()));
        if (component.SolderPasteMargin != Coord.Zero)
            b.AddChild("solder_paste_margin", c => c.AddValue(component.SolderPasteMargin.ToMm()));
        if (component.ThermalWidth != Coord.Zero)
            b.AddChild("thermal_width", c => c.AddValue(component.ThermalWidth.ToMm()));
        if (component.ThermalGap != Coord.Zero)
            b.AddChild("thermal_gap", c => c.AddValue(component.ThermalGap.ToMm()));

        // Texts
        foreach (var text in component.Texts.OfType<KiCadPcbText>())
        {
            b.AddChild(BuildFpText(text));
        }

        // Lines (stored as tracks in the model)
        foreach (var track in component.Tracks.OfType<KiCadPcbTrack>())
        {
            b.AddChild(BuildFpLine(track));
        }

        // Arcs
        foreach (var arc in component.Arcs.OfType<KiCadPcbArc>())
        {
            b.AddChild(BuildFpArc(arc));
        }

        // Pads
        foreach (var pad in component.Pads.OfType<KiCadPcbPad>())
        {
            b.AddChild(BuildPad(pad));
        }

        // 3D model
        if (component.Model3D is not null)
        {
            b.AddChild("model", m =>
            {
                m.AddValue(component.Model3D);
                m.AddChild("offset", o => o.AddChild("xyz", xyz =>
                {
                    xyz.AddValue(component.Model3DOffset.X.ToMm());
                    xyz.AddValue(component.Model3DOffset.Y.ToMm());
                    xyz.AddValue(component.Model3DOffsetZ);
                }));
                m.AddChild("scale", s => s.AddChild("xyz", xyz =>
                {
                    xyz.AddValue(component.Model3DScale.X.ToMm());
                    xyz.AddValue(component.Model3DScale.Y.ToMm());
                    xyz.AddValue(component.Model3DScaleZ);
                }));
                m.AddChild("rotate", r => r.AddChild("xyz", xyz =>
                {
                    xyz.AddValue(component.Model3DRotation.X.ToMm());
                    xyz.AddValue(component.Model3DRotation.Y.ToMm());
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

        if (text.IsHidden)
            tb.AddSymbol("hide");

        tb.AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));

        if (text.LayerName is not null)
            tb.AddChild("layer", l => l.AddSymbol(text.LayerName));

        tb.AddChild(WriterHelper.BuildTextEffects(text.Height, text.Height, isBold: text.FontBold, isItalic: text.FontItalic));

        if (text.Uuid is not null)
            tb.AddChild(WriterHelper.BuildUuid(text.Uuid));

        return tb.Build();
    }

    private static SExpr BuildFpLine(KiCadPcbTrack track)
    {
        var lb = new SExpressionBuilder("fp_line")
            .AddChild("start", s => { s.AddValue(track.Start.X.ToMm()); s.AddValue(track.Start.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(track.End.X.ToMm()); e.AddValue(track.End.Y.ToMm()); })
            .AddChild(WriterHelper.BuildStroke(track.Width));

        if (track.LayerName is not null)
            lb.AddChild("layer", l => l.AddSymbol(track.LayerName));

        if (track.Uuid is not null)
            lb.AddChild(WriterHelper.BuildUuid(track.Uuid));

        return lb.Build();
    }

    private static SExpr BuildFpArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("fp_arc")
            .AddChild("start", s => { s.AddValue(arc.ArcStart.X.ToMm()); s.AddValue(arc.ArcStart.Y.ToMm()); })
            .AddChild("mid", m => { m.AddValue(arc.ArcMid.X.ToMm()); m.AddValue(arc.ArcMid.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(arc.ArcEnd.X.ToMm()); e.AddValue(arc.ArcEnd.Y.ToMm()); })
            .AddChild(WriterHelper.BuildStroke(arc.Width));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddSymbol(arc.LayerName));

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

        pb.AddChild(WriterHelper.BuildPosition(pad.Location, pad.Rotation));
        pb.AddChild("size", s =>
        {
            s.AddValue(pad.Size.X.ToMm());
            s.AddValue(pad.Size.Y.ToMm());
        });

        if (pad.HoleSize != Coord.Zero)
        {
            if (pad.HoleType == PadHoleType.Slot)
            {
                pb.AddChild("drill", d =>
                {
                    d.AddSymbol("oval");
                    d.AddValue(pad.HoleSize.ToMm());
                });
            }
            else
            {
                pb.AddChild("drill", d => d.AddValue(pad.HoleSize.ToMm()));
            }
        }

        if (pad.Layers.Count > 0)
        {
            pb.AddChild("layers", l =>
            {
                foreach (var layer in pad.Layers)
                    l.AddSymbol(layer);
            });
        }

        if (pad.CornerRadiusPercentage > 0)
            pb.AddChild("roundrect_rratio", r => r.AddValue(pad.CornerRadiusPercentage / 100.0));

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
            pb.AddChild("solder_mask_margin", c => c.AddValue(pad.SolderMaskExpansion.ToMm()));
        if (pad.Clearance != Coord.Zero)
            pb.AddChild("clearance", c => c.AddValue(pad.Clearance.ToMm()));
        if (pad.SolderPasteMargin != Coord.Zero)
            pb.AddChild("solder_paste_margin", c => c.AddValue(pad.SolderPasteMargin.ToMm()));
        if (pad.SolderPasteRatio != 0)
            pb.AddChild("solder_paste_margin_ratio", c => c.AddValue(pad.SolderPasteRatio));
        if (pad.ThermalWidth != Coord.Zero)
            pb.AddChild("thermal_width", c => c.AddValue(pad.ThermalWidth.ToMm()));
        if (pad.ThermalGap != Coord.Zero)
            pb.AddChild("thermal_gap", c => c.AddValue(pad.ThermalGap.ToMm()));
        if (pad.ZoneConnect != ZoneConnectionType.Inherited)
            pb.AddChild("zone_connect", c => c.AddValue((int)pad.ZoneConnect));
        if (pad.DieLength != Coord.Zero)
            pb.AddChild("die_length", c => c.AddValue(pad.DieLength.ToMm()));

        if (pad.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuid(pad.Uuid));

        return pb.Build();
    }
}
