using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB generated element, mapped from <c>(generated (uuid ...) (type ...) ...)</c>.
/// Currently only the <c>tuning_pattern</c> type is known.
/// Properties are stored as key-value pairs for extensibility across KiCad versions.
/// </summary>
public sealed class KiCadPcbGeneratedElement
{
    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets the generated element type (e.g., "tuning_pattern").</summary>
    public string? GeneratedType { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the layer name.</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the base line points.</summary>
    public IReadOnlyList<CoordPoint> BaseLinePoints { get; set; } = [];

    /// <summary>Gets or sets the coupled base line points (for differential pairs).</summary>
    public IReadOnlyList<CoordPoint> BaseLineCoupledPoints { get; set; } = [];

    /// <summary>Gets or sets the origin point.</summary>
    public CoordPoint? Origin { get; set; }

    /// <summary>Gets or sets the end point.</summary>
    public CoordPoint? End { get; set; }

    /// <summary>Gets or sets the member UUIDs.</summary>
    public List<string> Members { get; set; } = [];

    /// <summary>
    /// Gets or sets the scalar properties as ordered key-value-isSymbol tuples.
    /// This stores all simple properties (string/number/bool values) for round-trip fidelity.
    /// </summary>
    public List<(string Key, string Value, bool IsSymbol)> Properties { get; set; } = [];
}
