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
    public IReadOnlyList<CoordPoint> ControlPoints { get; internal set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => ComputeBounds();

    private CoordRect ComputeBounds()
    {
        if (ControlPoints.Count == 0) return CoordRect.Empty;
        var minX = ControlPoints[0].X;
        var minY = ControlPoints[0].Y;
        var maxX = minX;
        var maxY = minY;
        for (var i = 1; i < ControlPoints.Count; i++)
        {
            var p = ControlPoints[i];
            minX = Coord.Min(minX, p.X);
            minY = Coord.Min(minY, p.Y);
            maxX = Coord.Max(maxX, p.X);
            maxY = Coord.Max(maxY, p.Y);
        }
        return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
    }
}
