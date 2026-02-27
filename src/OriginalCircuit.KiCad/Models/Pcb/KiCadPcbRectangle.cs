using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB rectangle, mapped from <c>(fp_rect (start X Y) (end X Y) (stroke ...) (fill ...) (layer L) (uuid UUID))</c>.
/// Preserves the rectangle identity during round-trip instead of decomposing into lines.
/// </summary>
public sealed class KiCadPcbRectangle
{
    /// <summary>
    /// Gets the start corner of the rectangle.
    /// </summary>
    public CoordPoint Start { get; set; }

    /// <summary>
    /// Gets the end corner of the rectangle (diagonally opposite).
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
    /// Gets whether this rectangle is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    public CoordRect Bounds => new CoordRect(Start, End).Inflate(Width / 2);
}
