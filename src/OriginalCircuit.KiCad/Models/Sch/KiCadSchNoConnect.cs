using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic no-connect marker, mapped from <c>(no_connect (at X Y) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchNoConnect : ISchNoConnect
{
    /// <inheritdoc />
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <summary>
    /// Gets the UUID of the no-connect marker.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
