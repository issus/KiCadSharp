using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Writes KiCad symbol library files (<c>.kicad_sym</c>) from <see cref="KiCadSymLib"/> objects.
/// </summary>
public static class SymLibWriter
{
    /// <summary>
    /// Writes a symbol library to a file path.
    /// </summary>
    /// <param name="lib">The symbol library to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadSymLib lib, string path, CancellationToken ct = default)
    {
        var expr = lib.SourceTree ?? Build(lib);
        await SExpressionWriter.WriteAsync(expr, path, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a symbol library to a stream.
    /// </summary>
    /// <param name="lib">The symbol library to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadSymLib lib, Stream stream, CancellationToken ct = default)
    {
        var expr = lib.SourceTree ?? Build(lib);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadSymLib lib)
    {
        var b = new SExpressionBuilder("kicad_symbol_lib")
            .AddChild("version", v => v.AddValue(lib.Version == 0 ? 20231120 : lib.Version))
            .AddChild("generator", g =>
            {
                var gen = lib.Generator ?? "kicadsharp";
                if (lib.GeneratorIsSymbol)
                    g.AddSymbol(gen);
                else
                    g.AddValue(gen);
            });

        if (lib.GeneratorVersion is not null)
            b.AddChild("generator_version", g => g.AddValue(lib.GeneratorVersion));

        foreach (var comp in lib.Components)
        {
            b.AddChild(BuildSymbol(comp));
        }

        if (lib.EmbeddedFilesRaw != null)
            b.AddChild(lib.EmbeddedFilesRaw);

        return b.Build();
    }

    internal static SExpr BuildSymbol(KiCadSchComponent component, bool isSubSymbol = false)
    {
        var b = new SExpressionBuilder("symbol").AddValue(component.Name);

        if (!isSubSymbol)
        {
            if (component.Extends is not null)
            {
                b.AddChild("extends", e => e.AddValue(component.Extends));
            }

            // power (KiCad 8+)
            if (component.IsPower)
            {
                if (component.PowerType is not null)
                    b.AddChild("power", p => p.AddSymbol(component.PowerType));
                else
                    b.AddChild("power", _ => { });
            }

            // Determine if this is KiCad 8+ format (embedded_fonts presence is the indicator)
            bool isKiCad8 = component.EmbeddedFonts.HasValue;

            // pin_numbers - emit when present in source file (before pin_names for KiCad ordering)
            if (component.PinNumbersPresent)
            {
                b.AddChild("pin_numbers", pn =>
                {
                    if (component.HidePinNumbers)
                    {
                        if (isKiCad8)
                            pn.AddChild("hide", h => h.AddBool(true));
                        else
                            pn.AddSymbol("hide");
                    }
                });
            }

            // pin_names - emit when present in source file
            if (component.PinNamesPresent)
            {
                b.AddChild("pin_names", pn =>
                {
                    // Only emit (offset N) when it was explicitly present in the source file
                    if (component.PinNamesHasOffset)
                        pn.AddChild("offset", o => o.AddMm(component.PinNamesOffset));
                    if (component.HidePinNames)
                    {
                        if (isKiCad8)
                            pn.AddChild("hide", h => h.AddBool(true));
                        else
                            pn.AddSymbol("hide");
                    }
                });
            }

            // exclude_from_sim - emit when it was present in the source (before in_bom for KiCad 8 ordering)
            if (component.ExcludeFromSimPresent)
                b.AddChild("exclude_from_sim", v => v.AddBool(component.ExcludeFromSim));

            b.AddChild("in_bom", v => v.AddBool(component.InBom));
            b.AddChild("on_board", v => v.AddBool(component.OnBoard));

            if (component.DuplicatePinNumbersAreJumpersPresent)
                b.AddChild("duplicate_pin_numbers_are_jumpers", v => v.AddBool(component.DuplicatePinNumbersAreJumpers));

            // Properties
            foreach (var param in component.Parameters.OfType<KiCadSchParameter>())
            {
                b.AddChild(BuildProperty(param));
            }
        }

        if (component.UnitName != null)
            b.AddChild("unit_name", u => u.AddValue(component.UnitName));

        // Sub-symbols
        foreach (var sub in component.SubSymbols)
        {
            b.AddChild(BuildSymbol(sub, isSubSymbol: true));
        }

        // Direct graphical primitives (from this level, not sub-symbols)
        // Use OrderedPrimitives to preserve original file ordering when available
        if (component.SubSymbols.Count == 0 && component.OrderedPrimitives.Count > 0)
        {
            foreach (var prim in component.OrderedPrimitives)
            {
                switch (prim)
                {
                    case KiCadSchPin pin:
                        b.AddChild(BuildPin(pin));
                        break;
                    case KiCadSchRectangle rect:
                        b.AddChild(BuildRectangle(rect));
                        break;
                    case KiCadSchLine line:
                        b.AddChild(BuildPolyline([line.Start, line.End], line.Width, line.LineStyle, line.Color, emitColor: line.HasStrokeColor));
                        break;
                    case KiCadSchPolyline poly:
                        b.AddChild(BuildPolyline(poly.Vertices, poly.LineWidth, poly.LineStyle, poly.Color, poly.FillType, poly.FillColor, emitColor: poly.HasStrokeColor, uuid: poly.Uuid, uuidIsSymbol: poly.UuidIsSymbol));
                        break;
                    case KiCadSchPolygon polygon:
                        b.AddChild(BuildPolygon(polygon));
                        break;
                    case KiCadSchArc arc:
                        b.AddChild(BuildArc(arc));
                        break;
                    case KiCadSchCircle circle:
                        b.AddChild(BuildCircle(circle));
                        break;
                    case KiCadSchBezier bezier:
                        b.AddChild(BuildBezier(bezier));
                        break;
                    case KiCadSchLabel label:
                        b.AddChild(BuildTextLabel(label));
                        break;
                }
            }
        }
        else if (component.SubSymbols.Count == 0)
        {
            // Fallback: emit in type-grouped order
            foreach (var pin in component.Pins.OfType<KiCadSchPin>())
                b.AddChild(BuildPin(pin));
            foreach (var rect in component.Rectangles.OfType<KiCadSchRectangle>())
                b.AddChild(BuildRectangle(rect));
            foreach (var line in component.Lines.OfType<KiCadSchLine>())
                b.AddChild(BuildPolyline([line.Start, line.End], line.Width, line.LineStyle, line.Color, emitColor: line.HasStrokeColor));
            foreach (var poly in component.Polylines.OfType<KiCadSchPolyline>())
                b.AddChild(BuildPolyline(poly.Vertices, poly.LineWidth, poly.LineStyle, poly.Color, poly.FillType, poly.FillColor, emitColor: poly.HasStrokeColor, uuid: poly.Uuid, uuidIsSymbol: poly.UuidIsSymbol));
            foreach (var poly in component.Polygons.OfType<KiCadSchPolygon>())
                b.AddChild(BuildPolygon(poly));
            foreach (var arc in component.Arcs.OfType<KiCadSchArc>())
                b.AddChild(BuildArc(arc));
            foreach (var circle in component.Circles.OfType<KiCadSchCircle>())
                b.AddChild(BuildCircle(circle));
            foreach (var bezier in component.Beziers.OfType<KiCadSchBezier>())
                b.AddChild(BuildBezier(bezier));
            foreach (var label in component.Labels.OfType<KiCadSchLabel>())
                b.AddChild(BuildTextLabel(label));
        }

        // embedded_fonts (KiCad 8+) — emit at end of top-level symbol, always when present (even if false)
        if (!isSubSymbol && component.EmbeddedFonts.HasValue)
        {
            b.AddChild("embedded_fonts", v => v.AddBool(component.EmbeddedFonts.Value));
        }

        return b.Build();
    }

    internal static SExpr BuildProperty(KiCadSchParameter param)
    {
        var b = new SExpressionBuilder("property");

        // Inline properties (e.g., ki_fp_filters) use bare symbol names
        if (param.IsInline)
        {
            b.AddSymbol(param.Name);
            b.AddValue(param.Value);
            return b.Build();
        }

        b.AddValue(param.Name)
            .AddValue(param.Value);

        if (param.Id.HasValue)
            b.AddChild("id", id => id.AddValue(param.Id.Value));

        b.AddChild(WriterHelper.BuildPosition(param.Location, param.Orientation));

        // (do_not_autoplace) or (do_not_autoplace yes) - KiCad 9+
        if (param.DoNotAutoplace)
        {
            if (param.DoNotAutoplaceHasValue)
                b.AddChild("do_not_autoplace", d => d.AddBool(true));
            else
                b.AddChild("do_not_autoplace", _ => { });
        }

        // KiCad 8 footprint property attributes
        if (param.IsUnlocked)
            b.AddChild("unlocked", u => u.AddBool(true));

        if (param.LayerName is not null)
            b.AddChild("layer", l => l.AddValue(param.LayerName));

        if (!param.IsVisible && param.HideIsDirectChild)
        {
            // KiCad 8 footprint / KiCad 9 lib symbol: (hide yes) as direct child
            b.AddChild("hide", h => h.AddBool(true));
        }

        if (param.Uuid is not null)
            b.AddChild(WriterHelper.BuildUuid(param.Uuid));

        b.AddChild(WriterHelper.BuildPropertyTextEffects(param));
        return b.Build();
    }

    private static SExpr BuildPin(KiCadSchPin pin)
    {
        var b = new SExpressionBuilder("pin")
            .AddSymbol(SExpressionHelper.PinElectricalTypeToString(pin.ElectricalType))
            .AddSymbol(SExpressionHelper.PinGraphicStyleToString(pin.GraphicStyle));

        b.AddChild(WriterHelper.BuildPosition(pin.Location, SExpressionHelper.PinOrientationToAngle(pin.Orientation)))
         .AddChild("length", l => l.AddMm(pin.Length));

        // Hide attribute - emit before name/number as KiCad does
        // KiCad 6: (pin ... hide) — bare symbol value
        // KiCad 8: (pin ... (hide yes)) — child node
        if (pin.IsHidden)
        {
            if (pin.HideIsSymbolValue)
                b.AddSymbol("hide");
            else
                b.AddChild("hide", h => h.AddBool(true));
        }

        // Name with font size and styling
        // When the font size is zero, KiCad implicitly hides the text via (size 0 0),
        // so preserve zero font sizes and don't add a separate (hide yes) node.
        bool nameHiddenByZeroFont = pin.NameFontSizeHeight == Coord.Zero && pin.NameFontSizeWidth == Coord.Zero && !pin.ShowName;
        var nameFontH = nameHiddenByZeroFont ? Coord.Zero : (pin.NameFontSizeHeight != Coord.Zero ? pin.NameFontSizeHeight : WriterHelper.DefaultTextSize);
        var nameFontW = nameHiddenByZeroFont ? Coord.Zero : (pin.NameFontSizeWidth != Coord.Zero ? pin.NameFontSizeWidth : WriterHelper.DefaultTextSize);
        b.AddChild("name", n =>
        {
            n.AddValue(pin.Name ?? "~");
            n.AddChild(WriterHelper.BuildTextEffects(nameFontH, nameFontW, hide: !pin.ShowName && !nameHiddenByZeroFont, isBold: pin.NameIsBold, isItalic: pin.NameIsItalic, fontFace: pin.NameFontFace, fontThickness: pin.NameFontThickness, fontColor: pin.NameFontColor, boldIsSymbol: pin.NameBoldIsSymbol, italicIsSymbol: pin.NameItalicIsSymbol));
        });

        // Number with font size and styling
        bool numHiddenByZeroFont = pin.NumberFontSizeHeight == Coord.Zero && pin.NumberFontSizeWidth == Coord.Zero && !pin.ShowDesignator;
        var numFontH = numHiddenByZeroFont ? Coord.Zero : (pin.NumberFontSizeHeight != Coord.Zero ? pin.NumberFontSizeHeight : WriterHelper.DefaultTextSize);
        var numFontW = numHiddenByZeroFont ? Coord.Zero : (pin.NumberFontSizeWidth != Coord.Zero ? pin.NumberFontSizeWidth : WriterHelper.DefaultTextSize);
        b.AddChild("number", n =>
        {
            n.AddValue(pin.Designator ?? "~");
            n.AddChild(WriterHelper.BuildTextEffects(numFontH, numFontW, hide: !pin.ShowDesignator && !numHiddenByZeroFont, isBold: pin.NumberIsBold, isItalic: pin.NumberIsItalic, fontFace: pin.NumberFontFace, fontThickness: pin.NumberFontThickness, fontColor: pin.NumberFontColor, boldIsSymbol: pin.NumberBoldIsSymbol, italicIsSymbol: pin.NumberItalicIsSymbol));
        });

        // Alternates
        foreach (var alt in pin.Alternates)
        {
            b.AddChild("alternate", a =>
            {
                a.AddValue(alt.Name);
                a.AddSymbol(SExpressionHelper.PinElectricalTypeToString(alt.ElectricalType));
                a.AddSymbol(SExpressionHelper.PinGraphicStyleToString(alt.GraphicStyle));
            });
        }

        return b.Build();
    }

    private static SExpr BuildRectangle(KiCadSchRectangle rect)
    {
        var b = new SExpressionBuilder("rectangle")
            .AddChild("start", s => { s.AddMm(rect.Corner1.X); s.AddMm(rect.Corner1.Y); })
            .AddChild("end", e => { e.AddMm(rect.Corner2.X); e.AddMm(rect.Corner2.Y); })
            .AddChild(WriterHelper.BuildStroke(rect.LineWidth, rect.LineStyle, rect.Color, emitColor: rect.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor));
        if (rect.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(rect.Uuid, rect.UuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildPolyline(IReadOnlyList<CoordPoint> vertices, Coord width, LineStyle style, EdaColor color = default, SchFillType fillType = SchFillType.None, EdaColor fillColor = default, bool emitColor = false, string? uuid = null, bool uuidIsSymbol = false)
    {
        var b = new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints(vertices))
            .AddChild(WriterHelper.BuildStroke(width, style, color, emitColor: emitColor))
            .AddChild(WriterHelper.BuildFill(fillType, fillColor));
        if (uuid != null)
            b.AddChild(WriterHelper.BuildUuid(uuid, uuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildPolygon(KiCadSchPolygon poly)
    {
        var b = new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints(poly.Vertices))
            .AddChild(WriterHelper.BuildStroke(poly.LineWidth, poly.LineStyle, poly.Color, emitColor: poly.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor));
        if (poly.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(poly.Uuid, poly.UuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildArc(KiCadSchArc arc)
    {
        var b = new SExpressionBuilder("arc")
            .AddChild("start", s => { s.AddMm(arc.ArcStart.X); s.AddMm(arc.ArcStart.Y); })
            .AddChild("mid", m => { m.AddMm(arc.ArcMid.X); m.AddMm(arc.ArcMid.Y); })
            .AddChild("end", e => { e.AddMm(arc.ArcEnd.X); e.AddMm(arc.ArcEnd.Y); })
            .AddChild(WriterHelper.BuildStroke(arc.LineWidth, arc.LineStyle, arc.Color, emitColor: arc.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(arc.FillType, arc.FillColor));
        if (arc.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(arc.Uuid, arc.UuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildCircle(KiCadSchCircle circle)
    {
        var b = new SExpressionBuilder("circle")
            .AddChild("center", c => { c.AddMm(circle.Center.X); c.AddMm(circle.Center.Y); })
            .AddChild("radius", r => r.AddMm(circle.Radius))
            .AddChild(WriterHelper.BuildStroke(circle.LineWidth, circle.LineStyle, circle.Color, emitColor: circle.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor));
        if (circle.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(circle.Uuid, circle.UuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildBezier(KiCadSchBezier bezier)
    {
        var b = new SExpressionBuilder("bezier")
            .AddChild(WriterHelper.BuildPoints(bezier.ControlPoints))
            .AddChild(WriterHelper.BuildStroke(bezier.LineWidth, bezier.LineStyle, bezier.Color, emitColor: bezier.HasStrokeColor))
            .AddChild(WriterHelper.BuildFill(bezier.FillType, bezier.FillColor));
        if (bezier.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(bezier.Uuid, bezier.UuidIsSymbol));
        return b.Build();
    }

    private static SExpr BuildTextLabel(KiCadSchLabel label)
    {
        var fontH = label.FontSizeHeight != Coord.Zero ? label.FontSizeHeight : WriterHelper.DefaultTextSize;
        var fontW = label.FontSizeWidth != Coord.Zero ? label.FontSizeWidth : WriterHelper.DefaultTextSize;
        var b = new SExpressionBuilder("text")
            .AddValue(label.Text)
            .AddChild(WriterHelper.BuildPosition(label.Location, label.Rotation));

        // Write stroke only if it was present in the source file
        if (label.HasStroke)
            b.AddChild(WriterHelper.BuildStroke(label.StrokeWidth, label.StrokeLineStyle, label.StrokeColor));

        b.AddChild(WriterHelper.BuildTextEffects(fontH, fontW, label.Justification, label.IsHidden, label.IsMirrored, label.IsBold, label.IsItalic, fontFace: label.FontFace, fontThickness: label.FontThickness, fontColor: label.FontColor, href: label.Href, boldIsSymbol: label.BoldIsSymbol, italicIsSymbol: label.ItalicIsSymbol));

        if (label.Uuid != null)
            b.AddChild(WriterHelper.BuildUuid(label.Uuid, label.UuidIsSymbol));

        return b.Build();
    }
}
