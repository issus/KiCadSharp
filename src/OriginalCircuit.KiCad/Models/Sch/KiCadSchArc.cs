using OriginalCircuit.Eda.Enums;
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
    public CoordPoint Center { get; set; }

    /// <inheritdoc />
    public Coord Radius { get; set; }

    /// <inheritdoc />
    public double StartAngle { get; set; }

    /// <inheritdoc />
    public double EndAngle { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets the fill color.
    /// </summary>
    public EdaColor FillColor { get; set; }

    /// <summary>
    /// Gets the original start point from the KiCad file.
    /// </summary>
    public CoordPoint ArcStart { get; set; }

    /// <summary>
    /// Gets the original mid point from the KiCad file.
    /// </summary>
    public CoordPoint ArcMid { get; set; }

    /// <summary>
    /// Gets the original end point from the KiCad file.
    /// </summary>
    public CoordPoint ArcEnd { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute([ArcStart, ArcMid, ArcEnd]);
}
