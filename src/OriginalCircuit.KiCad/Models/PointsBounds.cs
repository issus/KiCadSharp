using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models;

/// <summary>
/// Helper for computing bounding rectangles from point collections.
/// </summary>
internal static class PointsBounds
{
    /// <summary>
    /// Computes the bounding rectangle of a set of points.
    /// </summary>
    /// <param name="points">The points to compute bounds for.</param>
    /// <returns>The bounding rectangle, or <see cref="CoordRect.Empty"/> if the collection is empty.</returns>
    public static CoordRect Compute(IReadOnlyList<CoordPoint> points)
    {
        if (points.Count == 0) return CoordRect.Empty;
        var minX = points[0].X;
        var minY = points[0].Y;
        var maxX = minX;
        var maxY = minY;
        for (var i = 1; i < points.Count; i++)
        {
            var p = points[i];
            minX = Coord.Min(minX, p.X);
            minY = Coord.Min(minY, p.Y);
            maxX = Coord.Max(maxX, p.X);
            maxY = Coord.Max(maxY, p.Y);
        }
        return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
    }
}
