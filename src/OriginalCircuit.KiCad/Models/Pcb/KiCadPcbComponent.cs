using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB footprint component. Represents a <c>(footprint ...)</c> definition containing pads, graphical items, and 3D models.
/// </summary>
public sealed class KiCadPcbComponent : IPcbComponent
{
    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public Coord Height { get; set; }

    /// <inheritdoc />
    public int Layer { get; internal set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; internal set; }

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbArc> Arcs { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbText> Texts { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbRegion> Regions { get; internal set; } = [];

    /// <summary>
    /// Gets the footprint location on the board.
    /// </summary>
    public CoordPoint Location { get; internal set; }

    /// <summary>
    /// Gets the footprint rotation on the board.
    /// </summary>
    public double Rotation { get; internal set; }

    /// <summary>
    /// Gets whether the footprint is locked.
    /// </summary>
    public bool IsLocked { get; internal set; }

    /// <summary>
    /// Gets whether the footprint is placed.
    /// </summary>
    public bool IsPlaced { get; internal set; }

    /// <summary>
    /// Gets the footprint tags.
    /// </summary>
    public string? Tags { get; internal set; }

    /// <summary>
    /// Gets the path (hierarchical sheet reference).
    /// </summary>
    public string? Path { get; internal set; }

    /// <summary>
    /// Gets the footprint attributes.
    /// </summary>
    public FootprintAttribute Attributes { get; internal set; }

    /// <summary>
    /// Gets the autoplace cost for 90-degree rotation.
    /// </summary>
    public int AutoplaceCost90 { get; internal set; }

    /// <summary>
    /// Gets the autoplace cost for 180-degree rotation.
    /// </summary>
    public int AutoplaceCost180 { get; internal set; }

    /// <summary>
    /// Gets the solder mask margin.
    /// </summary>
    public Coord SolderMaskMargin { get; internal set; }

    /// <summary>
    /// Gets the solder paste margin.
    /// </summary>
    public Coord SolderPasteMargin { get; internal set; }

    /// <summary>
    /// Gets the solder paste ratio.
    /// </summary>
    public double SolderPasteRatio { get; internal set; }

    /// <summary>
    /// Gets the clearance override.
    /// </summary>
    public Coord Clearance { get; internal set; }

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
    /// Gets the 3D model path.
    /// </summary>
    public string? Model3D { get; internal set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <summary>
    /// Gets the properties of this footprint.
    /// </summary>
    public IReadOnlyList<KiCadSchParameter> Properties { get; internal set; } = [];

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics { get; internal set; } = [];

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var rect = CoordRect.Empty;
            foreach (var pad in Pads) rect = rect.Union(pad.Bounds);
            foreach (var track in Tracks) rect = rect.Union(track.Bounds);
            foreach (var arc in Arcs) rect = rect.Union(arc.Bounds);
            foreach (var text in Texts) rect = rect.Union(text.Bounds);
            return rect;
        }
    }
}
