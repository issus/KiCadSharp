using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic junction, mapped from <c>(junction (at X Y) (diameter D) (color R G B A) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchJunction : ISchJunction
{
    /// <inheritdoc />
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord Size { get; internal set; }

    /// <summary>
    /// Gets the UUID of the junction.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var half = Size / 2;
            return new CoordRect(
                new CoordPoint(Location.X - half, Location.Y - half),
                new CoordPoint(Location.X + half, Location.Y + half));
        }
    }
}
