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
    public int StartLayer { get; set; }

    /// <inheritdoc />
    public int EndLayer { get; set; }

    /// <summary>
    /// Gets the start layer name.
    /// </summary>
    public string? StartLayerName { get; set; }

    /// <summary>
    /// Gets the end layer name.
    /// </summary>
    public string? EndLayerName { get; set; }

    /// <summary>
    /// Gets the via type (through, blind, micro).
    /// </summary>
    public ViaType ViaType { get; set; }

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether this via is free (not locked to a net).
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// Gets whether this via is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether to remove unused layers.
    /// </summary>
    public bool RemoveUnusedLayers { get; set; }

    /// <summary>
    /// Gets whether to keep end layers.
    /// </summary>
    public bool KeepEndLayers { get; set; }

    /// <summary>
    /// Gets or sets the status flags.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Gets or sets whether teardrops are enabled on this via.
    /// </summary>
    public bool TeardropEnabled { get; set; }

    /// <summary>
    /// Gets or sets the raw teardrop S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpression.SExpression? TeardropRaw { get; set; }

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
