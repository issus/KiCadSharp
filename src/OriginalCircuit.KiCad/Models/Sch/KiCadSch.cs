using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Serialization;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic document (<c>.kicad_sch</c>), implementing <see cref="ISchDocument"/>.
/// </summary>
public sealed class KiCadSch : ISchDocument
{
    private readonly List<KiCadSchComponent> _components = [];

    /// <summary>
    /// The original parsed S-expression tree, if this model was loaded from a file.
    /// When set and the model has not been modified, the writer will re-emit this tree
    /// directly for byte-perfect round-trip fidelity.
    /// Set to <c>null</c> to force the writer to rebuild the tree from the model.
    /// </summary>
    public SExpr? SourceTree { get; set; }
    private readonly List<KiCadSchWire> _wires = [];
    private readonly List<KiCadSchNetLabel> _netLabels = [];
    private readonly List<KiCadSchJunction> _junctions = [];
    private readonly List<KiCadSchPowerObject> _powerObjects = [];
    private readonly List<KiCadSchLabel> _labels = [];
    private readonly List<KiCadSchNoConnect> _noConnects = [];
    private readonly List<KiCadSchBus> _buses = [];
    private readonly List<KiCadSchBusEntry> _busEntries = [];
    private readonly List<KiCadSchSheet> _sheets = [];
    private readonly List<KiCadSchComponent> _libSymbols = [];
    private readonly List<KiCadSchPolyline> _polylines = [];
    private readonly List<KiCadSchCircle> _circles = [];
    private readonly List<KiCadSchRectangle> _rectangles = [];
    private readonly List<KiCadSchArc> _arcs = [];
    private readonly List<KiCadSchBezier> _beziers = [];
    private readonly List<KiCadSchLine> _lines = [];
    private readonly List<KiCadDiagnostic> _diagnostics = [];
    private readonly List<SExpr> _tablesRaw = [];
    private readonly List<SExpr> _ruleAreasRaw = [];
    private readonly List<SExpr> _netclassFlagsRaw = [];
    private readonly List<SExpr> _busAliasesRaw = [];

    /// <summary>
    /// Gets the file format version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets the generator name.
    /// </summary>
    public string? Generator { get; set; }

    /// <summary>
    /// Gets the generator version.
    /// </summary>
    public string? GeneratorVersion { get; set; }

    /// <summary>
    /// Gets or sets whether embedded fonts are used in this schematic (KiCad 8+).
    /// Null means the token was not present in the source file.
    /// </summary>
    public bool? EmbeddedFonts { get; set; }

    /// <summary>
    /// Gets the UUID of the document.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the paper size (e.g., "A4", "A3", "USLetter").
    /// </summary>
    public string? Paper { get; set; }

    /// <summary>
    /// Gets or sets the raw title_block S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? TitleBlock { get; set; }

    /// <summary>
    /// Gets or sets the raw sheet_instances S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? SheetInstances { get; set; }

    /// <summary>
    /// Gets or sets the raw symbol_instances S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? SymbolInstances { get; set; }

    /// <summary>
    /// Gets the raw S-expression storage for table elements (KiCad 8+).
    /// </summary>
    public IReadOnlyList<SExpr> TablesRaw => _tablesRaw;
    internal List<SExpr> TablesRawList => _tablesRaw;

    /// <summary>
    /// Gets the raw S-expression storage for rule area definitions (KiCad 8+).
    /// </summary>
    public IReadOnlyList<SExpr> RuleAreasRaw => _ruleAreasRaw;
    internal List<SExpr> RuleAreasRawList => _ruleAreasRaw;

    /// <summary>
    /// Gets the raw S-expression storage for net class flag elements (KiCad 8+).
    /// </summary>
    public IReadOnlyList<SExpr> NetclassFlagsRaw => _netclassFlagsRaw;
    internal List<SExpr> NetclassFlagsRawList => _netclassFlagsRaw;

    /// <summary>
    /// Gets the raw S-expression storage for bus alias definitions.
    /// </summary>
    public IReadOnlyList<SExpr> BusAliasesRaw => _busAliasesRaw;
    internal List<SExpr> BusAliasesRawList => _busAliasesRaw;

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics => _diagnostics;
    internal List<KiCadDiagnostic> DiagnosticList => _diagnostics;

    /// <summary>Returns true if any diagnostic has Error severity.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    public IReadOnlyList<ISchComponent> Components => _components;
    internal List<KiCadSchComponent> ComponentList => _components;

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires => _wires;
    internal List<KiCadSchWire> WireList => _wires;

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels => _netLabels;
    internal List<KiCadSchNetLabel> NetLabelList => _netLabels;

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions => _junctions;
    internal List<KiCadSchJunction> JunctionList => _junctions;

    /// <inheritdoc />
    public IReadOnlyList<ISchPowerObject> PowerObjects => _powerObjects;
    internal List<KiCadSchPowerObject> PowerObjectList => _powerObjects;

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels => _labels;
    internal List<KiCadSchLabel> LabelList => _labels;

    /// <inheritdoc />
    public IReadOnlyList<ISchNoConnect> NoConnects => _noConnects;
    internal List<KiCadSchNoConnect> NoConnectList => _noConnects;

    /// <inheritdoc />
    public IReadOnlyList<ISchBus> Buses => _buses;
    internal List<KiCadSchBus> BusList => _buses;

    /// <inheritdoc />
    public IReadOnlyList<ISchBusEntry> BusEntries => _busEntries;
    internal List<KiCadSchBusEntry> BusEntryList => _busEntries;

    /// <summary>
    /// Gets the sheets in this document.
    /// </summary>
    public IReadOnlyList<KiCadSchSheet> Sheets => _sheets;
    internal List<KiCadSchSheet> SheetList => _sheets;

    /// <summary>
    /// Gets the symbol library instances embedded in this document.
    /// </summary>
    public IReadOnlyList<KiCadSchComponent> LibSymbols => _libSymbols;
    internal List<KiCadSchComponent> LibSymbolList => _libSymbols;

    /// <summary>
    /// Gets the schematic-level polylines.
    /// </summary>
    public IReadOnlyList<KiCadSchPolyline> Polylines => _polylines;
    internal List<KiCadSchPolyline> PolylineList => _polylines;

    /// <summary>
    /// Gets the schematic-level circles.
    /// </summary>
    public IReadOnlyList<KiCadSchCircle> Circles => _circles;
    internal List<KiCadSchCircle> CircleList => _circles;

    /// <summary>
    /// Gets the schematic-level rectangles.
    /// </summary>
    public IReadOnlyList<KiCadSchRectangle> Rectangles => _rectangles;
    internal List<KiCadSchRectangle> RectangleList => _rectangles;

    /// <summary>
    /// Gets the schematic-level arcs.
    /// </summary>
    public IReadOnlyList<KiCadSchArc> Arcs => _arcs;
    internal List<KiCadSchArc> ArcList => _arcs;

    /// <summary>
    /// Gets the schematic-level bezier curves.
    /// </summary>
    public IReadOnlyList<KiCadSchBezier> Beziers => _beziers;
    internal List<KiCadSchBezier> BezierList => _beziers;

    /// <summary>
    /// Gets the schematic-level lines (2-point polylines).
    /// </summary>
    public IReadOnlyList<KiCadSchLine> Lines => _lines;
    internal List<KiCadSchLine> LineList => _lines;

    /// <summary>
    /// Gets the raw image nodes at schematic level for round-trip fidelity.
    /// </summary>
    public List<SExpr> ImagesRaw { get; } = [];

    /// <inheritdoc />
    /// <remarks>This property is computed on each access. Cache the result if accessing repeatedly.</remarks>
    public CoordRect Bounds
    {
        get
        {
            var rect = CoordRect.Empty;
            foreach (var w in Wires) rect = rect.Union(w.Bounds);
            foreach (var c in Components) rect = rect.Union(c.Bounds);
            return rect;
        }
    }

    /// <inheritdoc />
    public void AddComponent(ISchComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component is not KiCadSchComponent kcomp)
            throw new ArgumentException($"Expected {nameof(KiCadSchComponent)}", nameof(component));
        _components.Add(kcomp);
    }

    /// <inheritdoc />
    public bool RemoveComponent(ISchComponent component) => component is KiCadSchComponent kcomp && _components.Remove(kcomp);

    /// <inheritdoc />
    public void AddWire(ISchWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        if (wire is not KiCadSchWire kwire)
            throw new ArgumentException($"Expected {nameof(KiCadSchWire)}", nameof(wire));
        _wires.Add(kwire);
    }

    /// <inheritdoc />
    public bool RemoveWire(ISchWire wire) => wire is KiCadSchWire kwire && _wires.Remove(kwire);

    /// <inheritdoc />
    public void AddNetLabel(ISchNetLabel netLabel)
    {
        ArgumentNullException.ThrowIfNull(netLabel);
        if (netLabel is not KiCadSchNetLabel knl)
            throw new ArgumentException($"Expected {nameof(KiCadSchNetLabel)}", nameof(netLabel));
        _netLabels.Add(knl);
    }

    /// <inheritdoc />
    public bool RemoveNetLabel(ISchNetLabel netLabel) => netLabel is KiCadSchNetLabel knl && _netLabels.Remove(knl);

    /// <inheritdoc />
    public void AddJunction(ISchJunction junction)
    {
        ArgumentNullException.ThrowIfNull(junction);
        if (junction is not KiCadSchJunction kj)
            throw new ArgumentException($"Expected {nameof(KiCadSchJunction)}", nameof(junction));
        _junctions.Add(kj);
    }

    /// <inheritdoc />
    public bool RemoveJunction(ISchJunction junction) => junction is KiCadSchJunction kj && _junctions.Remove(kj);

    /// <inheritdoc />
    public void AddPowerObject(ISchPowerObject powerObject)
    {
        ArgumentNullException.ThrowIfNull(powerObject);
        if (powerObject is not KiCadSchPowerObject kpo)
            throw new ArgumentException($"Expected {nameof(KiCadSchPowerObject)}", nameof(powerObject));
        _powerObjects.Add(kpo);
    }

    /// <inheritdoc />
    public bool RemovePowerObject(ISchPowerObject powerObject) => powerObject is KiCadSchPowerObject kpo && _powerObjects.Remove(kpo);

    /// <inheritdoc />
    public void AddLabel(ISchLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        if (label is not KiCadSchLabel klabel)
            throw new ArgumentException($"Expected {nameof(KiCadSchLabel)}", nameof(label));
        _labels.Add(klabel);
    }

    /// <inheritdoc />
    public bool RemoveLabel(ISchLabel label) => label is KiCadSchLabel klabel && _labels.Remove(klabel);

    /// <inheritdoc />
    public void AddNoConnect(ISchNoConnect noConnect)
    {
        ArgumentNullException.ThrowIfNull(noConnect);
        if (noConnect is not KiCadSchNoConnect knc)
            throw new ArgumentException($"Expected {nameof(KiCadSchNoConnect)}", nameof(noConnect));
        _noConnects.Add(knc);
    }

    /// <inheritdoc />
    public bool RemoveNoConnect(ISchNoConnect noConnect) => noConnect is KiCadSchNoConnect knc && _noConnects.Remove(knc);

    /// <inheritdoc />
    public void AddBus(ISchBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (bus is not KiCadSchBus kbus)
            throw new ArgumentException($"Expected {nameof(KiCadSchBus)}", nameof(bus));
        _buses.Add(kbus);
    }

    /// <inheritdoc />
    public bool RemoveBus(ISchBus bus) => bus is KiCadSchBus kbus && _buses.Remove(kbus);

    /// <inheritdoc />
    public void AddBusEntry(ISchBusEntry busEntry)
    {
        ArgumentNullException.ThrowIfNull(busEntry);
        if (busEntry is not KiCadSchBusEntry kbe)
            throw new ArgumentException($"Expected {nameof(KiCadSchBusEntry)}", nameof(busEntry));
        _busEntries.Add(kbe);
    }

    /// <inheritdoc />
    public bool RemoveBusEntry(ISchBusEntry busEntry) => busEntry is KiCadSchBusEntry kbe && _busEntries.Remove(kbe);

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken ct = default)
    {
        await SchWriter.WriteAsync(this, path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken ct = default)
    {
        await SchWriter.WriteAsync(this, stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>No resources to dispose in the current implementation. Included for API consistency.</remarks>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
