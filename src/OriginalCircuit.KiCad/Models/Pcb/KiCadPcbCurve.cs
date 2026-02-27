using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB bezier curve, mapped from <c>(fp_curve (pts (xy X Y) (xy X Y) (xy X Y) (xy X Y)) (stroke ...) (layer L) (uuid UUID))</c>.
/// Has 4 control points for a cubic bezier.
/// </summary>
public sealed class KiCadPcbCurve
{
    /// <summary>
    /// Gets the 4 control points of the cubic bezier curve.
    /// </summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];

    /// <summary>
    /// Gets the stroke width.
    /// </summary>
    public Coord Width { get; set; }

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle StrokeStyle { get; set; } = LineStyle.Solid;

    /// <summary>
    /// Gets the stroke color.
    /// </summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>
    /// Gets the layer number.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether this curve is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    public CoordRect Bounds => PointsBounds.Compute(Points);
}
