using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

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

    /// <summary>Gets or sets whether text effects were present in the source.</summary>
    public bool HasEffects { get; set; }

    /// <summary>Gets or sets whether the position includes the angle.</summary>
    public bool PositionIncludesAngle { get; set; } = true;

    /// <summary>Gets or sets the font height for round-trip fidelity.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width for round-trip fidelity.</summary>
    public Coord FontWidth { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
