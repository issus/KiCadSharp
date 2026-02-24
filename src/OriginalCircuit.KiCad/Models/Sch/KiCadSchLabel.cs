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
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public double Rotation { get; set; }

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <inheritdoc />
    public bool IsHidden { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
