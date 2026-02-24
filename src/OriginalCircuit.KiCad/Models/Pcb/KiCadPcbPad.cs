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
    public double Rotation { get; set; }

    /// <inheritdoc />
    public PadShape Shape { get; set; }

    /// <inheritdoc />
    public CoordPoint Size { get; set; }

    /// <inheritdoc />
    public Coord HoleSize { get; set; }

    /// <inheritdoc />
    public PadHoleType HoleType { get; set; }

    /// <inheritdoc />
    public bool IsPlated { get; set; } = true;

    /// <inheritdoc />
    public int Layer { get; set; }

    /// <inheritdoc />
    public Coord SolderMaskExpansion { get; set; }

    /// <inheritdoc />
    public int CornerRadiusPercentage { get; set; }

    /// <summary>
    /// Gets the KiCad pad type.
    /// </summary>
    public PadType PadType { get; set; }

    /// <summary>
    /// Gets the layers this pad is on as a list of layer names.
    /// </summary>
    public IReadOnlyList<string> Layers { get; set; } = [];

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; set; }

    /// <summary>
    /// Gets the net name.
    /// </summary>
    public string? NetName { get; set; }

    /// <summary>
    /// Gets the zone connection type.
    /// </summary>
    public ZoneConnectionType ZoneConnect { get; set; }

    /// <summary>
    /// Gets the thermal relief width.
    /// </summary>
    public Coord ThermalWidth { get; set; }

    /// <summary>
    /// Gets the thermal relief gap.
    /// </summary>
    public Coord ThermalGap { get; set; }

    /// <summary>
    /// Gets the pin function name.
    /// </summary>
    public string? PinFunction { get; set; }

    /// <summary>
    /// Gets the pin type.
    /// </summary>
    public string? PinType { get; set; }

    /// <summary>
    /// Gets the die length for wire bonding.
    /// </summary>
    public Coord DieLength { get; set; }

    /// <summary>
    /// Gets whether to remove unused layers on this pad.
    /// </summary>
    public bool RemoveUnusedLayers { get; set; }

    /// <summary>
    /// Gets whether to keep end layers on this pad.
    /// </summary>
    public bool KeepEndLayers { get; set; }

    /// <summary>
    /// Gets the solder paste margin.
    /// </summary>
    public Coord SolderPasteMargin { get; set; }

    /// <summary>
    /// Gets the solder paste ratio.
    /// </summary>
    public double SolderPasteRatio { get; set; }

    /// <summary>
    /// Gets the clearance override for this pad.
    /// </summary>
    public Coord Clearance { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => CoordRect.FromCenterAndSize(Location, Size.X, Size.Y);
}
