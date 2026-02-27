using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Writes KiCad schematic files (<c>.kicad_sch</c>) from <see cref="KiCadSch"/> objects.
/// </summary>
public static class SchWriter
{
    /// <summary>
    /// Writes a schematic document to a file path.
    /// </summary>
    /// <param name="sch">The schematic document to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadSch sch, string path, CancellationToken ct = default)
    {
        var expr = Build(sch);
        await SExpressionWriter.WriteAsync(expr, path, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a schematic document to a stream.
    /// </summary>
    /// <param name="sch">The schematic document to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadSch sch, Stream stream, CancellationToken ct = default)
    {
        var expr = Build(sch);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadSch sch)
    {
        var b = new SExpressionBuilder("kicad_sch")
            .AddChild("version", v => v.AddValue(sch.Version == 0 ? 20231120 : sch.Version))
            .AddChild("generator", g => g.AddValue(sch.Generator ?? "kicadsharp"))
            .AddChild("generator_version", g => g.AddValue(sch.GeneratorVersion ?? "1.0"));

        if (sch.EmbeddedFonts)
            b.AddChild("embedded_fonts", v => v.AddBool(true));

        if (sch.Uuid is not null)
            b.AddChild(WriterHelper.BuildUuid(sch.Uuid));

        // Lib symbols
        if (sch.LibSymbols.Count > 0)
        {
            b.AddChild("lib_symbols", ls =>
            {
                foreach (var sym in sch.LibSymbols)
                    ls.AddChild(SymLibWriter.BuildSymbol(sym));
            });
        }

        // Wires
        foreach (var wire in sch.Wires.OfType<KiCadSchWire>())
        {
            b.AddChild(BuildWire(wire));
        }

        // Junctions
        foreach (var junction in sch.Junctions.OfType<KiCadSchJunction>())
        {
            b.AddChild(BuildJunction(junction));
        }

        // Net labels
        foreach (var label in sch.NetLabels.OfType<KiCadSchNetLabel>())
        {
            b.AddChild(BuildNetLabel(label));
        }

        // Text labels
        foreach (var label in sch.Labels.OfType<KiCadSchLabel>())
        {
            var tb = new SExpressionBuilder("text")
                .AddValue(label.Text)
                .AddChild(WriterHelper.BuildPosition(label.Location, label.Rotation))
                .AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize, label.Justification, label.IsHidden, label.IsMirrored));
            b.AddChild(tb.Build());
        }

        // No connects
        foreach (var nc in sch.NoConnects.OfType<KiCadSchNoConnect>())
        {
            var ncb = new SExpressionBuilder("no_connect")
                .AddChild(WriterHelper.BuildPosition(nc.Location));
            if (nc.Uuid is not null) ncb.AddChild(WriterHelper.BuildUuid(nc.Uuid));
            b.AddChild(ncb.Build());
        }

        // Buses
        foreach (var bus in sch.Buses.OfType<KiCadSchBus>())
        {
            var bb = new SExpressionBuilder("bus")
                .AddChild(WriterHelper.BuildPoints(bus.Vertices))
                .AddChild(WriterHelper.BuildStroke(bus.LineWidth));
            if (bus.Uuid is not null) bb.AddChild(WriterHelper.BuildUuid(bus.Uuid));
            b.AddChild(bb.Build());
        }

        // Bus entries
        foreach (var entry in sch.BusEntries.OfType<KiCadSchBusEntry>())
        {
            var eb = new SExpressionBuilder("bus_entry")
                .AddChild(WriterHelper.BuildPosition(entry.Location))
                .AddChild("size", s =>
                {
                    s.AddValue((entry.Corner.X - entry.Location.X).ToMm());
                    s.AddValue((entry.Corner.Y - entry.Location.Y).ToMm());
                })
                .AddChild(WriterHelper.BuildStroke(entry.LineWidth));
            if (entry.Uuid is not null) eb.AddChild(WriterHelper.BuildUuid(entry.Uuid));
            b.AddChild(eb.Build());
        }

        // Sheets
        foreach (var sheet in sch.Sheets)
        {
            b.AddChild(BuildSheet(sheet));
        }

        // Tables (raw S-expression, KiCad 8+)
        foreach (var table in sch.TablesRaw)
        {
            b.AddChild(table);
        }

        // Rule areas (raw S-expression, KiCad 8+)
        foreach (var ruleArea in sch.RuleAreasRaw)
        {
            b.AddChild(ruleArea);
        }

        // Netclass flags (raw S-expression, KiCad 8+)
        foreach (var netclassFlag in sch.NetclassFlagsRaw)
        {
            b.AddChild(netclassFlag);
        }

        // Bus aliases (raw S-expression)
        foreach (var busAlias in sch.BusAliasesRaw)
        {
            b.AddChild(busAlias);
        }

        // Placed symbols
        foreach (var comp in sch.Components.OfType<KiCadSchComponent>())
        {
            b.AddChild(BuildPlacedSymbol(comp));
        }

        return b.Build();
    }

    private static SExpr BuildWire(KiCadSchWire wire)
    {
        var wb = new SExpressionBuilder("wire")
            .AddChild(WriterHelper.BuildPoints(wire.Vertices))
            .AddChild(WriterHelper.BuildStroke(wire.LineWidth));
        if (wire.Uuid is not null) wb.AddChild(WriterHelper.BuildUuid(wire.Uuid));
        return wb.Build();
    }

    private static SExpr BuildJunction(KiCadSchJunction junction)
    {
        var jb = new SExpressionBuilder("junction")
            .AddChild(WriterHelper.BuildPosition(junction.Location))
            .AddChild("diameter", d => d.AddValue(junction.Size.ToMm()))
            .AddChild(WriterHelper.BuildColor(junction.Color));
        if (junction.Uuid is not null) jb.AddChild(WriterHelper.BuildUuid(junction.Uuid));
        return jb.Build();
    }

    private static SExpr BuildNetLabel(KiCadSchNetLabel label)
    {
        var token = label.LabelType switch
        {
            NetLabelType.Global => "global_label",
            NetLabelType.Hierarchical => "hierarchical_label",
            _ => "label"
        };
        var lb = new SExpressionBuilder(token)
            .AddValue(label.Text)
            .AddChild(WriterHelper.BuildPosition(label.Location, label.Orientation))
            .AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize, label.Justification));
        if (label.Uuid is not null) lb.AddChild(WriterHelper.BuildUuid(label.Uuid));
        return lb.Build();
    }

    private static SExpr BuildSheet(KiCadSchSheet sheet)
    {
        var sb = new SExpressionBuilder("sheet")
            .AddChild(WriterHelper.BuildPosition(sheet.Location))
            .AddChild("size", s =>
            {
                s.AddValue(sheet.Size.X.ToMm());
                s.AddValue(sheet.Size.Y.ToMm());
            })
            .AddChild(WriterHelper.BuildStroke(sheet.LineWidth));

        // Properties
        sb.AddChild("property", p =>
        {
            p.AddValue("Sheetname");
            p.AddValue(sheet.SheetName);
            p.AddChild(WriterHelper.BuildPosition(sheet.Location));
            p.AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize));
        });
        sb.AddChild("property", p =>
        {
            p.AddValue("Sheetfile");
            p.AddValue(sheet.FileName);
            p.AddChild(WriterHelper.BuildPosition(sheet.Location));
            p.AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize));
        });

        foreach (var pin in sheet.Pins.OfType<KiCadSchSheetPin>())
        {
            var ioStr = SExpressionHelper.SheetPinIoTypeToString(pin.IoType);
            var angleFromSide = SExpressionHelper.SheetPinSideToAngle(pin.Side);
            var pb = new SExpressionBuilder("pin")
                .AddValue(pin.Name)
                .AddSymbol(ioStr)
                .AddChild(WriterHelper.BuildPosition(pin.Location, angleFromSide))
                .AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize));
            if (pin.Uuid is not null) pb.AddChild(WriterHelper.BuildUuid(pin.Uuid));
            sb.AddChild(pb.Build());
        }

        if (sheet.Uuid is not null) sb.AddChild(WriterHelper.BuildUuid(sheet.Uuid));
        return sb.Build();
    }

    private static SExpr BuildPlacedSymbol(KiCadSchComponent comp)
    {
        var sb = new SExpressionBuilder("symbol")
            .AddChild("lib_id", l => l.AddValue(comp.Name));

        sb.AddChild(WriterHelper.BuildPosition(comp.Location, comp.Rotation));

        if (comp.IsMirroredX)
            sb.AddChild("mirror", m => m.AddSymbol("x"));
        else if (comp.IsMirroredY)
            sb.AddChild("mirror", m => m.AddSymbol("y"));

        if (comp.Unit > 0)
            sb.AddChild("unit", u => u.AddValue(comp.Unit));

        foreach (var param in comp.Parameters.OfType<KiCadSchParameter>())
        {
            sb.AddChild(SymLibWriter.BuildProperty(param));
        }

        if (comp.Uuid is not null)
            sb.AddChild(WriterHelper.BuildUuid(comp.Uuid));

        return sb.Build();
    }
}
