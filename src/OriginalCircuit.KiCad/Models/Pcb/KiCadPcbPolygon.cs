using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB polygon, mapped from <c>(fp_poly (pts (xy X Y) ...) (stroke ...) (fill ...) (layer L) (uuid UUID))</c>.
/// </summary>
public sealed class KiCadPcbPolygon
{
    /// <summary>
    /// Gets the polygon vertices.
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
    /// Gets the fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets the fill color.
    /// </summary>
    public EdaColor FillColor { get; set; }

    /// <summary>
    /// Gets whether the fill uses PCB format (yes/no/solid) vs schematic format (type none/outline/background).
    /// When true, the writer emits <c>(fill yes)</c> / <c>(fill no)</c> instead of <c>(fill (type ...))</c>.
    /// </summary>
    public bool UsePcbFillFormat { get; set; }

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
    /// Gets whether this polygon is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether the locked state was stored as a child node <c>(locked yes)</c> vs bare symbol.
    /// </summary>
    public bool LockedIsChildNode { get; set; }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    public CoordRect Bounds => PointsBounds.Compute(Points);
}
