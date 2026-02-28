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
        var expr = sch.SourceTree ?? Build(sch);
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
        var expr = sch.SourceTree ?? Build(sch);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadSch sch)
    {
        var b = new SExpressionBuilder("kicad_sch")
            .AddChild("version", v => v.AddValue(sch.Version == 0 ? 20231120 : sch.Version))
            .AddChild("generator", g =>
            {
                if (sch.GeneratorIsSymbol)
                    g.AddSymbol(sch.Generator ?? "eeschema");
                else
                    g.AddValue(sch.Generator ?? "kicadsharp");
            });

        if (sch.GeneratorVersion is not null)
            b.AddChild("generator_version", g => g.AddValue(sch.GeneratorVersion));

        if (sch.Uuid is not null)
            b.AddChild(WriterHelper.BuildUuid(sch.Uuid));

        // Paper size
        if (sch.Paper is not null)
            b.AddChild("paper", p => p.AddValue(sch.Paper));

        // Title block
        if (sch.TitleBlock is not null)
            b.AddChild(sch.TitleBlock);

        // Lib symbols
        if (sch.LibSymbols.Count > 0)
        {
            b.AddChild("lib_symbols", ls =>
            {
                foreach (var sym in sch.LibSymbols)
                    ls.AddChild(SymLibWriter.BuildSymbol(sym));
            });
        }

        // Content elements - use ordered list when available to preserve original file ordering
        if (sch.OrderedElements.Count > 0)
        {
            foreach (var elem in sch.OrderedElements)
            {
                EmitElement(b, elem);
            }
        }
        else
        {
            // Fallback: emit in type-grouped order for newly created schematics
            EmitElementsGrouped(b, sch);
        }

        // Sheet instances
        if (sch.SheetInstances is not null)
            b.AddChild(sch.SheetInstances);

        // Symbol instances
        if (sch.SymbolInstances is not null)
            b.AddChild(sch.SymbolInstances);

        // embedded_fonts is emitted at the end of the file (KiCad 9+ position)
        if (sch.EmbeddedFonts.HasValue)
            b.AddChild("embedded_fonts", v => v.AddBool(sch.EmbeddedFonts.Value));

        return b.Build();
    }

    private static void EmitElement(SExpressionBuilder b, object elem)
    {
        switch (elem)
        {
            case KiCadSchWire wire:
                b.AddChild(BuildWire(wire));
                break;
            case KiCadSchJunction junction:
                b.AddChild(BuildJunction(junction));
                break;
            case KiCadSchNetLabel label:
                b.AddChild(BuildNetLabel(label));
                break;
            case KiCadSchLabel textLabel:
                b.AddChild(BuildTextLabel(textLabel));
                break;
            case KiCadSchNoConnect nc:
                b.AddChild(BuildNoConnect(nc));
                break;
            case KiCadSchBus bus:
                b.AddChild(BuildBus(bus));
                break;
            case KiCadSchBusEntry entry:
                b.AddChild(BuildBusEntry(entry));
                break;
            case KiCadSchComponent comp:
                b.AddChild(BuildPlacedSymbol(comp));
                break;
            case KiCadSchSheet sheet:
                b.AddChild(BuildSheet(sheet));
                break;
            case KiCadSchPowerObject power:
                b.AddChild(BuildPowerPort(power));
                break;
            case KiCadSchPolyline poly:
                b.AddChild(BuildPolyline(poly));
                break;
            case KiCadSchLine line:
                b.AddChild(BuildLine(line));
                break;
            case KiCadSchCircle circle:
                b.AddChild(BuildCircle(circle));
                break;
            case KiCadSchRectangle rect:
                b.AddChild(BuildRectangle(rect));
                break;
            case KiCadSchArc arc:
                b.AddChild(BuildArc(arc));
                break;
            case KiCadSchBezier bezier:
                b.AddChild(BuildBezier(bezier));
                break;
            case SExpr raw:
                b.AddChild(raw);
                break;
        }
    }

    private static void EmitElementsGrouped(SExpressionBuilder b, KiCadSch sch)
    {
        foreach (var wire in sch.Wires.OfType<KiCadSchWire>())
            b.AddChild(BuildWire(wire));
        foreach (var junction in sch.Junctions.OfType<KiCadSchJunction>())
            b.AddChild(BuildJunction(junction));
        foreach (var label in sch.NetLabels.OfType<KiCadSchNetLabel>())
            b.AddChild(BuildNetLabel(label));
        foreach (var label in sch.Labels.OfType<KiCadSchLabel>())
            b.AddChild(BuildTextLabel(label));
        foreach (var nc in sch.NoConnects.OfType<KiCadSchNoConnect>())
            b.AddChild(BuildNoConnect(nc));
        foreach (var bus in sch.Buses.OfType<KiCadSchBus>())
            b.AddChild(BuildBus(bus));
        foreach (var entry in sch.BusEntries.OfType<KiCadSchBusEntry>())
            b.AddChild(BuildBusEntry(entry));
        foreach (var poly in sch.Polylines)
            b.AddChild(BuildPolyline(poly));
        foreach (var line in sch.Lines)
            b.AddChild(BuildLine(line));
        foreach (var circle in sch.Circles)
            b.AddChild(BuildCircle(circle));
        foreach (var rect in sch.Rectangles)
            b.AddChild(BuildRectangle(rect));
        foreach (var arc in sch.Arcs)
            b.AddChild(BuildArc(arc));
        foreach (var bezier in sch.Beziers)
            b.AddChild(BuildBezier(bezier));
        foreach (var sheet in sch.Sheets)
            b.AddChild(BuildSheet(sheet));
        foreach (var power in sch.PowerObjects.OfType<KiCadSchPowerObject>())
            b.AddChild(BuildPowerPort(power));
        foreach (var image in sch.ImagesRaw)
            b.AddChild(image);
        foreach (var table in sch.TablesRaw)
            b.AddChild(table);
        foreach (var ruleArea in sch.RuleAreasRaw)
            b.AddChild(ruleArea);
        foreach (var netclassFlag in sch.NetclassFlagsRaw)
            b.AddChild(netclassFlag);
        foreach (var textBox in sch.TextBoxesRaw)
            b.AddChild(textBox);
        foreach (var busAlias in sch.BusAliasesRaw)
            b.AddChild(busAlias);
        foreach (var group in sch.GroupsRaw)
            b.AddChild(group);
        foreach (var comp in sch.Components.OfType<KiCadSchComponent>())
            b.AddChild(BuildPlacedSymbol(comp));

        // embedded_files at end
        if (sch.EmbeddedFilesRaw is not null)
            b.AddChild(sch.EmbeddedFilesRaw);
    }

    private static SExpr BuildWire(KiCadSchWire wire)
    {
        var wb = new SExpressionBuilder("wire")
            .AddChild(WriterHelper.BuildPoints(wire.Vertices))
            .AddChild(WriterHelper.BuildStroke(wire.LineWidth, wire.LineStyle, wire.Color));
        if (wire.Uuid is not null) wb.AddChild(WriterHelper.BuildUuid(wire.Uuid));
        return wb.Build();
    }

    private static SExpr BuildJunction(KiCadSchJunction junction)
    {
        var jb = new SExpressionBuilder("junction")
            .AddChild(WriterHelper.BuildPositionCompact(junction.Location))
            .AddChild("diameter", d => d.AddMm(junction.Size))
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
            .AddValue(label.Text);

        var fontH = label.FontSizeHeight != Coord.Zero ? label.FontSizeHeight : WriterHelper.DefaultTextSize;
        var fontW = label.FontSizeWidth != Coord.Zero ? label.FontSizeWidth : WriterHelper.DefaultTextSize;

        if (label.Shape is not null)
            lb.AddChild("shape", s => s.AddSymbol(label.Shape));

        lb.AddChild(WriterHelper.BuildPosition(label.Location, label.Orientation));

        if (label.FieldsAutoplaced)
            lb.AddChild("fields_autoplaced", f => f.AddBool(true));

        lb.AddChild(WriterHelper.BuildTextEffects(fontH, fontW, label.Justification, hide: false,
            isMirrored: label.IsMirrored, isBold: label.IsBold, isItalic: label.IsItalic,
            fontFace: label.FontFace, fontThickness: label.FontThickness, fontColor: label.FontColor));

        // uuid comes before properties in KiCad 9+
        if (label.Uuid is not null) lb.AddChild(WriterHelper.BuildUuid(label.Uuid));

        // Write properties for global/hierarchical labels
        foreach (var prop in label.Properties)
        {
            lb.AddChild(SymLibWriter.BuildProperty(prop));
        }
        return lb.Build();
    }

    private static SExpr BuildSheet(KiCadSchSheet sheet)
    {
        var sb = new SExpressionBuilder("sheet")
            .AddChild(WriterHelper.BuildPositionCompact(sheet.Location))
            .AddChild("size", s =>
            {
                s.AddMm(sheet.Size.X);
                s.AddMm(sheet.Size.Y);
            });

        // KiCad 9+ flags (emitted before stroke/fill)
        if (sheet.ExcludeFromSimPresent)
            sb.AddChild("exclude_from_sim", v => v.AddBool(sheet.ExcludeFromSim));
        if (sheet.InBomPresent)
            sb.AddChild("in_bom", v => v.AddBool(sheet.InBom));
        if (sheet.OnBoardPresent)
            sb.AddChild("on_board", v => v.AddBool(sheet.OnBoard));
        if (sheet.DnpPresent)
            sb.AddChild("dnp", v => v.AddBool(sheet.Dnp));

        if (sheet.FieldsAutoplaced)
            sb.AddChild("fields_autoplaced", f => f.AddBool(true));

        sb.AddChild(WriterHelper.BuildStroke(sheet.LineWidth, sheet.LineStyle, sheet.Color));
        if (sheet.FillColorOnly)
            sb.AddChild(WriterHelper.BuildFillColorOnly(sheet.FillColor));
        else
            sb.AddChild(WriterHelper.BuildFill(sheet.FillType, sheet.FillColor));

        if (sheet.Uuid is not null) sb.AddChild(WriterHelper.BuildUuid(sheet.Uuid));

        // Properties - use stored per-property data if available, otherwise fall back to defaults
        if (sheet.SheetProperties.Count > 0)
        {
            foreach (var prop in sheet.SheetProperties)
            {
                sb.AddChild(SymLibWriter.BuildProperty(prop));
            }
        }
        else
        {
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
        }

        foreach (var pin in sheet.Pins.OfType<KiCadSchSheetPin>())
        {
            var ioStr = SExpressionHelper.SheetPinIoTypeToString(pin.IoType);
            var angleFromSide = SExpressionHelper.SheetPinSideToAngle(pin.Side);
            var pb = new SExpressionBuilder("pin")
                .AddValue(pin.Name);

            // Simplified KiCad 9+ sheet pin format: (pin "name" (uuid "..."))
            // Full format: (pin "name" ioType (at X Y ANGLE) (uuid "...") (effects ...))
            var hasAtOrEffects = pin.FontSizeHeight != Coord.Zero || pin.Location != default;
            if (hasAtOrEffects)
            {
                pb.AddSymbol(ioStr);
                pb.AddChild(WriterHelper.BuildPosition(pin.Location, angleFromSide));
            }
            if (pin.Uuid is not null) pb.AddChild(WriterHelper.BuildUuid(pin.Uuid));
            if (hasAtOrEffects)
            {
                var fontH = pin.FontSizeHeight != Coord.Zero ? pin.FontSizeHeight : WriterHelper.DefaultTextSize;
                var fontW = pin.FontSizeWidth != Coord.Zero ? pin.FontSizeWidth : WriterHelper.DefaultTextSize;
                pb.AddChild(WriterHelper.BuildTextEffects(fontH, fontW, pin.Justification,
                    hide: false, isMirrored: pin.IsMirrored,
                    isBold: pin.IsBold, isItalic: pin.IsItalic,
                    fontFace: pin.FontFace, fontThickness: pin.FontThickness, fontColor: pin.FontColor));
            }
            sb.AddChild(pb.Build());
        }

        // Instances
        if (sheet.Instances is not null)
            sb.AddChild(sheet.Instances);

        return sb.Build();
    }

    private static SExpr BuildTextLabel(KiCadSchLabel label)
    {
        var fontH = label.FontSizeHeight != Coord.Zero ? label.FontSizeHeight : WriterHelper.DefaultTextSize;
        var fontW = label.FontSizeWidth != Coord.Zero ? label.FontSizeWidth : WriterHelper.DefaultTextSize;
        var tb = new SExpressionBuilder("text")
            .AddValue(label.Text);
        if (label.ExcludeFromSimPresent)
            tb.AddChild("exclude_from_sim", v => v.AddBool(label.ExcludeFromSim));
        tb.AddChild(WriterHelper.BuildPosition(label.Location, label.Rotation))
            .AddChild(WriterHelper.BuildTextEffects(fontH, fontW, label.Justification, label.IsHidden, label.IsMirrored, label.IsBold, label.IsItalic, fontFace: label.FontFace, fontThickness: label.FontThickness, fontColor: label.FontColor, href: label.Href));
        if (label.Uuid is not null) tb.AddChild(WriterHelper.BuildUuid(label.Uuid));
        return tb.Build();
    }

    private static SExpr BuildNoConnect(KiCadSchNoConnect nc)
    {
        var ncb = new SExpressionBuilder("no_connect")
            .AddChild(WriterHelper.BuildPositionCompact(nc.Location));
        if (nc.Uuid is not null) ncb.AddChild(WriterHelper.BuildUuid(nc.Uuid));
        return ncb.Build();
    }

    private static SExpr BuildBus(KiCadSchBus bus)
    {
        var bb = new SExpressionBuilder("bus")
            .AddChild(WriterHelper.BuildPoints(bus.Vertices))
            .AddChild(WriterHelper.BuildStroke(bus.LineWidth, bus.LineStyle, bus.Color));
        if (bus.Uuid is not null) bb.AddChild(WriterHelper.BuildUuid(bus.Uuid));
        return bb.Build();
    }

    private static SExpr BuildBusEntry(KiCadSchBusEntry entry)
    {
        var eb = new SExpressionBuilder("bus_entry")
            .AddChild(WriterHelper.BuildPositionCompact(entry.Location))
            .AddChild("size", s =>
            {
                s.AddMm(entry.Corner.X - entry.Location.X);
                s.AddMm(entry.Corner.Y - entry.Location.Y);
            })
            .AddChild(WriterHelper.BuildStroke(entry.LineWidth, entry.LineStyle, entry.Color));
        if (entry.Uuid is not null) eb.AddChild(WriterHelper.BuildUuid(entry.Uuid));
        return eb.Build();
    }

    private static SExpr BuildPolyline(KiCadSchPolyline poly)
    {
        var pb = new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints(poly.Vertices))
            .AddChild(WriterHelper.BuildStroke(poly.LineWidth, poly.LineStyle, poly.Color));
        if (poly.HasFill)
            pb.AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor));
        if (poly.Uuid is not null) pb.AddChild(WriterHelper.BuildUuid(poly.Uuid, poly.UuidIsSymbol));
        return pb.Build();
    }

    private static SExpr BuildLine(KiCadSchLine line)
    {
        var lb = new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints([line.Start, line.End]))
            .AddChild(WriterHelper.BuildStroke(line.Width, line.LineStyle, line.Color));
        if (line.HasFill)
            lb.AddChild(WriterHelper.BuildFill(SchFillType.None));
        if (line.Uuid is not null) lb.AddChild(WriterHelper.BuildUuid(line.Uuid, line.UuidIsSymbol));
        return lb.Build();
    }

    private static SExpr BuildCircle(KiCadSchCircle circle)
    {
        var cb = new SExpressionBuilder("circle")
            .AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
            .AddChild("radius", r => r.AddMm(circle.Radius))
            .AddChild(WriterHelper.BuildStroke(circle.LineWidth, circle.LineStyle, circle.Color, emitColor: circle.HasStrokeColor));
        if (circle.HasFill)
            cb.AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor));
        if (circle.Uuid is not null)
            cb.AddChild(WriterHelper.BuildUuid(circle.Uuid, circle.UuidIsSymbol));
        return cb.Build();
    }

    private static SExpr BuildRectangle(KiCadSchRectangle rect)
    {
        var rb = new SExpressionBuilder("rectangle")
            .AddChild("start", s => { s.AddMm(rect.Corner1.X); s.AddMm(rect.Corner1.Y); })
            .AddChild("end", e => { e.AddMm(rect.Corner2.X); e.AddMm(rect.Corner2.Y); })
            .AddChild(WriterHelper.BuildStroke(rect.LineWidth, rect.LineStyle, rect.Color, emitColor: rect.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor));
        if (rect.Uuid is not null) rb.AddChild(WriterHelper.BuildUuid(rect.Uuid, rect.UuidIsSymbol));
        return rb.Build();
    }

    private static SExpr BuildArc(KiCadSchArc arc)
    {
        var ab = new SExpressionBuilder("arc")
            .AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
            .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
            .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
            .AddChild(WriterHelper.BuildStroke(arc.LineWidth, arc.LineStyle, arc.Color, emitColor: arc.HasStrokeColor));
        if (arc.HasFill)
            ab.AddChild(WriterHelper.BuildFill(arc.FillType, arc.FillColor));
        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuid(arc.Uuid, arc.UuidIsSymbol));
        return ab.Build();
    }

    private static SExpr BuildBezier(KiCadSchBezier bezier)
    {
        var bb = new SExpressionBuilder("bezier")
            .AddChild(WriterHelper.BuildPoints(bezier.ControlPoints))
            .AddChild(WriterHelper.BuildStroke(bezier.LineWidth, bezier.LineStyle, bezier.Color, emitColor: bezier.HasStrokeColor));
        if (bezier.HasFill)
            bb.AddChild(WriterHelper.BuildFill(bezier.FillType, bezier.FillColor));
        if (bezier.Uuid is not null)
            bb.AddChild(WriterHelper.BuildUuid(bezier.Uuid, bezier.UuidIsSymbol));
        return bb.Build();
    }

    private static SExpr BuildPowerPort(KiCadSchPowerObject power)
    {
        // If we have the raw S-expression node, re-emit it verbatim for perfect round-trip
        if (power.RawNode is not null)
            return power.RawNode;

        // Otherwise build from model properties
        var pb = new SExpressionBuilder("power_port")
            .AddValue(power.Text ?? "");
        pb.AddChild(WriterHelper.BuildPosition(power.Location, power.Rotation));
        pb.AddChild(WriterHelper.BuildTextEffects(WriterHelper.DefaultTextSize, WriterHelper.DefaultTextSize));
        if (power.Uuid is not null) pb.AddChild(WriterHelper.BuildUuid(power.Uuid));
        return pb.Build();
    }

    private static SExpr BuildPlacedSymbol(KiCadSchComponent comp)
    {
        var sb = new SExpressionBuilder("symbol");

        // lib_name comes before lib_id when present
        if (comp.LibName is not null)
            sb.AddChild("lib_name", l => l.AddValue(comp.LibName));

        sb.AddChild("lib_id", l => l.AddValue(comp.Name));

        sb.AddChild(WriterHelper.BuildPosition(comp.Location, comp.Rotation));

        // Mirror - only emit when it was present in the source file
        if (comp.MirrorPresent)
        {
            if (comp.IsMirroredX && comp.IsMirroredY)
                sb.AddChild("mirror", m => m.AddSymbol("xy"));
            else if (comp.IsMirroredX)
                sb.AddChild("mirror", m => m.AddSymbol("x"));
            else if (comp.IsMirroredY)
                sb.AddChild("mirror", m => m.AddSymbol("y"));
        }

        if (comp.Unit > 0)
            sb.AddChild("unit", u => u.AddValue(comp.Unit));

        // convert / body_style - comes right after unit
        if (comp.BodyStyle > 0)
        {
            var tokenName = comp.UseBodyStyleToken ? "body_style" : "convert";
            sb.AddChild(tokenName, c => c.AddValue(comp.BodyStyle));
        }

        // exclude_from_sim - emit before in_bom when present
        if (comp.ExcludeFromSimPresent)
            sb.AddChild("exclude_from_sim", v => v.AddBool(comp.ExcludeFromSim));

        // in_bom / on_board
        sb.AddChild("in_bom", v => v.AddBool(comp.InBom));
        sb.AddChild("on_board", v => v.AddBool(comp.OnBoard));

        // dnp - emit after on_board when present
        if (comp.DnpPresent)
            sb.AddChild("dnp", v => v.AddBool(comp.Dnp));

        if (comp.FieldsAutoplaced)
            sb.AddChild("fields_autoplaced", f => f.AddBool(true));

        // uuid - emitted before properties in KiCad 9+
        if (comp.Uuid is not null)
            sb.AddChild(WriterHelper.BuildUuid(comp.Uuid));

        foreach (var param in comp.Parameters.OfType<KiCadSchParameter>())
        {
            sb.AddChild(SymLibWriter.BuildProperty(param));
        }

        // Write pins
        foreach (var pin in comp.Pins.OfType<KiCadSchPin>())
        {
            sb.AddChild(BuildPlacedPin(pin));
        }

        // Instances
        if (comp.InstancesRaw is not null)
            sb.AddChild(comp.InstancesRaw);

        return sb.Build();
    }

    private static SExpr BuildPlacedPin(KiCadSchPin pin)
    {
        var pb = new SExpressionBuilder("pin")
            .AddValue(pin.Name ?? "~");
        if (pin.Uuid is not null)
            pb.AddChild(WriterHelper.BuildUuid(pin.Uuid));
        return pb.Build();
    }
}
