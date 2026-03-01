using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// Text render cache containing pre-rendered glyph polygons.
/// Mapped from <c>(render_cache "font" SIZE (polygon (pts ...)) ...)</c>.
/// </summary>
public sealed class KiCadTextRenderCache
{
    /// <summary>Gets or sets the font name.</summary>
    public string? FontName { get; set; }

    /// <summary>Gets or sets the font size (width height).</summary>
    public CoordPoint FontSize { get; set; }

    /// <summary>Gets or sets the rendered glyph polygons. Each polygon is a list of points.</summary>
    public List<List<CoordPoint>> Polygons { get; set; } = [];
}
