using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// Base class for board-level graphic elements (gr_line, gr_arc, gr_circle, gr_rect, gr_poly, gr_curve).
/// </summary>
public abstract class KiCadPcbGraphic
{
    /// <summary>
    /// Gets or sets the layer name.
    /// </summary>
    public string? LayerName { get; set; }

    /// <summary>
    /// Gets or sets the stroke width.
    /// </summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>
    /// Gets or sets the stroke style.
    /// </summary>
    public LineStyle StrokeStyle { get; set; } = LineStyle.Solid;

    /// <summary>
    /// Gets or sets the stroke color.
    /// </summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>
    /// Gets or sets the UUID.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets or sets the fill color.
    /// </summary>
    public EdaColor FillColor { get; set; }

    /// <summary>
    /// Gets or sets whether this graphic is locked.
    /// </summary>
    public bool IsLocked { get; set; }
}

/// <summary>
/// Board-level graphic line (<c>gr_line</c>).
/// </summary>
public sealed class KiCadPcbGraphicLine : KiCadPcbGraphic
{
    /// <summary>Gets or sets the start point.</summary>
    public CoordPoint Start { get; set; }

    /// <summary>Gets or sets the end point.</summary>
    public CoordPoint End { get; set; }
}

/// <summary>
/// Board-level graphic arc (<c>gr_arc</c>).
/// </summary>
public sealed class KiCadPcbGraphicArc : KiCadPcbGraphic
{
    /// <summary>Gets or sets the start point.</summary>
    public CoordPoint Start { get; set; }

    /// <summary>Gets or sets the mid point.</summary>
    public CoordPoint Mid { get; set; }

    /// <summary>Gets or sets the end point.</summary>
    public CoordPoint End { get; set; }
}

/// <summary>
/// Board-level graphic circle (<c>gr_circle</c>).
/// </summary>
public sealed class KiCadPcbGraphicCircle : KiCadPcbGraphic
{
    /// <summary>Gets or sets the center point.</summary>
    public CoordPoint Center { get; set; }

    /// <summary>Gets or sets the end point (defines radius).</summary>
    public CoordPoint End { get; set; }
}

/// <summary>
/// Board-level graphic rectangle (<c>gr_rect</c>).
/// </summary>
public sealed class KiCadPcbGraphicRect : KiCadPcbGraphic
{
    /// <summary>Gets or sets the start corner point.</summary>
    public CoordPoint Start { get; set; }

    /// <summary>Gets or sets the end corner point.</summary>
    public CoordPoint End { get; set; }
}

/// <summary>
/// Board-level graphic polygon (<c>gr_poly</c>).
/// </summary>
public sealed class KiCadPcbGraphicPoly : KiCadPcbGraphic
{
    /// <summary>Gets or sets the polygon points.</summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];
}

/// <summary>
/// Board-level graphic bezier curve (<c>gr_curve</c> / <c>bezier</c>).
/// </summary>
public sealed class KiCadPcbGraphicBezier : KiCadPcbGraphic
{
    /// <summary>Gets or sets the bezier control points (4 points: start, control1, control2, end).</summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];
}
