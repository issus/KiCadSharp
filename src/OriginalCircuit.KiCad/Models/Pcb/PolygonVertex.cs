using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// Represents a vertex in a polygon, which can be either a straight point (xy) or an arc segment (start, mid, end).
/// </summary>
public sealed class PolygonVertex
{
    /// <summary>Gets or sets whether this vertex is an arc segment.</summary>
    public bool IsArc { get; set; }

    /// <summary>Gets or sets the point (for xy vertices).</summary>
    public CoordPoint Point { get; set; }

    /// <summary>Gets or sets the arc start point (for arc vertices).</summary>
    public CoordPoint ArcStart { get; set; }

    /// <summary>Gets or sets the arc midpoint (for arc vertices).</summary>
    public CoordPoint ArcMid { get; set; }

    /// <summary>Gets or sets the arc end point (for arc vertices).</summary>
    public CoordPoint ArcEnd { get; set; }

    /// <summary>Creates a straight point vertex.</summary>
    public static PolygonVertex FromPoint(CoordPoint point) => new() { Point = point };

    /// <summary>Creates an arc vertex.</summary>
    public static PolygonVertex FromArc(CoordPoint start, CoordPoint mid, CoordPoint end) =>
        new() { IsArc = true, ArcStart = start, ArcMid = mid, ArcEnd = end };
}
