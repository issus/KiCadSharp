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

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
