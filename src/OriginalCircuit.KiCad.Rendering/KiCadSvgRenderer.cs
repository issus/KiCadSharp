using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.Eda.Rendering.Svg;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;

namespace OriginalCircuit.KiCad.Rendering;

/// <summary>
/// Renders KiCad components to SVG vector graphics.
/// Implements <see cref="ISchLibRenderer"/> and <see cref="IPcbLibRenderer"/> for integration with the shared renderer interfaces.
/// </summary>
public sealed class KiCadSvgRenderer : SvgRendererBase, ISchLibRenderer, IPcbLibRenderer
{
    /// <inheritdoc />
    protected override void RenderComponent(IComponent component, IRenderContext context, CoordTransform transform)
    {
        if (component is KiCadSchComponent sch)
        {
            var renderer = new KiCadSchRenderer(transform);
            renderer.Render(sch, context);
        }
        else if (component is KiCadPcbComponent pcb)
        {
            var renderer = new KiCadPcbRenderer(transform);
            renderer.Render(pcb, context);
        }
    }

    /// <inheritdoc />
    ValueTask ISchLibRenderer.RenderAsync(
        ISchComponent component,
        Stream output,
        RenderOptions? options,
        CancellationToken ct)
    {
        return RenderAsync(component, output, options, ct);
    }

    /// <inheritdoc />
    ValueTask IPcbLibRenderer.RenderAsync(
        IPcbComponent component,
        Stream output,
        RenderOptions? options,
        CancellationToken ct)
    {
        return RenderAsync(component, output, options, ct);
    }
}
