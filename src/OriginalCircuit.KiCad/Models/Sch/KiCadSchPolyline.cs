using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic polyline, mapped from <c>(polyline (pts ...) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchPolyline : ISchPolyline
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices { get; set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <inheritdoc />
    public LineStyle LineStyle { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Vertices);
}
