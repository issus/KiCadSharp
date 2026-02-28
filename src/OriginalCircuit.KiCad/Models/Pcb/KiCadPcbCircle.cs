using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB circle, mapped from <c>(fp_circle (center X Y) (end X Y) (stroke ...) (fill ...) (layer L) (uuid UUID))</c>.
/// Preserves the circle identity during round-trip instead of converting to an arc.
/// </summary>
public sealed class KiCadPcbCircle
{
    /// <summary>
    /// Gets the center point of the circle.
    /// </summary>
    public CoordPoint Center { get; set; }

    /// <summary>
    /// Gets a point on the circumference of the circle.
    /// </summary>
    public CoordPoint End { get; set; }

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
    /// Gets whether this circle is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether the locked state was stored as a child node <c>(locked yes)</c> vs bare symbol.
    /// </summary>
    public bool LockedIsChildNode { get; set; }

    /// <summary>
    /// Computes the radius of the circle from center and end point.
    /// </summary>
    public Coord Radius
    {
        get
        {
            var dx = End.X.ToMm() - Center.X.ToMm();
            var dy = End.Y.ToMm() - Center.Y.ToMm();
            return Coord.FromMm(Math.Sqrt(dx * dx + dy * dy));
        }
    }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    public CoordRect Bounds
    {
        get
        {
            var r = Radius + Width / 2;
            return new CoordRect(
                new CoordPoint(Center.X - r, Center.Y - r),
                new CoordPoint(Center.X + r, Center.Y + r));
        }
    }
}
