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
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public string Text { get; internal set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public int Orientation { get; internal set; }

    /// <inheritdoc />
    public TextJustification Justification { get; internal set; }

    /// <summary>
    /// Gets the UUID of the net label.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
