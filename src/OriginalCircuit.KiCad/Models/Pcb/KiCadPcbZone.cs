using OriginalCircuit.Eda.Primitives;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB zone, mapped from <c>(zone ...)</c> definitions.
/// Stores comprehensive zone properties including fill settings, keepout rules,
/// and polygon data for full round-trip fidelity.
/// </summary>
public sealed class KiCadPcbZone
{
    /// <summary>Gets or sets the net number.</summary>
    public int Net { get; set; }

    /// <summary>Gets or sets the net name.</summary>
    public string? NetName { get; set; }

    /// <summary>Gets or sets the layer name (for single-layer zones).</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the layer names (for multi-layer zones).</summary>
    public IReadOnlyList<string>? LayerNames { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets the zone name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets whether this zone is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Gets or sets the hatch style (edge, full, none).</summary>
    public string? HatchStyle { get; set; }

    /// <summary>Gets or sets the hatch pitch.</summary>
    public double HatchPitch { get; set; }

    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets the connect pads mode (thru_hole_only, yes, no).</summary>
    public string? ConnectPadsMode { get; set; }

    /// <summary>Gets or sets the connect pads clearance.</summary>
    public Coord ConnectPadsClearance { get; set; }

    /// <summary>Gets or sets the minimum thickness.</summary>
    public Coord MinThickness { get; set; }

    // -- Keepout settings --

    /// <summary>Gets or sets whether this is a keepout zone.</summary>
    public bool IsKeepout { get; set; }

    /// <summary>Gets or sets the keepout tracks allowed setting.</summary>
    public string? KeepoutTracks { get; set; }

    /// <summary>Gets or sets the keepout vias allowed setting.</summary>
    public string? KeepoutVias { get; set; }

    /// <summary>Gets or sets the keepout pads allowed setting.</summary>
    public string? KeepoutPads { get; set; }

    /// <summary>Gets or sets the keepout copperpour allowed setting.</summary>
    public string? KeepoutCopperpour { get; set; }

    /// <summary>Gets or sets the keepout footprints allowed setting.</summary>
    public string? KeepoutFootprints { get; set; }

    // -- Fill settings --

    /// <summary>Gets or sets whether the zone is filled.</summary>
    public bool IsFilled { get; set; }

    /// <summary>Gets or sets the thermal gap.</summary>
    public Coord ThermalGap { get; set; }

    /// <summary>Gets or sets the thermal bridge width.</summary>
    public Coord ThermalBridgeWidth { get; set; }

    /// <summary>Gets or sets the smoothing type.</summary>
    public string? SmoothingType { get; set; }

    /// <summary>Gets or sets the smoothing radius.</summary>
    public Coord SmoothingRadius { get; set; }

    /// <summary>Gets or sets the island removal mode.</summary>
    public int? IslandRemovalMode { get; set; }

    /// <summary>Gets or sets the minimum island area.</summary>
    public double? IslandAreaMin { get; set; }

    /// <summary>Gets or sets the hatch thickness for zone fill.</summary>
    public Coord HatchThickness { get; set; }

    /// <summary>Gets or sets the hatch gap for zone fill.</summary>
    public Coord HatchGap { get; set; }

    /// <summary>Gets or sets the hatch orientation for zone fill.</summary>
    public double HatchOrientation { get; set; }

    /// <summary>Gets or sets the hatch smoothing level.</summary>
    public int HatchSmoothingLevel { get; set; }

    /// <summary>Gets or sets the hatch smoothing value.</summary>
    public double HatchSmoothingValue { get; set; }

    /// <summary>Gets or sets the hatch border algorithm.</summary>
    public int HatchBorderAlgorithm { get; set; }

    /// <summary>Gets or sets the hatch minimum hole area.</summary>
    public double HatchMinHoleArea { get; set; }

    // -- Polygon data --

    /// <summary>Gets or sets the zone outline polygon points.</summary>
    public IReadOnlyList<CoordPoint> Outline { get; set; } = [];

    /// <summary>
    /// Gets or sets the raw <c>(polygon ...)</c> S-expression subtree for round-trip fidelity.
    /// Used when the polygon contains arcs or other non-xy elements that cannot be represented as simple CoordPoints.
    /// </summary>
    public SExpr? PolygonRaw { get; set; }

    /// <summary>
    /// Gets or sets raw filled polygon S-expression subtrees for round-trip fidelity.
    /// These are large auto-generated data from KiCad's fill algorithm.
    /// </summary>
    public List<SExpr> FilledPolygonsRaw { get; set; } = [];

    /// <summary>
    /// Gets or sets raw fill segments S-expression subtrees for round-trip fidelity.
    /// </summary>
    public List<SExpr> FillSegmentsRaw { get; set; } = [];

    /// <summary>
    /// Gets or sets the raw <c>(fill ...)</c> S-expression subtree for complex fill settings.
    /// </summary>
    public SExpr? FillRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw <c>(keepout ...)</c> S-expression subtree.
    /// </summary>
    public SExpr? KeepoutRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw <c>(connect_pads ...)</c> S-expression subtree.
    /// </summary>
    public SExpr? ConnectPadsRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw <c>(filled_areas_thickness ...)</c> S-expression value.
    /// </summary>
    public SExpr? FilledAreasThicknessRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw <c>(attr ...)</c> S-expression value for the zone.
    /// </summary>
    public SExpr? AttrRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw <c>(placement ...)</c> S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? PlacementRaw { get; set; }
}
