using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic net label, mapped from <c>(label "TEXT" (at X Y ANGLE) (effects ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchNetLabel : ISchNetLabel
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public int Orientation { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <summary>
    /// Gets the UUID of the net label.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
