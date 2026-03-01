using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic rule area, mapped from <c>(rule_area (polyline ...))</c>.
/// Contains a polyline with stroke, fill, and UUID.
/// </summary>
public sealed class KiCadSchRuleArea
{
    /// <summary>Gets or sets the polyline points.</summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];

    /// <summary>Gets or sets the stroke width.</summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>Gets or sets the stroke type.</summary>
    public string? StrokeType { get; set; }

    /// <summary>Gets or sets the fill type.</summary>
    public string? FillType { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets whether the UUID is a bare symbol.</summary>
    public bool UuidIsSymbol { get; set; }
}
