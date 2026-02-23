using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic bus entry, mapped from <c>(bus_entry (at X Y) (size X Y) (stroke ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchBusEntry : ISchBusEntry
{
    /// <inheritdoc />
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public CoordPoint Corner { get; internal set; }

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <summary>
    /// Gets the UUID of the bus entry.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Corner);
}
