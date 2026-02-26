using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic bezier curve, mapped from <c>(bezier (pts ...) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchBezier : ISchBezier
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> ControlPoints { get; set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(ControlPoints);
}
