using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB arc, mapped from <c>(arc (start X Y) (mid X Y) (end X Y) (width W) (layer L) (net N) (tstamp UUID))</c>.
/// Center, radius, and angles are computed from the start/mid/end representation.
/// </summary>
public sealed class KiCadPcbArc : IPcbArc
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
    public Coord Width { get; set; }

    /// <inheritdoc />
    public int Layer { get; set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; set; }

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

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether this arc is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets the status flags.
    /// </summary>
    public int? Status { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute([ArcStart, ArcMid, ArcEnd]);
}
