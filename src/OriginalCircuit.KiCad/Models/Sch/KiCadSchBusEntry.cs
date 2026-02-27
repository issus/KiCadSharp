using OriginalCircuit.Eda.Enums;
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
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Gets or sets the line dash style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets the UUID of the bus entry.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Corner);
}
