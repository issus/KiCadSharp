using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB footprint component. Represents a <c>(footprint ...)</c> definition containing pads, graphical items, and 3D models.
/// </summary>
public sealed class KiCadPcbComponent : IPcbComponent
{
    private readonly List<KiCadPcbPad> _pads = [];
    private readonly List<KiCadPcbTrack> _tracks = [];
    private readonly List<KiCadPcbVia> _vias = [];
    private readonly List<KiCadPcbArc> _arcs = [];
    private readonly List<KiCadPcbText> _texts = [];
    private readonly List<KiCadPcbRegion> _regions = [];
    private readonly List<KiCadPcbRectangle> _rectangles = [];
    private readonly List<KiCadPcbCircle> _circles = [];
    private readonly List<KiCadPcbPolygon> _polygons = [];
    private readonly List<KiCadPcbCurve> _curves = [];
    private readonly List<KiCadPcb3DModel> _models3D = [];
    private readonly List<KiCadSchParameter> _properties = [];
    private readonly List<KiCadDiagnostic> _diagnostics = [];
    private readonly List<object> _graphicalItemOrder = [];

    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the root S-expression token name for round-trip fidelity.
    /// Legacy KiCad 5 files use "module"; KiCad 6+ uses "footprint".
    /// </summary>
    public string RootToken { get; set; } = "footprint";

    /// <summary>
    /// Gets or sets the footprint file version (only present in standalone .kicad_mod files).
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Gets or sets the generator name (only present in standalone .kicad_mod files).
    /// </summary>
    public string? Generator { get; set; }

    /// <summary>
    /// Gets or sets whether the generator value was a bare symbol (unquoted) in the source file.
    /// </summary>
    public bool GeneratorIsSymbol { get; set; }

    /// <summary>
    /// Gets or sets the generator version (only present in standalone .kicad_mod files).
    /// </summary>
    public string? GeneratorVersion { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public Coord Height { get; set; }

    /// <inheritdoc />
    public int Layer { get; set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads => _pads;
    internal List<KiCadPcbPad> PadList => _pads;

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks => _tracks;
    internal List<KiCadPcbTrack> TrackList => _tracks;

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias => _vias;
    internal List<KiCadPcbVia> ViaList => _vias;

    /// <inheritdoc />
    public IReadOnlyList<IPcbArc> Arcs => _arcs;
    internal List<KiCadPcbArc> ArcList => _arcs;

    /// <inheritdoc />
    public IReadOnlyList<IPcbText> Texts => _texts;
    internal List<KiCadPcbText> TextList => _texts;

    /// <inheritdoc />
    public IReadOnlyList<IPcbRegion> Regions => _regions;
    internal List<KiCadPcbRegion> RegionList => _regions;

    /// <summary>
    /// Gets the rectangles (fp_rect) in this footprint.
    /// </summary>
    public IReadOnlyList<KiCadPcbRectangle> Rectangles => _rectangles;
    internal List<KiCadPcbRectangle> RectangleList => _rectangles;

    /// <summary>
    /// Gets the circles (fp_circle) in this footprint.
    /// </summary>
    public IReadOnlyList<KiCadPcbCircle> Circles => _circles;
    internal List<KiCadPcbCircle> CircleList => _circles;

    /// <summary>
    /// Gets the polygons (fp_poly) in this footprint.
    /// </summary>
    public IReadOnlyList<KiCadPcbPolygon> Polygons => _polygons;
    internal List<KiCadPcbPolygon> PolygonList => _polygons;

    /// <summary>
    /// Gets the bezier curves (fp_curve) in this footprint.
    /// </summary>
    public IReadOnlyList<KiCadPcbCurve> Curves => _curves;
    internal List<KiCadPcbCurve> CurveList => _curves;

    /// <summary>
    /// Gets all graphical items (lines, rects, circles, arcs, polygons, curves) in their original parse order.
    /// Used by the writer for round-trip ordering fidelity.
    /// </summary>
    public IReadOnlyList<object> GraphicalItemOrder => _graphicalItemOrder;
    internal List<object> GraphicalItemOrderList => _graphicalItemOrder;

    /// <summary>
    /// Gets the footprint location on the board.
    /// </summary>
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Gets the footprint rotation on the board.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Gets whether the footprint is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether the locked/placed flags use child node format (<c>(locked yes)</c>)
    /// instead of bare symbol format (<c>locked</c>). Used for round-trip fidelity.
    /// </summary>
    public bool UsesChildNodeFlags { get; set; }

    /// <summary>
    /// Gets whether the footprint is placed.
    /// </summary>
    public bool IsPlaced { get; set; }

    /// <summary>
    /// Gets or sets whether embedded fonts are used (KiCad 8+).
    /// Null means the token was not present in the source file.
    /// </summary>
    public bool? EmbeddedFonts { get; set; }

    /// <summary>
    /// Gets or sets whether duplicate pad numbers are treated as jumpers (net-tie footprint flag).
    /// Null means the token was not present in the source file.
    /// </summary>
    public bool? DuplicatePadNumbersAreJumpers { get; set; }

    /// <summary>
    /// Gets the footprint tags.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets the path (hierarchical sheet reference).
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the sheet name (set by the schematic when placing footprints on the PCB).
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Gets or sets the sheet file name (set by the schematic when placing footprints on the PCB).
    /// </summary>
    public string? SheetFile { get; set; }

    /// <summary>
    /// Gets the footprint attributes.
    /// </summary>
    public FootprintAttribute Attributes { get; set; }

    /// <summary>
    /// Gets the autoplace cost for 90-degree rotation.
    /// </summary>
    public int AutoplaceCost90 { get; set; }

    /// <summary>
    /// Gets the autoplace cost for 180-degree rotation.
    /// </summary>
    public int AutoplaceCost180 { get; set; }

    /// <summary>
    /// Gets the solder mask margin.
    /// </summary>
    public Coord SolderMaskMargin { get; set; }

    /// <summary>
    /// Gets the solder paste margin.
    /// </summary>
    public Coord SolderPasteMargin { get; set; }

    /// <summary>
    /// Gets the solder paste ratio.
    /// </summary>
    public double SolderPasteRatio { get; set; }

    /// <summary>
    /// Gets the solder paste margin ratio (footprint-level paste ratio override).
    /// </summary>
    public double? SolderPasteMarginRatio { get; set; }

    /// <summary>
    /// Gets the clearance override.
    /// </summary>
    public Coord Clearance { get; set; }

    /// <summary>
    /// Gets whether the clearance token was explicitly present in the source file.
    /// </summary>
    public bool HasClearance { get; set; }

    /// <summary>
    /// Gets whether the solder_mask_margin token was explicitly present in the source file.
    /// </summary>
    public bool HasSolderMaskMargin { get; set; }

    /// <summary>
    /// Gets whether the solder_paste_margin token was explicitly present in the source file.
    /// </summary>
    public bool HasSolderPasteMargin { get; set; }

    /// <summary>
    /// Gets the zone connection type.
    /// </summary>
    public ZoneConnectionType ZoneConnect { get; set; }

    /// <summary>
    /// Gets the thermal relief width.
    /// </summary>
    public Coord ThermalWidth { get; set; }

    /// <summary>
    /// Gets whether the thermal_width token was explicitly present in the source file.
    /// </summary>
    public bool HasThermalWidth { get; set; }

    /// <summary>
    /// Gets the thermal relief gap.
    /// </summary>
    public Coord ThermalGap { get; set; }

    /// <summary>
    /// Gets whether the thermal_gap token was explicitly present in the source file.
    /// </summary>
    public bool HasThermalGap { get; set; }

    /// <summary>
    /// Gets the tedit timestamp (legacy).
    /// </summary>
    public string? Tedit { get; set; }

    /// <summary>
    /// Gets the list of all 3D models associated with this footprint.
    /// </summary>
    public IReadOnlyList<KiCadPcb3DModel> Models3D => _models3D;
    internal List<KiCadPcb3DModel> Model3DList => _models3D;

    /// <summary>
    /// Gets the 3D model path.
    /// </summary>
    public string? Model3D { get; set; }

    /// <summary>
    /// Gets the 3D model offset (X, Y in mm).
    /// </summary>
    public CoordPoint Model3DOffset { get; set; }

    /// <summary>
    /// Gets the 3D model Z offset in mm.
    /// </summary>
    public double Model3DOffsetZ { get; set; }

    /// <summary>
    /// Gets the 3D model scale (X, Y factors).
    /// </summary>
    public CoordPoint Model3DScale { get; set; } = new(Coord.FromMm(1), Coord.FromMm(1));

    /// <summary>
    /// Gets the 3D model Z scale factor.
    /// </summary>
    public double Model3DScaleZ { get; set; } = 1.0;

    /// <summary>
    /// Gets the 3D model rotation (X, Y angles in degrees).
    /// </summary>
    public CoordPoint Model3DRotation { get; set; }

    /// <summary>
    /// Gets the 3D model Z rotation in degrees.
    /// </summary>
    public double Model3DRotationZ { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the UUID token name ("uuid" or "tstamp") for round-trip fidelity.
    /// </summary>
    public string UuidToken { get; set; } = "uuid";

    /// <summary>
    /// Gets or sets whether UUID/tstamp values are bare symbols (unquoted) in the original file.
    /// KiCad 6 tstamp values are bare symbols; KiCad 8+ uuid values are quoted strings.
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>
    /// Gets the raw fp_text_private nodes for round-trip fidelity.
    /// </summary>
    public List<SExpr> TextPrivateRaw { get; } = [];

    /// <summary>
    /// Gets the raw teardrop node for round-trip fidelity.
    /// </summary>
    public SExpr? TeardropRaw { get; set; }

    /// <summary>
    /// Gets the raw net_tie_pad_groups node for round-trip fidelity.
    /// </summary>
    public SExpr? NetTiePadGroupsRaw { get; set; }

    /// <summary>
    /// Gets the raw fp_text_box nodes for round-trip fidelity.
    /// </summary>
    public List<SExpr> TextBoxesRaw { get; } = [];

    /// <summary>
    /// Gets the raw fp_image / image nodes within this footprint for round-trip fidelity.
    /// </summary>
    public List<SExpr> ImagesRaw { get; } = [];

    /// <summary>
    /// Gets the raw private_layers node for round-trip fidelity.
    /// </summary>
    public SExpr? PrivateLayersRaw { get; set; }

    /// <summary>
    /// Gets the raw zone nodes within this footprint for round-trip fidelity.
    /// </summary>
    public List<SExpr> ZonesRaw { get; } = [];

    /// <summary>
    /// Gets the raw group nodes within this footprint for round-trip fidelity.
    /// </summary>
    public List<SExpr> GroupsRaw { get; } = [];

    /// <summary>
    /// Gets the raw dimension nodes within this footprint for round-trip fidelity.
    /// </summary>
    public List<SExpr> DimensionsRaw { get; } = [];

    /// <summary>
    /// Gets the raw component_classes node for round-trip fidelity.
    /// </summary>
    public SExpr? ComponentClassesRaw { get; set; }

    /// <summary>
    /// Gets or sets the raw embedded_files S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? EmbeddedFilesRaw { get; set; }

    /// <summary>
    /// Gets the properties of this footprint.
    /// </summary>
    public IReadOnlyList<KiCadSchParameter> Properties => _properties;
    internal List<KiCadSchParameter> PropertyList => _properties;

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics => _diagnostics;
    internal List<KiCadDiagnostic> DiagnosticList => _diagnostics;

    /// <summary>Returns true if any diagnostic has Error severity.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    /// <remarks>This property is computed on each access. Cache the result if accessing repeatedly.</remarks>
    public CoordRect Bounds
    {
        get
        {
            var rect = CoordRect.Empty;
            foreach (var pad in Pads) rect = rect.Union(pad.Bounds);
            foreach (var track in Tracks) rect = rect.Union(track.Bounds);
            foreach (var arc in Arcs) rect = rect.Union(arc.Bounds);
            foreach (var text in Texts) rect = rect.Union(text.Bounds);
            foreach (var r in Rectangles) rect = rect.Union(r.Bounds);
            foreach (var c in Circles) rect = rect.Union(c.Bounds);
            foreach (var p in Polygons) rect = rect.Union(p.Bounds);
            foreach (var cv in Curves) rect = rect.Union(cv.Bounds);
            return rect;
        }
    }

    /// <inheritdoc />
    public void AddPad(IPcbPad pad)
    {
        ArgumentNullException.ThrowIfNull(pad);
        if (pad is not KiCadPcbPad kpad)
            throw new ArgumentException($"Expected {nameof(KiCadPcbPad)}", nameof(pad));
        _pads.Add(kpad);
    }

    /// <inheritdoc />
    public bool RemovePad(IPcbPad pad) => pad is KiCadPcbPad kpad && _pads.Remove(kpad);

    /// <inheritdoc />
    public void AddTrack(IPcbTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (track is not KiCadPcbTrack ktrack)
            throw new ArgumentException($"Expected {nameof(KiCadPcbTrack)}", nameof(track));
        _tracks.Add(ktrack);
    }

    /// <inheritdoc />
    public bool RemoveTrack(IPcbTrack track) => track is KiCadPcbTrack ktrack && _tracks.Remove(ktrack);

    /// <inheritdoc />
    public void AddVia(IPcbVia via)
    {
        ArgumentNullException.ThrowIfNull(via);
        if (via is not KiCadPcbVia kvia)
            throw new ArgumentException($"Expected {nameof(KiCadPcbVia)}", nameof(via));
        _vias.Add(kvia);
    }

    /// <inheritdoc />
    public bool RemoveVia(IPcbVia via) => via is KiCadPcbVia kvia && _vias.Remove(kvia);

    /// <inheritdoc />
    public void AddArc(IPcbArc arc)
    {
        ArgumentNullException.ThrowIfNull(arc);
        if (arc is not KiCadPcbArc karc)
            throw new ArgumentException($"Expected {nameof(KiCadPcbArc)}", nameof(arc));
        _arcs.Add(karc);
    }

    /// <inheritdoc />
    public bool RemoveArc(IPcbArc arc) => arc is KiCadPcbArc karc && _arcs.Remove(karc);

    /// <inheritdoc />
    public void AddText(IPcbText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text is not KiCadPcbText ktext)
            throw new ArgumentException($"Expected {nameof(KiCadPcbText)}", nameof(text));
        _texts.Add(ktext);
    }

    /// <inheritdoc />
    public bool RemoveText(IPcbText text) => text is KiCadPcbText ktext && _texts.Remove(ktext);

    /// <inheritdoc />
    public void AddRegion(IPcbRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);
        if (region is not KiCadPcbRegion kregion)
            throw new ArgumentException($"Expected {nameof(KiCadPcbRegion)}", nameof(region));
        _regions.Add(kregion);
    }

    /// <inheritdoc />
    public bool RemoveRegion(IPcbRegion region) => region is KiCadPcbRegion kregion && _regions.Remove(kregion);

    /// <summary>
    /// Adds a rectangle to this footprint.
    /// </summary>
    public void AddRectangle(KiCadPcbRectangle rectangle)
    {
        ArgumentNullException.ThrowIfNull(rectangle);
        _rectangles.Add(rectangle);
    }

    /// <summary>
    /// Removes a rectangle from this footprint.
    /// </summary>
    public bool RemoveRectangle(KiCadPcbRectangle rectangle) => _rectangles.Remove(rectangle);

    /// <summary>
    /// Adds a circle to this footprint.
    /// </summary>
    public void AddCircle(KiCadPcbCircle circle)
    {
        ArgumentNullException.ThrowIfNull(circle);
        _circles.Add(circle);
    }

    /// <summary>
    /// Removes a circle from this footprint.
    /// </summary>
    public bool RemoveCircle(KiCadPcbCircle circle) => _circles.Remove(circle);

    /// <summary>
    /// Adds a polygon to this footprint.
    /// </summary>
    public void AddPolygon(KiCadPcbPolygon polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        _polygons.Add(polygon);
    }

    /// <summary>
    /// Removes a polygon from this footprint.
    /// </summary>
    public bool RemovePolygon(KiCadPcbPolygon polygon) => _polygons.Remove(polygon);

    /// <summary>
    /// Adds a bezier curve to this footprint.
    /// </summary>
    public void AddCurve(KiCadPcbCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        _curves.Add(curve);
    }

    /// <summary>
    /// Removes a bezier curve from this footprint.
    /// </summary>
    public bool RemoveCurve(KiCadPcbCurve curve) => _curves.Remove(curve);

    /// <summary>
    /// Adds a property to this footprint.
    /// </summary>
    public void AddProperty(KiCadSchParameter property)
    {
        ArgumentNullException.ThrowIfNull(property);
        _properties.Add(property);
    }

    /// <summary>
    /// Removes a property from this footprint.
    /// </summary>
    public bool RemoveProperty(KiCadSchParameter property) => _properties.Remove(property);
}
