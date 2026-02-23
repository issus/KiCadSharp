using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB pad, mapped from <c>(pad "NUMBER" TYPE SHAPE (at X Y [ANGLE]) (size W H) [(drill ...)] (layers ...) ...)</c>.
/// </summary>
public sealed class KiCadPcbPad : IPcbPad
{
    /// <inheritdoc />
    public string? Designator { get; set; }

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public double Rotation { get; internal set; }

    /// <inheritdoc />
    public PadShape Shape { get; internal set; }

    /// <inheritdoc />
    public CoordPoint Size { get; internal set; }

    /// <inheritdoc />
    public Coord HoleSize { get; internal set; }

    /// <inheritdoc />
    public PadHoleType HoleType { get; internal set; }

    /// <inheritdoc />
    public bool IsPlated { get; internal set; } = true;

    /// <inheritdoc />
    public int Layer { get; internal set; }

    /// <inheritdoc />
    public Coord SolderMaskExpansion { get; internal set; }

    /// <inheritdoc />
    public int CornerRadiusPercentage { get; internal set; }

    /// <summary>
    /// Gets the KiCad pad type.
    /// </summary>
    public PadType PadType { get; internal set; }

    /// <summary>
    /// Gets the layers this pad is on as a list of layer names.
    /// </summary>
    public IReadOnlyList<string> Layers { get; internal set; } = [];

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; internal set; }

    /// <summary>
    /// Gets the net name.
    /// </summary>
    public string? NetName { get; internal set; }

    /// <summary>
    /// Gets the zone connection type.
    /// </summary>
    public ZoneConnectionType ZoneConnect { get; internal set; }

    /// <summary>
    /// Gets the thermal relief width.
    /// </summary>
    public Coord ThermalWidth { get; internal set; }

    /// <summary>
    /// Gets the thermal relief gap.
    /// </summary>
    public Coord ThermalGap { get; internal set; }

    /// <summary>
    /// Gets the pin function name.
    /// </summary>
    public string? PinFunction { get; internal set; }

    /// <summary>
    /// Gets the pin type.
    /// </summary>
    public string? PinType { get; internal set; }

    /// <summary>
    /// Gets the die length for wire bonding.
    /// </summary>
    public Coord DieLength { get; internal set; }

    /// <summary>
    /// Gets whether to remove unused layers on this pad.
    /// </summary>
    public bool RemoveUnusedLayers { get; internal set; }

    /// <summary>
    /// Gets whether to keep end layers on this pad.
    /// </summary>
    public bool KeepEndLayers { get; internal set; }

    /// <summary>
    /// Gets the solder paste margin.
    /// </summary>
    public Coord SolderPasteMargin { get; internal set; }

    /// <summary>
    /// Gets the solder paste ratio.
    /// </summary>
    public double SolderPasteRatio { get; internal set; }

    /// <summary>
    /// Gets the clearance override for this pad.
    /// </summary>
    public Coord Clearance { get; internal set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => CoordRect.FromCenterAndSize(Location, Size.X, Size.Y);
}
