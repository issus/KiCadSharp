using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB region (zone filled polygon), mapped from zone definitions containing filled polygons.
/// </summary>
public sealed class KiCadPcbRegion : IPcbRegion
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Outline { get; internal set; } = [];

    /// <inheritdoc />
    public int Layer { get; internal set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; internal set; }

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; internal set; }

    /// <summary>
    /// Gets the net name.
    /// </summary>
    public string? NetName { get; internal set; }

    /// <summary>
    /// Gets the zone priority.
    /// </summary>
    public int Priority { get; internal set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Outline);
}
