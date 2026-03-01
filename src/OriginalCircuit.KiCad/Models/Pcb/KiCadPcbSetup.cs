using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB setup section, mapped from <c>(setup ...)</c>.
/// </summary>
public sealed class KiCadPcbSetup
{
    /// <summary>Gets or sets the pad to solder mask clearance.</summary>
    public Coord PadToMaskClearance { get; set; }
    /// <summary>Gets whether the pad_to_mask_clearance token was explicitly present.</summary>
    public bool HasPadToMaskClearance { get; set; }

    /// <summary>Gets or sets the minimum solder mask width.</summary>
    public Coord SolderMaskMinWidth { get; set; }
    /// <summary>Gets whether the solder_mask_min_width token was explicitly present.</summary>
    public bool HasSolderMaskMinWidth { get; set; }

    /// <summary>Gets or sets the pad to solder paste clearance.</summary>
    public Coord PadToPasteClearance { get; set; }
    /// <summary>Gets whether the pad_to_paste_clearance token was explicitly present.</summary>
    public bool HasPadToPasteClearance { get; set; }

    /// <summary>Gets or sets the pad to paste clearance ratio.</summary>
    public double? PadToPasteClearanceRatio { get; set; }

    /// <summary>Gets or sets whether solder mask bridges in footprints are allowed.</summary>
    public bool? AllowSolderMaskBridgesInFootprints { get; set; }
    /// <summary>Gets whether the allow_soldermask_bridges_in_footprints token was explicitly present.</summary>
    public bool HasAllowSolderMaskBridges { get; set; }

    /// <summary>Gets or sets the pad to paste clearance ratio presence flag.</summary>
    public bool HasPadToPasteClearanceRatio { get; set; }

    /// <summary>Gets or sets whether tenting settings are present in setup.</summary>
    public bool HasTenting { get; set; }
    /// <summary>Gets or sets whether front tenting is enabled.</summary>
    public bool TentingFront { get; set; }
    /// <summary>Gets or sets whether back tenting is enabled.</summary>
    public bool TentingBack { get; set; }
    /// <summary>Gets or sets whether tenting uses child node format <c>(front yes)</c> vs bare symbol <c>front</c>.</summary>
    public bool TentingIsChildNode { get; set; }

    /// <summary>Gets or sets the auxiliary axis origin.</summary>
    public CoordPoint? AuxAxisOrigin { get; set; }

    /// <summary>Gets or sets the grid origin.</summary>
    public CoordPoint? GridOrigin { get; set; }

    /// <summary>Gets or sets the board thickness.</summary>
    public Coord BoardThickness { get; set; }
    /// <summary>Gets whether the board_thickness token was explicitly present.</summary>
    public bool HasBoardThickness { get; set; }

    /// <summary>Gets or sets the stackup configuration.</summary>
    public KiCadPcbStackup? Stackup { get; set; }

    /// <summary>Gets or sets the covering front/back settings.</summary>
    public bool HasCovering { get; set; }
    /// <summary>Gets or sets the covering front value.</summary>
    public string? CoveringFront { get; set; }
    /// <summary>Gets or sets the covering back value.</summary>
    public string? CoveringBack { get; set; }

    /// <summary>Gets or sets the plugging front/back settings.</summary>
    public bool HasPlugging { get; set; }
    /// <summary>Gets or sets the plugging front value.</summary>
    public string? PluggingFront { get; set; }
    /// <summary>Gets or sets the plugging back value.</summary>
    public string? PluggingBack { get; set; }

    /// <summary>Gets or sets the capping value (yes/no/none).</summary>
    public string? Capping { get; set; }

    /// <summary>Gets or sets the filling value (yes/no/none).</summary>
    public string? Filling { get; set; }

    /// <summary>Gets or sets the plot parameters.</summary>
    public KiCadPcbPlotParams? PlotParams { get; set; }
}

/// <summary>
/// KiCad PCB stackup configuration, mapped from <c>(stackup ...)</c> inside setup.
/// </summary>
public sealed class KiCadPcbStackup
{
    /// <summary>Gets or sets the stackup layers.</summary>
    public List<KiCadPcbStackupLayer> Layers { get; set; } = [];

    /// <summary>Gets or sets the copper finish type.</summary>
    public string? CopperFinish { get; set; }

    /// <summary>Gets or sets whether dielectric constraints are enabled.</summary>
    public bool? DielectricConstraints { get; set; }

    /// <summary>Gets or sets the edge connector type.</summary>
    public string? EdgeConnector { get; set; }

    /// <summary>Gets or sets whether castellated pads are used.</summary>
    public bool? CastellatedPads { get; set; }

    /// <summary>Gets or sets whether edge plating is used.</summary>
    public bool? EdgePlating { get; set; }
}

/// <summary>
/// KiCad PCB stackup layer, mapped from <c>(layer ...)</c> inside stackup.
/// </summary>
public sealed class KiCadPcbStackupLayer
{
    /// <summary>Gets or sets the layer name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets whether this is a dielectric layer.</summary>
    public bool IsDielectric { get; set; }

    /// <summary>Gets or sets the dielectric number (for dielectric layers).</summary>
    public int? DielectricNumber { get; set; }

    /// <summary>Gets or sets the layer type.</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the layer color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the layer thickness in mm.</summary>
    public double? Thickness { get; set; }

    /// <summary>Gets or sets the material name.</summary>
    public string? Material { get; set; }

    /// <summary>Gets or sets the dielectric constant (epsilon_r).</summary>
    public double? EpsilonR { get; set; }

    /// <summary>Gets or sets the loss tangent.</summary>
    public double? LossTangent { get; set; }

    /// <summary>
    /// Gets or sets the additional sublayers.
    /// When present, the layer node contains <c>addsublayer</c> markers separating groups of sublayer properties.
    /// </summary>
    public List<KiCadPcbStackupSublayer> Sublayers { get; set; } = [];
}

/// <summary>
/// Represents an additional sublayer within a stackup layer, separated by the <c>addsublayer</c> keyword.
/// </summary>
public sealed class KiCadPcbStackupSublayer
{
    /// <summary>Gets or sets the sublayer color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the sublayer thickness in mm.</summary>
    public double? Thickness { get; set; }

    /// <summary>Gets or sets the sublayer material name.</summary>
    public string? Material { get; set; }

    /// <summary>Gets or sets the sublayer dielectric constant (epsilon_r).</summary>
    public double? EpsilonR { get; set; }

    /// <summary>Gets or sets the sublayer loss tangent.</summary>
    public double? LossTangent { get; set; }
}
