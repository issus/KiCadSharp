using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.KiCad.Models;
using OriginalCircuit.KiCad.Models.Sch;

namespace OriginalCircuit.KiCad.Rendering;

/// <summary>
/// Renders KiCad schematic components (symbols) to an <see cref="IRenderContext"/>.
/// </summary>
public sealed class KiCadSchRenderer
{
    private readonly CoordTransform _transform;
    private const double MinLineWidth = 1.0;
    private const double DefaultFontSize = 12.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadSchRenderer"/> class.
    /// </summary>
    /// <param name="transform">The coordinate transform for world-to-screen conversion.</param>
    public KiCadSchRenderer(CoordTransform transform)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Renders a schematic component with all its primitives.
    /// </summary>
    /// <param name="component">The schematic component to render.</param>
    /// <param name="ctx">The render context to draw on.</param>
    public void Render(KiCadSchComponent component, IRenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(ctx);

        // Render graphical primitives first, then pins on top
        foreach (var rect in component.Rectangles) RenderRectangle(rect, ctx);
        foreach (var circle in component.Circles) RenderCircle(circle, ctx);
        foreach (var arc in component.Arcs) RenderArc(arc, ctx);
        foreach (var poly in component.Polylines) RenderPolyline(poly, ctx);
        foreach (var poly in component.Polygons) RenderPolygon(poly, ctx);
        foreach (var bez in component.Beziers) RenderBezier(bez, ctx);
        foreach (var line in component.Lines) RenderLine(line, ctx);
        foreach (var pin in component.Pins) RenderPin(pin, ctx, component);
        foreach (var param in component.Parameters) RenderParameter(param, ctx);

        // Render sub-symbols
        foreach (var sub in component.SubSymbols)
        {
            Render(sub, ctx);
        }
    }

    /// <summary>
    /// Renders an entire schematic document including wires, junctions, labels,
    /// buses, no-connects, sheets, images, and placed symbol instances.
    /// </summary>
    /// <param name="schematic">The schematic document to render.</param>
    /// <param name="ctx">The render context to draw on.</param>
    public void RenderDocument(KiCadSch schematic, IRenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(ctx);

        // Render connectivity elements first (background)
        foreach (var wire in schematic.Wires) RenderWire(wire, ctx);
        foreach (var bus in schematic.Buses) RenderBus(bus, ctx);
        foreach (var entry in schematic.BusEntries) RenderBusEntry(entry, ctx);

        // Render markers
        foreach (var junction in schematic.Junctions) RenderJunction(junction, ctx);
        foreach (var nc in schematic.NoConnects) RenderNoConnect(nc, ctx);

        // Render sheets
        foreach (var sheet in schematic.Sheets) RenderSheet(sheet, ctx);

        // Render placed symbols (using lib symbols for graphical data)
        foreach (var comp in schematic.Components)
        {
            if (comp is KiCadSchComponent kComp)
                Render(kComp, ctx);
        }

        // Render labels on top
        foreach (var label in schematic.Labels) RenderLabel(label, ctx);
        foreach (var netLabel in schematic.NetLabels) RenderNetLabel(netLabel, ctx);
    }

    /// <summary>
    /// Renders a schematic pin with its line, name, and designator.
    /// </summary>
    public void RenderPin(ISchPin pin, IRenderContext ctx, KiCadSchComponent? parent = null)
    {
        if (pin.IsHidden) return;

        var (sx, sy) = _transform.WorldToScreen(pin.Location);
        var len = _transform.ScaleValue(pin.Length);

        double ex = sx, ey = sy;
        double nameOffsetX = 0, nameOffsetY = 0;
        double numOffsetX = 0, numOffsetY = 0;

        switch (pin.Orientation)
        {
            case PinOrientation.Right:
                ex = sx + len;
                nameOffsetX = len + 4;
                numOffsetX = len / 2;
                numOffsetY = -4;
                break;
            case PinOrientation.Left:
                ex = sx - len;
                nameOffsetX = -(len + 4);
                numOffsetX = -(len / 2);
                numOffsetY = -4;
                break;
            case PinOrientation.Up:
                ey = sy - len; // screen Y is inverted
                nameOffsetY = -(len + 4);
                numOffsetX = 4;
                numOffsetY = -(len / 2);
                break;
            case PinOrientation.Down:
                ey = sy + len;
                nameOffsetY = len + 4;
                numOffsetX = 4;
                numOffsetY = len / 2;
                break;
        }

        // Draw pin line
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(Coord.FromMm(0.15)));
        ctx.DrawLine(sx, sy, ex, ey, KiCadLayerColors.SchematicPin, lineWidth);

        // Draw graphic style decorations
        if (pin is KiCadSchPin kPin)
        {
            RenderPinGraphicStyle(kPin, ctx, sx, sy, ex, ey, lineWidth);
        }

        // Draw pin name
        var hidePinNames = parent?.HidePinNames ?? false;
        if (pin.ShowName && !hidePinNames && !string.IsNullOrEmpty(pin.Name) && pin.Name != "~")
        {
            var fontSize = Math.Max(6, _transform.ScaleValue(Coord.FromMm(1.27)));
            var textOptions = new TextRenderOptions
            {
                HorizontalAlignment = pin.Orientation is PinOrientation.Left ? TextHAlign.Right : TextHAlign.Left,
                VerticalAlignment = TextVAlign.Middle,
            };
            ctx.DrawText(pin.Name, ex + nameOffsetX, ey + nameOffsetY, fontSize,
                KiCadLayerColors.SchematicPinName, textOptions);
        }

        // Draw pin number (designator)
        var hidePinNumbers = parent?.HidePinNumbers ?? false;
        if (pin.ShowDesignator && !hidePinNumbers && !string.IsNullOrEmpty(pin.Designator))
        {
            var fontSize = Math.Max(6, _transform.ScaleValue(Coord.FromMm(1.27)));
            var textOptions = new TextRenderOptions
            {
                HorizontalAlignment = TextHAlign.Center,
                VerticalAlignment = TextVAlign.Baseline,
            };
            ctx.DrawText(pin.Designator, sx + numOffsetX, sy + numOffsetY, fontSize,
                KiCadLayerColors.SchematicPinNumber, textOptions);
        }
    }

    private void RenderPinGraphicStyle(KiCadSchPin pin, IRenderContext ctx,
        double sx, double sy, double ex, double ey, double lineWidth)
    {
        var bubbleRadius = Math.Max(3, _transform.ScaleValue(Coord.FromMm(0.5)));

        switch (pin.GraphicStyle)
        {
            case PinGraphicStyle.Inverted:
                // Draw inversion bubble at pin end
                ctx.DrawEllipse(ex, ey, bubbleRadius, bubbleRadius, KiCadLayerColors.SchematicPin, lineWidth);
                break;

            case PinGraphicStyle.Clock:
                // Draw clock triangle at pin connection point
                RenderClockTriangle(ctx, sx, sy, pin.Orientation, lineWidth);
                break;

            case PinGraphicStyle.InvertedClock:
                ctx.DrawEllipse(ex, ey, bubbleRadius, bubbleRadius, KiCadLayerColors.SchematicPin, lineWidth);
                RenderClockTriangle(ctx, sx, sy, pin.Orientation, lineWidth);
                break;
        }
    }

    private void RenderClockTriangle(IRenderContext ctx, double x, double y,
        PinOrientation orientation, double lineWidth)
    {
        var size = Math.Max(4, _transform.ScaleValue(Coord.FromMm(0.6)));
        double[] xs, ys;

        switch (orientation)
        {
            case PinOrientation.Right:
                xs = [x - size, x, x - size];
                ys = [y - size, y, y + size];
                break;
            case PinOrientation.Left:
                xs = [x + size, x, x + size];
                ys = [y - size, y, y + size];
                break;
            case PinOrientation.Up:
                xs = [x - size, x, x + size];
                ys = [y + size, y, y + size];
                break;
            default: // Down
                xs = [x - size, x, x + size];
                ys = [y - size, y, y - size];
                break;
        }

        ctx.DrawPolyline(xs, ys, KiCadLayerColors.SchematicPin, lineWidth);
    }

    /// <summary>
    /// Renders a schematic line.
    /// </summary>
    public void RenderLine(ISchLine line, IRenderContext ctx)
    {
        var (x1, y1) = _transform.WorldToScreen(line.Start);
        var (x2, y2) = _transform.WorldToScreen(line.End);
        var color = ResolveColor(line.Color, KiCadLayerColors.SchematicDefault);
        var width = Math.Max(MinLineWidth, _transform.ScaleValue(line.Width));
        ctx.DrawLine(x1, y1, x2, y2, color, width, ToRenderLineStyle(line.LineStyle));
    }

    /// <summary>
    /// Renders a schematic rectangle.
    /// </summary>
    public void RenderRectangle(ISchRectangle rect, IRenderContext ctx)
    {
        var (x1, y1) = _transform.WorldToScreen(rect.Corner1);
        var (x2, y2) = _transform.WorldToScreen(rect.Corner2);
        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        // Fill first
        if (rect.IsFilled)
        {
            var fillColor = ResolveFillColor(rect.FillColor, rect is KiCadSchRectangle kr ? kr.FillType : SchFillType.None);
            if (fillColor != 0)
            {
                ctx.FillRectangle(x, y, w, h, fillColor);
            }
        }

        // Then stroke
        var color = ResolveColor(rect.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(rect.LineWidth));
        ctx.DrawRectangle(x, y, w, h, color, lineWidth);
    }

    /// <summary>
    /// Renders a schematic arc.
    /// </summary>
    public void RenderArc(ISchArc arc, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center);
        var r = _transform.ScaleValue(arc.Radius);
        var color = ResolveColor(arc.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(arc.LineWidth));

        // Convert angles: KiCad uses math convention (CCW from +X), screen uses CW from +X
        var (startAngle, sweepAngle) = ComputeScreenArcAngles(arc.StartAngle, arc.EndAngle);

        // Normalize sweep
        if (sweepAngle > 360) sweepAngle -= 360;
        if (sweepAngle < -360) sweepAngle += 360;

        ctx.DrawArc(cx, cy, r, r, startAngle, sweepAngle, color, lineWidth);
    }

    /// <summary>
    /// Renders a schematic circle.
    /// </summary>
    public void RenderCircle(ISchCircle circle, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(circle.Center);
        var r = _transform.ScaleValue(circle.Radius);

        // Fill first
        if (circle.IsFilled)
        {
            var fillColor = ResolveFillColor(circle.FillColor, circle is KiCadSchCircle kc ? kc.FillType : SchFillType.None);
            if (fillColor != 0)
            {
                ctx.FillEllipse(cx, cy, r, r, fillColor);
            }
        }

        var color = ResolveColor(circle.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(circle.LineWidth));
        ctx.DrawEllipse(cx, cy, r, r, color, lineWidth);
    }

    /// <summary>
    /// Renders a schematic polyline.
    /// </summary>
    public void RenderPolyline(ISchPolyline polyline, IRenderContext ctx)
    {
        if (polyline.Vertices.Count < 2) return;

        var xs = new double[polyline.Vertices.Count];
        var ys = new double[polyline.Vertices.Count];
        for (int i = 0; i < polyline.Vertices.Count; i++)
        {
            (xs[i], ys[i]) = _transform.WorldToScreen(polyline.Vertices[i]);
        }

        var color = ResolveColor(polyline.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(polyline.LineWidth));
        ctx.DrawPolyline(xs, ys, color, lineWidth, ToRenderLineStyle(polyline.LineStyle));
    }

    /// <summary>
    /// Renders a schematic polygon.
    /// </summary>
    public void RenderPolygon(ISchPolygon polygon, IRenderContext ctx)
    {
        if (polygon.Vertices.Count < 3) return;

        var xs = new double[polygon.Vertices.Count];
        var ys = new double[polygon.Vertices.Count];
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            (xs[i], ys[i]) = _transform.WorldToScreen(polygon.Vertices[i]);
        }

        // Fill
        if (polygon.IsFilled)
        {
            var fillColor = ResolveFillColor(polygon.FillColor, polygon is KiCadSchPolygon kp ? kp.FillType : SchFillType.None);
            if (fillColor != 0)
            {
                ctx.FillPolygon(xs, ys, fillColor);
            }
        }

        // Stroke
        var color = ResolveColor(polygon.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(polygon.LineWidth));
        ctx.DrawPolygon(xs, ys, color, lineWidth);
    }

    /// <summary>
    /// Renders a schematic bezier curve.
    /// </summary>
    public void RenderBezier(ISchBezier bezier, IRenderContext ctx)
    {
        if (bezier.ControlPoints.Count < 4) return;

        var color = ResolveColor(bezier.Color, KiCadLayerColors.SchematicDefault);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(bezier.LineWidth));

        // Render each group of 4 control points as a cubic bezier
        for (int i = 0; i + 3 < bezier.ControlPoints.Count; i += 3)
        {
            var (x0, y0) = _transform.WorldToScreen(bezier.ControlPoints[i]);
            var (x1, y1) = _transform.WorldToScreen(bezier.ControlPoints[i + 1]);
            var (x2, y2) = _transform.WorldToScreen(bezier.ControlPoints[i + 2]);
            var (x3, y3) = _transform.WorldToScreen(bezier.ControlPoints[i + 3]);

            ctx.DrawBezier(x0, y0, x1, y1, x2, y2, x3, y3, color, lineWidth);
        }
    }

    /// <summary>
    /// Renders a schematic parameter (property text).
    /// </summary>
    public void RenderParameter(ISchParameter param, IRenderContext ctx)
    {
        if (!param.IsVisible || string.IsNullOrEmpty(param.Value)) return;

        var (x, y) = _transform.WorldToScreen(param.Location);
        var color = ResolveColor(param.Color, KiCadLayerColors.SchematicText);

        double fontSize = DefaultFontSize;
        if (param is KiCadSchParameter kParam && kParam.FontSizeHeight.ToRaw() > 0)
        {
            fontSize = Math.Max(6, _transform.ScaleValue(kParam.FontSizeHeight));
        }

        var (hAlign, vAlign) = MapJustification(param.Justification);

        var options = new TextRenderOptions
        {
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign,
        };

        if (param.Orientation != 0)
        {
            ctx.SaveState();
            ctx.Translate(x, y);
            ctx.Rotate(-param.Orientation);
            ctx.DrawText(param.Value, 0, 0, fontSize, color, options);
            ctx.RestoreState();
        }
        else
        {
            ctx.DrawText(param.Value, x, y, fontSize, color, options);
        }
    }

    /// <summary>
    /// Renders a schematic label.
    /// </summary>
    public void RenderLabel(ISchLabel label, IRenderContext ctx)
    {
        if (label.IsHidden) return;

        var (x, y) = _transform.WorldToScreen(label.Location);
        var color = ResolveColor(label.Color, KiCadLayerColors.SchematicText);

        var (hAlign, vAlign) = MapJustification(label.Justification);
        var options = new TextRenderOptions
        {
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign,
        };

        if (label.Rotation != 0)
        {
            ctx.SaveState();
            ctx.Translate(x, y);
            ctx.Rotate(-label.Rotation);
            ctx.DrawText(label.Text, 0, 0, DefaultFontSize, color, options);
            ctx.RestoreState();
        }
        else
        {
            ctx.DrawText(label.Text, x, y, DefaultFontSize, color, options);
        }
    }

    /// <summary>
    /// Renders a schematic junction (connection dot).
    /// </summary>
    public void RenderJunction(ISchJunction junction, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(junction.Location);
        var r = Math.Max(3, _transform.ScaleValue(junction.Size) / 2);
        ctx.FillEllipse(cx, cy, r, r, KiCadLayerColors.SchematicJunction);
    }

    /// <summary>
    /// Renders a schematic net label.
    /// </summary>
    public void RenderNetLabel(ISchNetLabel netLabel, IRenderContext ctx)
    {
        var (x, y) = _transform.WorldToScreen(netLabel.Location);
        var color = ResolveColor(netLabel.Color, KiCadLayerColors.SchematicNetLabel);
        var (hAlign, vAlign) = MapJustification(netLabel.Justification);
        var options = new TextRenderOptions
        {
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign,
        };

        if (netLabel.Orientation != 0)
        {
            ctx.SaveState();
            ctx.Translate(x, y);
            ctx.Rotate(-netLabel.Orientation);
            ctx.DrawText(netLabel.Text, 0, 0, DefaultFontSize, color, options);
            ctx.RestoreState();
        }
        else
        {
            ctx.DrawText(netLabel.Text, x, y, DefaultFontSize, color, options);
        }
    }

    /// <summary>
    /// Renders a schematic wire.
    /// </summary>
    public void RenderWire(ISchWire wire, IRenderContext ctx)
    {
        if (wire.Vertices.Count < 2) return;

        var xs = new double[wire.Vertices.Count];
        var ys = new double[wire.Vertices.Count];
        for (int i = 0; i < wire.Vertices.Count; i++)
        {
            (xs[i], ys[i]) = _transform.WorldToScreen(wire.Vertices[i]);
        }

        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(wire.LineWidth));
        ctx.DrawPolyline(xs, ys, KiCadLayerColors.SchematicWire, lineWidth);
    }

    /// <summary>
    /// Renders a schematic bus.
    /// </summary>
    public void RenderBus(ISchBus bus, IRenderContext ctx)
    {
        if (bus.Vertices.Count < 2) return;

        var xs = new double[bus.Vertices.Count];
        var ys = new double[bus.Vertices.Count];
        for (int i = 0; i < bus.Vertices.Count; i++)
        {
            (xs[i], ys[i]) = _transform.WorldToScreen(bus.Vertices[i]);
        }

        var lineWidth = Math.Max(MinLineWidth * 2, _transform.ScaleValue(bus.LineWidth));
        ctx.DrawPolyline(xs, ys, KiCadLayerColors.SchematicBus, lineWidth);
    }

    /// <summary>
    /// Renders a schematic bus entry.
    /// </summary>
    public void RenderBusEntry(ISchBusEntry entry, IRenderContext ctx)
    {
        var (x1, y1) = _transform.WorldToScreen(entry.Location);
        var (x2, y2) = _transform.WorldToScreen(entry.Corner);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(entry.LineWidth));
        ctx.DrawLine(x1, y1, x2, y2, KiCadLayerColors.SchematicBus, lineWidth);
    }

    /// <summary>
    /// Renders a schematic no-connect marker (X shape).
    /// </summary>
    public void RenderNoConnect(ISchNoConnect nc, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(nc.Location);
        // Default no-connect size when not specified by interface
        var size = Math.Max(5, _transform.ScaleValue(Coord.FromMm(0.5)));
        var lineWidth = Math.Max(MinLineWidth, 2.0);

        ctx.DrawLine(cx - size, cy - size, cx + size, cy + size, KiCadLayerColors.SchematicNoConnect, lineWidth);
        ctx.DrawLine(cx - size, cy + size, cx + size, cy - size, KiCadLayerColors.SchematicNoConnect, lineWidth);
    }

    /// <summary>
    /// Renders a schematic sheet (hierarchical sheet rectangle with pins).
    /// </summary>
    public void RenderSheet(ISchSheet sheet, IRenderContext ctx)
    {
        var (x, y) = _transform.WorldToScreen(sheet.Location);
        var w = _transform.ScaleValue(sheet.Size.X);
        var h = _transform.ScaleValue(sheet.Size.Y);

        // In screen space, Y is inverted, so adjust
        var screenY = y - h;

        // Fill
        if (ColorHelper.IsNonZero(sheet.FillColor))
        {
            ctx.FillRectangle(x, screenY, w, h, ColorHelper.EdaColorToArgb(sheet.FillColor));
        }
        else
        {
            ctx.FillRectangle(x, screenY, w, h, KiCadLayerColors.SchematicBackgroundFill);
        }

        // Border
        var borderColor = ResolveColor(sheet.Color, KiCadLayerColors.SchematicSheet);
        var lineWidth = Math.Max(MinLineWidth, _transform.ScaleValue(sheet.LineWidth));
        ctx.DrawRectangle(x, screenY, w, h, borderColor, lineWidth);

        // Sheet name
        if (!string.IsNullOrEmpty(sheet.SheetName))
        {
            ctx.DrawText(sheet.SheetName, x + 4, screenY - 4, DefaultFontSize, KiCadLayerColors.SchematicSheet);
        }
    }

    /// <summary>
    /// Renders a schematic image.
    /// </summary>
    public void RenderImage(ISchImage image, IRenderContext ctx)
    {
        if (image.ImageData == null || image.ImageData.Length == 0) return;

        var (x1, y1) = _transform.WorldToScreen(image.Corner1);
        var (x2, y2) = _transform.WorldToScreen(image.Corner2);
        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        ctx.DrawImage(image.ImageData, x, y, w, h);
    }

    /// <summary>
    /// Converts KiCad world-space arc angles (CCW from +X) to screen-space
    /// angles (CW from +X due to Y-axis inversion).
    /// </summary>
    internal static (double startAngle, double sweepAngle) ComputeScreenArcAngles(double worldStart, double worldEnd)
    {
        return (-worldStart, -(worldEnd - worldStart));
    }

    private static uint ResolveColor(EdaColor color, uint defaultColor)
    {
        if (ColorHelper.IsNonZero(color))
            return ColorHelper.EdaColorToArgb(color);
        return defaultColor;
    }

    private static uint ResolveFillColor(EdaColor fillColor, SchFillType fillType)
    {
        return fillType switch
        {
            SchFillType.Filled when ColorHelper.IsNonZero(fillColor) => ColorHelper.EdaColorToArgb(fillColor),
            SchFillType.Filled => KiCadLayerColors.SchematicFill,
            SchFillType.Background => KiCadLayerColors.SchematicBackgroundFill,
            _ => 0
        };
    }

    private static LineStyle ToRenderLineStyle(LineStyle style) => style;

    private static (TextHAlign h, TextVAlign v) MapJustification(TextJustification j)
    {
        return j switch
        {
            TextJustification.TopLeft => (TextHAlign.Left, TextVAlign.Top),
            TextJustification.TopCenter => (TextHAlign.Center, TextVAlign.Top),
            TextJustification.TopRight => (TextHAlign.Right, TextVAlign.Top),
            TextJustification.MiddleLeft => (TextHAlign.Left, TextVAlign.Middle),
            TextJustification.MiddleCenter => (TextHAlign.Center, TextVAlign.Middle),
            TextJustification.MiddleRight => (TextHAlign.Right, TextVAlign.Middle),
            TextJustification.BottomLeft => (TextHAlign.Left, TextVAlign.Bottom),
            TextJustification.BottomCenter => (TextHAlign.Center, TextVAlign.Bottom),
            TextJustification.BottomRight => (TextHAlign.Right, TextVAlign.Bottom),
            _ => (TextHAlign.Left, TextVAlign.Baseline)
        };
    }
}
