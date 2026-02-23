using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic text label, mapped from <c>(text "TEXT" (at X Y ANGLE) (effects ...))</c>.
/// </summary>
public sealed class KiCadSchLabel : ISchLabel
{
    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public TextJustification Justification { get; internal set; }

    /// <inheritdoc />
    public double Rotation { get; internal set; }

    /// <inheritdoc />
    public bool IsMirrored { get; internal set; }

    /// <inheritdoc />
    public bool IsHidden { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
