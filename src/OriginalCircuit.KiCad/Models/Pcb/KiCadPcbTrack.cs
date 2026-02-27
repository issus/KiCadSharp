using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB track segment, mapped from <c>(segment (start X Y) (end X Y) (width W) (layer L) (net N) (tstamp UUID))</c>.
/// </summary>
public sealed class KiCadPcbTrack : IPcbTrack
{
    /// <inheritdoc />
    public CoordPoint Start { get; set; }

    /// <inheritdoc />
    public CoordPoint End { get; set; }

    /// <inheritdoc />
    public Coord Width { get; set; }

    /// <inheritdoc />
    public int Layer { get; set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; set; }

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether this track is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle StrokeStyle { get; set; } = LineStyle.Solid;

    /// <summary>
    /// Gets the stroke color.
    /// </summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>
    /// Gets the fill type for this line (used in fp_line).
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets the fill color for this line.
    /// </summary>
    public EdaColor FillColor { get; set; }

    /// <summary>
    /// Gets whether the fill uses PCB format (yes/no/solid) vs schematic format (type none/outline/background).
    /// When true, the writer emits <c>(fill yes)</c> / <c>(fill no)</c> instead of <c>(fill (type ...))</c>.
    /// </summary>
    public bool UsePcbFillFormat { get; set; }

    /// <summary>
    /// Gets or sets the status flags.
    /// </summary>
    public int? Status { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new CoordRect(Start, End).Inflate(Width / 2);
}
