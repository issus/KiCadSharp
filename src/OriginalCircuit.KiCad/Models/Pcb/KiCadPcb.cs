using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Serialization;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB document (<c>.kicad_pcb</c>), implementing <see cref="IPcbDocument"/>.
/// </summary>
public sealed class KiCadPcb : IPcbDocument
{
    private readonly List<KiCadPcbComponent> _components = [];
    private readonly List<KiCadPcbPad> _pads = [];
    private readonly List<KiCadPcbVia> _vias = [];
    private readonly List<KiCadPcbTrack> _tracks = [];
    private readonly List<KiCadPcbArc> _arcs = [];
    private readonly List<KiCadPcbText> _texts = [];
    private readonly List<KiCadPcbRegion> _regions = [];
    private readonly List<(int Number, string Name)> _nets = [];
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
    /// Gets or sets whether embedded fonts are enabled (KiCad 8+).
    /// </summary>
    public bool EmbeddedFonts { get; set; }

    /// <summary>
    /// Gets the list of raw S-expression nodes for <c>(generated ...)</c> elements.
    /// These are auto-generated graphical primitives preserved verbatim during round-trip.
    /// </summary>
    public List<SExpr> GeneratedElements { get; set; } = [];

    /// <summary>
    /// Gets or sets the raw S-expression for <c>(embedded_files ...)</c> data,
    /// preserved verbatim during round-trip.
    /// </summary>
    public SExpr? EmbeddedFilesRaw { get; set; }

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics => _diagnostics;
    internal List<KiCadDiagnostic> DiagnosticList => _diagnostics;

    /// <summary>Returns true if any diagnostic has Error severity.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    public IReadOnlyList<IPcbComponent> Components => _components;
    internal List<KiCadPcbComponent> ComponentList => _components;

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads => _pads;
    internal List<KiCadPcbPad> PadList => _pads;

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias => _vias;
    internal List<KiCadPcbVia> ViaList => _vias;

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks => _tracks;
    internal List<KiCadPcbTrack> TrackList => _tracks;

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
    /// Gets the net definitions as a list of (number, name) tuples.
    /// </summary>
    public IReadOnlyList<(int Number, string Name)> Nets => _nets;
    internal List<(int Number, string Name)> NetList => _nets;

    /// <summary>
    /// Gets the board thickness.
    /// </summary>
    public Coord BoardThickness { get; set; }

    /// <inheritdoc />
    /// <remarks>This property is computed on each access. Cache the result if accessing repeatedly.</remarks>
    public CoordRect Bounds
    {
        get
        {
            var rect = CoordRect.Empty;
            foreach (var c in Components) rect = rect.Union(c.Bounds);
            foreach (var t in Tracks) rect = rect.Union(t.Bounds);
            foreach (var v in Vias) rect = rect.Union(v.Bounds);
            foreach (var a in Arcs) rect = rect.Union(a.Bounds);
            return rect;
        }
    }

    /// <inheritdoc />
    public void AddComponent(IPcbComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component is not KiCadPcbComponent kcomp)
            throw new ArgumentException($"Expected {nameof(KiCadPcbComponent)}", nameof(component));
        _components.Add(kcomp);
    }

    /// <inheritdoc />
    public bool RemoveComponent(IPcbComponent component) => component is KiCadPcbComponent kcomp && _components.Remove(kcomp);

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
    /// Adds a net definition.
    /// </summary>
    public void AddNet(int number, string name) => _nets.Add((number, name));

    /// <summary>
    /// Removes a net definition by number.
    /// </summary>
    public bool RemoveNet(int number) => _nets.RemoveAll(n => n.Number == number) > 0;

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken ct = default)
    {
        await PcbWriter.WriteAsync(this, path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken ct = default)
    {
        await PcbWriter.WriteAsync(this, stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>No resources to dispose in the current implementation. Included for API consistency.</remarks>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
