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
    /// Gets or sets the status flags.
    /// </summary>
    public int? Status { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new CoordRect(Start, End).Inflate(Width / 2);
}
