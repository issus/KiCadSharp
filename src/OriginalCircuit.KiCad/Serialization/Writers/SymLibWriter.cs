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
        var expr = Build(lib);
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
        var expr = Build(lib);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadSymLib lib)
    {
        var b = new SExpressionBuilder("kicad_symbol_lib")
            .AddChild("version", v => v.AddValue(lib.Version == 0 ? 20231120 : lib.Version))
            .AddChild("generator", g => g.AddValue(lib.Generator ?? "kicadsharp"))
            .AddChild("generator_version", g => g.AddValue(lib.GeneratorVersion ?? "1.0"));

        foreach (var comp in lib.Components)
        {
            b.AddChild(BuildSymbol(comp));
        }

        return b.Build();
    }

    internal static SExpr BuildSymbol(KiCadSchComponent component)
    {
        var b = new SExpressionBuilder("symbol").AddValue(component.Name);

        if (component.Extends is not null)
        {
            b.AddChild("extends", e => e.AddValue(component.Extends));
        }

        // pin_names
        if (component.HidePinNames || component.PinNamesOffset != Coord.Zero)
        {
            b.AddChild("pin_names", pn =>
            {
                if (component.PinNamesOffset != Coord.Zero)
                    pn.AddChild("offset", o => o.AddValue(component.PinNamesOffset.ToMm()));
                if (component.HidePinNames)
                    pn.AddSymbol("hide");
            });
        }

        // pin_numbers
        if (component.HidePinNumbers)
        {
            b.AddChild("pin_numbers", pn => pn.AddSymbol("hide"));
        }

        b.AddChild("in_bom", v => v.AddBool(component.InBom));
        b.AddChild("on_board", v => v.AddBool(component.OnBoard));

        // Properties
        foreach (var param in component.Parameters.OfType<KiCadSchParameter>())
        {
            b.AddChild(BuildProperty(param));
        }

        // Sub-symbols
        foreach (var sub in component.SubSymbols)
        {
            b.AddChild(BuildSymbol(sub));
        }

        // Direct graphical primitives (from this level, not sub-symbols)
        foreach (var pin in component.Pins.OfType<KiCadSchPin>())
        {
            // Only write pins that belong directly to this symbol, not inherited from sub-symbols
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildPin(pin));
        }

        foreach (var rect in component.Rectangles.OfType<KiCadSchRectangle>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildRectangle(rect));
        }

        foreach (var line in component.Lines.OfType<KiCadSchLine>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildPolyline([line.Start, line.End], line.Width, line.LineStyle, line.Color));
        }

        foreach (var poly in component.Polylines.OfType<KiCadSchPolyline>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildPolyline(poly.Vertices, poly.LineWidth, poly.LineStyle, poly.Color, poly.FillType, poly.FillColor));
        }

        foreach (var poly in component.Polygons.OfType<KiCadSchPolygon>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildPolygon(poly));
        }

        foreach (var arc in component.Arcs.OfType<KiCadSchArc>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildArc(arc));
        }

        foreach (var circle in component.Circles.OfType<KiCadSchCircle>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildCircle(circle));
        }

        foreach (var bezier in component.Beziers.OfType<KiCadSchBezier>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildBezier(bezier));
        }

        foreach (var label in component.Labels.OfType<KiCadSchLabel>())
        {
            if (component.SubSymbols.Count == 0)
                b.AddChild(BuildTextLabel(label));
        }

        return b.Build();
    }

    internal static SExpr BuildProperty(KiCadSchParameter param)
    {
        var b = new SExpressionBuilder("property")
            .AddValue(param.Name)
            .AddValue(param.Value);

        if (param.Id.HasValue)
            b.AddChild("id", id => id.AddValue(param.Id.Value));

        b.AddChild(WriterHelper.BuildPosition(param.Location, param.Orientation))
         .AddChild(WriterHelper.BuildPropertyTextEffects(param));
        return b.Build();
    }

    private static SExpr BuildPin(KiCadSchPin pin)
    {
        var b = new SExpressionBuilder("pin")
            .AddSymbol(SExpressionHelper.PinElectricalTypeToString(pin.ElectricalType))
            .AddSymbol(SExpressionHelper.PinGraphicStyleToString(pin.GraphicStyle));

        b.AddChild(WriterHelper.BuildPosition(pin.Location, SExpressionHelper.PinOrientationToAngle(pin.Orientation)))
         .AddChild("length", l => l.AddValue(pin.Length.ToMm()));

        // Name with font size
        var nameFontH = pin.NameFontSizeHeight != Coord.Zero ? pin.NameFontSizeHeight : WriterHelper.DefaultTextSize;
        var nameFontW = pin.NameFontSizeWidth != Coord.Zero ? pin.NameFontSizeWidth : WriterHelper.DefaultTextSize;
        b.AddChild("name", n =>
        {
            n.AddValue(pin.Name ?? "~");
            n.AddChild(WriterHelper.BuildTextEffects(nameFontH, nameFontW, hide: !pin.ShowName));
        });

        // Number with font size
        var numFontH = pin.NumberFontSizeHeight != Coord.Zero ? pin.NumberFontSizeHeight : WriterHelper.DefaultTextSize;
        var numFontW = pin.NumberFontSizeWidth != Coord.Zero ? pin.NumberFontSizeWidth : WriterHelper.DefaultTextSize;
        b.AddChild("number", n =>
        {
            n.AddValue(pin.Designator ?? "~");
            n.AddChild(WriterHelper.BuildTextEffects(numFontH, numFontW, hide: !pin.ShowDesignator));
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
        return new SExpressionBuilder("rectangle")
            .AddChild("start", s => { s.AddValue(rect.Corner1.X.ToMm()); s.AddValue(rect.Corner1.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(rect.Corner2.X.ToMm()); e.AddValue(rect.Corner2.Y.ToMm()); })
            .AddChild(WriterHelper.BuildStroke(rect.LineWidth, rect.LineStyle, rect.Color))
            .AddChild(WriterHelper.BuildFill(rect.FillType, rect.FillColor))
            .Build();
    }

    private static SExpr BuildPolyline(IReadOnlyList<CoordPoint> vertices, Coord width, LineStyle style, EdaColor color = default, SchFillType fillType = SchFillType.None, EdaColor fillColor = default)
    {
        return new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints(vertices))
            .AddChild(WriterHelper.BuildStroke(width, style, color))
            .AddChild(WriterHelper.BuildFill(fillType, fillColor))
            .Build();
    }

    private static SExpr BuildPolygon(KiCadSchPolygon poly)
    {
        return new SExpressionBuilder("polyline")
            .AddChild(WriterHelper.BuildPoints(poly.Vertices))
            .AddChild(WriterHelper.BuildStroke(poly.LineWidth, poly.LineStyle, poly.Color))
            .AddChild(WriterHelper.BuildFill(poly.FillType, poly.FillColor))
            .Build();
    }

    private static SExpr BuildArc(KiCadSchArc arc)
    {
        return new SExpressionBuilder("arc")
            .AddChild("start", s => { s.AddValue(arc.ArcStart.X.ToMm()); s.AddValue(arc.ArcStart.Y.ToMm()); })
            .AddChild("mid", m => { m.AddValue(arc.ArcMid.X.ToMm()); m.AddValue(arc.ArcMid.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(arc.ArcEnd.X.ToMm()); e.AddValue(arc.ArcEnd.Y.ToMm()); })
            .AddChild(WriterHelper.BuildStroke(arc.LineWidth, arc.LineStyle, arc.Color))
            .AddChild(WriterHelper.BuildFill(arc.FillType, arc.FillColor))
            .Build();
    }

    private static SExpr BuildCircle(KiCadSchCircle circle)
    {
        return new SExpressionBuilder("circle")
            .AddChild("center", c => { c.AddValue(circle.Center.X.ToMm()); c.AddValue(circle.Center.Y.ToMm()); })
            .AddChild("radius", r => r.AddValue(circle.Radius.ToMm()))
            .AddChild(WriterHelper.BuildStroke(circle.LineWidth, circle.LineStyle, circle.Color))
            .AddChild(WriterHelper.BuildFill(circle.FillType, circle.FillColor))
            .Build();
    }

    private static SExpr BuildBezier(KiCadSchBezier bezier)
    {
        return new SExpressionBuilder("bezier")
            .AddChild(WriterHelper.BuildPoints(bezier.ControlPoints))
            .AddChild(WriterHelper.BuildStroke(bezier.LineWidth, bezier.LineStyle, bezier.Color))
            .AddChild(WriterHelper.BuildFill(bezier.FillType, bezier.FillColor))
            .Build();
    }

    private static SExpr BuildTextLabel(KiCadSchLabel label)
    {
        var fontH = label.FontSizeHeight != Coord.Zero ? label.FontSizeHeight : WriterHelper.DefaultTextSize;
        var fontW = label.FontSizeWidth != Coord.Zero ? label.FontSizeWidth : WriterHelper.DefaultTextSize;
        var b = new SExpressionBuilder("text")
            .AddValue(label.Text)
            .AddChild(WriterHelper.BuildPosition(label.Location, label.Rotation));

        // Write stroke if present
        if (label.StrokeWidth != Coord.Zero || label.StrokeColor != default)
            b.AddChild(WriterHelper.BuildStroke(label.StrokeWidth, label.StrokeLineStyle, label.StrokeColor));

        b.AddChild(WriterHelper.BuildTextEffects(fontH, fontW, label.Justification, label.IsHidden, label.IsMirrored, label.IsBold, label.IsItalic));
        return b.Build();
    }
}
