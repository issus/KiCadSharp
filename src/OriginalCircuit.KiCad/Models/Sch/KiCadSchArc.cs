using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic arc, mapped from <c>(arc (start X Y) (mid X Y) (end X Y) (stroke ...) (fill ...))</c>.
/// Center, radius, and angles are computed from the start/mid/end representation.
/// </summary>
public sealed class KiCadSchArc : ISchArc
{
    /// <inheritdoc />
    public CoordPoint Center { get; internal set; }

    /// <inheritdoc />
    public Coord Radius { get; internal set; }

    /// <inheritdoc />
    public double StartAngle { get; internal set; }

    /// <inheritdoc />
    public double EndAngle { get; internal set; }

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <summary>
    /// Gets the original start point from the KiCad file.
    /// </summary>
    public CoordPoint ArcStart { get; internal set; }

    /// <summary>
    /// Gets the original mid point from the KiCad file.
    /// </summary>
    public CoordPoint ArcMid { get; internal set; }

    /// <summary>
    /// Gets the original end point from the KiCad file.
    /// </summary>
    public CoordPoint ArcEnd { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var r = Radius;
            return new CoordRect(
                new CoordPoint(Center.X - r, Center.Y - r),
                new CoordPoint(Center.X + r, Center.Y + r));
        }
    }
}
