using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic wire, mapped from <c>(wire (pts (xy X1 Y1) (xy X2 Y2)) (stroke ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchWire : ISchWire
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices { get; internal set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <summary>
    /// Gets the UUID of the wire.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Vertices);
}
