using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic power object, mapped from power symbols (symbols with power_in pins flagged specially).
/// </summary>
public sealed class KiCadSchPowerObject : ISchPowerObject
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string? Text { get; set; }

    /// <inheritdoc />
    public PowerPortStyle Style { get; set; }

    /// <inheritdoc />
    public double Rotation { get; set; }

    /// <inheritdoc />
    public bool ShowNetName { get; set; } = true;

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Gets the UUID of the power object.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the raw S-expression node for round-trip fidelity.
    /// </summary>
    public SExpr? RawNode { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
