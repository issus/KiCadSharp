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
    /// <summary>
    /// Gets the file format version number.
    /// </summary>
    public int Version { get; internal set; }

    /// <summary>
    /// Gets the generator name.
    /// </summary>
    public string? Generator { get; internal set; }

    /// <summary>
    /// Gets the generator version.
    /// </summary>
    public string? GeneratorVersion { get; internal set; }

    /// <summary>
    /// Gets the UUID of the document.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchComponent> Components { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchPowerObject> PowerObjects { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchNoConnect> NoConnects { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchBus> Buses { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchBusEntry> BusEntries { get; internal set; } = [];

    /// <summary>
    /// Gets the sheets in this document.
    /// </summary>
    public IReadOnlyList<KiCadSchSheet> Sheets { get; internal set; } = [];

    /// <summary>
    /// Gets the symbol library instances embedded in this document.
    /// </summary>
    public IReadOnlyList<KiCadSchComponent> LibSymbols { get; internal set; } = [];

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
