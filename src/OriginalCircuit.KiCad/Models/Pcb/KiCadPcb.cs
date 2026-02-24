using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB document (<c>.kicad_pcb</c>), implementing <see cref="IPcbDocument"/>.
/// </summary>
public sealed class KiCadPcb : IPcbDocument
{
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
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbComponent> Components { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbArc> Arcs { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbText> Texts { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IPcbRegion> Regions { get; set; } = [];

    /// <summary>
    /// Gets the net definitions as a list of (number, name) tuples.
    /// </summary>
    public IReadOnlyList<(int Number, string Name)> Nets { get; set; } = [];

    /// <summary>
    /// Gets the board thickness.
    /// </summary>
    public Coord BoardThickness { get; set; }

    /// <inheritdoc />
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
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
