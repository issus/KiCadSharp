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
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public string? Text { get; internal set; }

    /// <inheritdoc />
    public PowerPortStyle Style { get; internal set; }

    /// <inheritdoc />
    public double Rotation { get; internal set; }

    /// <inheritdoc />
    public bool ShowNetName { get; internal set; } = true;

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public bool IsMirrored { get; internal set; }

    /// <summary>
    /// Gets the UUID of the power object.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
