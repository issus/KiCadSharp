using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB via, mapped from <c>(via [TYPE] (at X Y) (size D) (drill D) (layers L1 L2) (net N) (tstamp UUID))</c>.
/// </summary>
public sealed class KiCadPcbVia : IPcbVia
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Diameter { get; set; }

    /// <inheritdoc />
    public Coord HoleSize { get; set; }

    /// <inheritdoc />
    public int StartLayer { get; internal set; }

    /// <inheritdoc />
    public int EndLayer { get; internal set; }

    /// <summary>
    /// Gets the start layer name.
    /// </summary>
    public string? StartLayerName { get; internal set; }

    /// <summary>
    /// Gets the end layer name.
    /// </summary>
    public string? EndLayerName { get; internal set; }

    /// <summary>
    /// Gets the via type (through, blind, micro).
    /// </summary>
    public ViaType ViaType { get; internal set; }

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; internal set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <summary>
    /// Gets whether this via is free (not locked to a net).
    /// </summary>
    public bool IsFree { get; internal set; }

    /// <summary>
    /// Gets whether this via is locked.
    /// </summary>
    public bool IsLocked { get; internal set; }

    /// <summary>
    /// Gets whether to remove unused layers.
    /// </summary>
    public bool RemoveUnusedLayers { get; internal set; }

    /// <summary>
    /// Gets whether to keep end layers.
    /// </summary>
    public bool KeepEndLayers { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var half = Diameter / 2;
            return new CoordRect(
                new CoordPoint(Location.X - half, Location.Y - half),
                new CoordPoint(Location.X + half, Location.Y + half));
        }
    }
}
