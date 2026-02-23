using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic bus, mapped from <c>(bus (pts ...) (stroke ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchBus : ISchBus
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices { get; internal set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <summary>
    /// Gets the UUID of the bus.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Vertices);
}
