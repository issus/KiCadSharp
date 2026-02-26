using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic document (<c>.kicad_sch</c>), implementing <see cref="ISchDocument"/>.
/// </summary>
public sealed class KiCadSch : ISchDocument
{
    private readonly List<KiCadSchComponent> _components = [];
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
    private readonly List<KiCadDiagnostic> _diagnostics = [];

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
    /// Gets the UUID of the document.
    /// </summary>
    public string? Uuid { get; set; }

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

    /// <inheritdoc />
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
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
