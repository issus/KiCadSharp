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
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <summary>
    /// Gets the UUID of the no-connect marker.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
