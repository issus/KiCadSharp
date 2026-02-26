using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.KiCad.Models.Pcb;

namespace OriginalCircuit.KiCad.Rendering;

/// <summary>
/// Renders KiCad PCB components (footprints) to an <see cref="IRenderContext"/>.
/// </summary>
public sealed class KiCadPcbRenderer
{
    private readonly CoordTransform _transform;
    private const double MinLineWidth = 1.0;
    private const double DefaultFontSize = 10.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadPcbRenderer"/> class.
    /// </summary>
    /// <param name="transform">The coordinate transform for world-to-screen conversion.</param>
    public KiCadPcbRenderer(CoordTransform transform)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Renders a PCB footprint component with all its primitives.
    /// Primitives are sorted by layer draw priority.
    /// </summary>
    /// <param name="component">The PCB component to render.</param>
    /// <param name="ctx">The render context to draw on.</param>
    public void Render(KiCadPcbComponent component, IRenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(ctx);

        // Collect all primitives with layer info and sort by draw priority
        var items = new List<(int priority, Action draw)>();

        foreach (var region in component.Regions)
        {
            var r = region;
            var layer = r is KiCadPcbRegion kr ? kr.LayerName : null;
            items.Add((KiCadLayerColors.GetPriority(layer), () => RenderRegion(r, ctx)));
        }

        foreach (var track in component.Tracks)
        {
            var t = track;
            var layer = t is KiCadPcbTrack kt ? kt.LayerName : null;
            items.Add((KiCadLayerColors.GetPriority(layer), () => RenderTrack(t, ctx)));
        }

        foreach (var arc in component.Arcs)
        {
            var a = arc;
            var layer = a is KiCadPcbArc ka ? ka.LayerName : null;
            items.Add((KiCadLayerColors.GetPriority(layer), () => RenderArc(a, ctx)));
        }

        foreach (var text in component.Texts)
        {
            var t = text;
            var layer = t is KiCadPcbText kt ? kt.LayerName : null;
            items.Add((KiCadLayerColors.GetPriority(layer), () => RenderText(t, ctx)));
        }

        foreach (var pad in component.Pads)
        {
            var p = pad;
            var padLayer = p is KiCadPcbPad kp ? kp.Layers.FirstOrDefault() ?? "F.Cu" : "F.Cu";
            items.Add((KiCadLayerColors.GetPriority(padLayer), () => RenderPad(p, ctx)));
        }

        foreach (var via in component.Vias)
        {
            var v = via;
            var viaLayer = v is KiCadPcbVia kv ? kv.StartLayerName ?? "F.Cu" : "F.Cu";
            items.Add((KiCadLayerColors.GetPriority(viaLayer) + 1, () => RenderVia(v, ctx)));
        }

        // Sort by priority (lower drawn first) and render
        items.Sort((a, b) => a.priority.CompareTo(b.priority));
        foreach (var (_, draw) in items)
        {
            draw();
        }
    }

    /// <summary>
    /// Renders a PCB pad.
    /// </summary>
    public void RenderPad(IPcbPad pad, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(pad.Location);
        var sw = _transform.ScaleValue(pad.Size.X);
        var sh = _transform.ScaleValue(pad.Size.Y);

        // Determine pad color from first layer
        var color = GetPadColor(pad);

        if (pad.Rotation != 0)
        {
            ctx.SaveState();
            ctx.Translate(cx, cy);
            ctx.Rotate(-pad.Rotation);
            RenderPadShape(pad.Shape, 0, 0, sw, sh, color, pad.CornerRadiusPercentage, ctx);
            ctx.RestoreState();
        }
        else
        {
            RenderPadShape(pad.Shape, cx, cy, sw, sh, color, pad.CornerRadiusPercentage, ctx);
        }

        // Draw drill hole
        if (pad.HoleSize.ToRaw() > 0)
        {
            var holeR = _transform.ScaleValue(pad.HoleSize) / 2;
            var holeCx = pad.Rotation != 0 ? cx : cx;
            var holeCy = pad.Rotation != 0 ? cy : cy;
            ctx.FillEllipse(holeCx, holeCy, holeR, holeR, ColorHelper.White);
            ctx.DrawEllipse(holeCx, holeCy, holeR, holeR, ColorHelper.Gray, Math.Max(MinLineWidth, 0.5));
        }

        // Draw pad designator
        if (!string.IsNullOrEmpty(pad.Designator))
        {
            var fontSize = Math.Min(sw * 0.6, sh * 0.6);
            if (fontSize > 4)
            {
                var textOptions = new TextRenderOptions
                {
                    HorizontalAlignment = TextHAlign.Center,
                    VerticalAlignment = TextVAlign.Middle,
                };
                ctx.DrawText(pad.Designator, cx, cy, fontSize, ColorHelper.White, textOptions);
            }
        }
    }

    private static void RenderPadShape(PadShape shape, double cx, double cy,
        double w, double h, uint color, int cornerRadiusPct, IRenderContext ctx)
    {
        switch (shape)
        {
            case PadShape.Circle:
                var r = Math.Max(w, h) / 2;
                ctx.FillEllipse(cx, cy, r, r, color);
                break;

            case PadShape.Oval:
                var rx = w / 2;
                var ry = h / 2;
                ctx.FillEllipse(cx, cy, rx, ry, color);
                break;

            case PadShape.Rect:
                ctx.FillRectangle(cx - w / 2, cy - h / 2, w, h, color);
                break;

            case PadShape.RoundRect:
                var cornerR = Math.Min(w, h) * cornerRadiusPct / 200.0;
                ctx.FillRoundedRectangle(cx - w / 2, cy - h / 2, w, h, cornerR, color);
                break;

            case PadShape.Trapezoid:
                // Approximate as rectangle
                ctx.FillRectangle(cx - w / 2, cy - h / 2, w, h, color);
                break;

            default:
                // Custom/unknown shapes: draw as rectangle fallback
                ctx.FillRectangle(cx - w / 2, cy - h / 2, w, h, color);
                break;
        }
    }

    private static uint GetPadColor(IPcbPad pad)
    {
        if (pad is KiCadPcbPad kPad && kPad.Layers.Count > 0)
        {
            return KiCadLayerColors.GetColor(kPad.Layers[0]);
        }
        return KiCadLayerColors.GetColor("F.Cu");
    }

    /// <summary>
    /// Renders a PCB track segment.
    /// </summary>
    public void RenderTrack(IPcbTrack track, IRenderContext ctx)
    {
        var (x1, y1) = _transform.WorldToScreen(track.Start);
        var (x2, y2) = _transform.WorldToScreen(track.End);
        var width = Math.Max(MinLineWidth, _transform.ScaleValue(track.Width));
        var layerName = track is KiCadPcbTrack kt ? kt.LayerName : null;
        var color = KiCadLayerColors.GetColor(layerName);
        ctx.DrawLine(x1, y1, x2, y2, color, width);
    }

    /// <summary>
    /// Renders a PCB via.
    /// </summary>
    public void RenderVia(IPcbVia via, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(via.Location);
        var outerR = _transform.ScaleValue(via.Diameter) / 2;
        var innerR = _transform.ScaleValue(via.HoleSize) / 2;

        // Outer annulus (copper)
        ctx.FillEllipse(cx, cy, outerR, outerR, ColorHelper.Gray);

        // Drill hole
        ctx.FillEllipse(cx, cy, innerR, innerR, ColorHelper.White);
        ctx.DrawEllipse(cx, cy, innerR, innerR, ColorHelper.Gray, Math.Max(MinLineWidth, 0.5));
    }

    /// <summary>
    /// Renders a PCB arc.
    /// </summary>
    public void RenderArc(IPcbArc arc, IRenderContext ctx)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center);
        var r = _transform.ScaleValue(arc.Radius);
        var width = Math.Max(MinLineWidth, _transform.ScaleValue(arc.Width));
        var layerName = arc is KiCadPcbArc ka ? ka.LayerName : null;
        var color = KiCadLayerColors.GetColor(layerName);

        var startAngle = -arc.StartAngle;
        var sweepAngle = -(arc.EndAngle - arc.StartAngle);

        if (sweepAngle > 360) sweepAngle -= 360;
        if (sweepAngle < -360) sweepAngle += 360;

        ctx.DrawArc(cx, cy, r, r, startAngle, sweepAngle, color, width);
    }

    /// <summary>
    /// Renders a PCB text element.
    /// </summary>
    public void RenderText(IPcbText text, IRenderContext ctx)
    {
        if (text is KiCadPcbText kt && kt.IsHidden) return;

        var (x, y) = _transform.WorldToScreen(text.Location);
        var fontSize = Math.Max(6, _transform.ScaleValue(text.Height));
        var layerName = text is KiCadPcbText kt2 ? kt2.LayerName : null;
        var color = KiCadLayerColors.GetColor(layerName);

        var options = new TextRenderOptions
        {
            HorizontalAlignment = TextHAlign.Center,
            VerticalAlignment = TextVAlign.Middle,
            Bold = text.FontBold,
            Italic = text.FontItalic,
            FontFamily = text.FontName ?? "Arial",
        };

        if (text.Rotation != 0)
        {
            ctx.SaveState();
            ctx.Translate(x, y);
            ctx.Rotate(-text.Rotation);
            ctx.DrawText(text.Text, 0, 0, fontSize, color, options);
            ctx.RestoreState();
        }
        else
        {
            ctx.DrawText(text.Text, x, y, fontSize, color, options);
        }
    }

    /// <summary>
    /// Renders a PCB region (zone filled polygon).
    /// </summary>
    public void RenderRegion(IPcbRegion region, IRenderContext ctx)
    {
        if (region.Outline.Count < 3) return;

        var xs = new double[region.Outline.Count];
        var ys = new double[region.Outline.Count];
        for (int i = 0; i < region.Outline.Count; i++)
        {
            (xs[i], ys[i]) = _transform.WorldToScreen(region.Outline[i]);
        }

        var layerName = region is KiCadPcbRegion kr ? kr.LayerName : null;
        var color = KiCadLayerColors.GetColor(layerName);

        // Regions are filled copper zones
        ctx.FillPolygon(xs, ys, color);
    }
}
