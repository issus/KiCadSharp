using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad symbol library file (<c>.kicad_sym</c>), implementing <see cref="ISchLibrary"/>.
/// </summary>
public sealed class KiCadSymLib : ISchLibrary
{
    private readonly List<KiCadSchComponent> _components = [];

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

    private readonly List<KiCadDiagnostic> _diagnostics = [];

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics => _diagnostics;
    internal List<KiCadDiagnostic> DiagnosticList => _diagnostics;

    /// <summary>Returns true if any diagnostic has Error severity.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    public IReadOnlyList<KiCadSchComponent> Components => _components;

    /// <inheritdoc />
    IReadOnlyList<ISchComponent> ILibrary<ISchComponent>.Components => _components;

    /// <inheritdoc />
    public IReadOnlyList<IComponent> AllComponents => _components;

    /// <inheritdoc />
    public int Count => _components.Count;

    /// <inheritdoc />
    public KiCadSchComponent? this[string name] => _components.Find(c => c.Name == name);

    /// <inheritdoc />
    ISchComponent? ILibrary<ISchComponent>.this[string name] => this[name];

    /// <inheritdoc />
    public bool Contains(string name) => _components.Exists(c => c.Name == name);

    /// <inheritdoc />
    public bool Remove(string name) => _components.RemoveAll(c => c.Name == name) > 0;

    /// <inheritdoc />
    public void Add(ISchComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component is KiCadSchComponent kc)
            _components.Add(kc);
        else
            throw new ArgumentException("Component must be a KiCadSchComponent.", nameof(component));
    }

    /// <summary>
    /// Adds a KiCad schematic component to the library.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void Add(KiCadSchComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        _components.Add(component);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken ct = default)
    {
        await SymLibWriter.WriteAsync(this, path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken ct = default)
    {
        await SymLibWriter.WriteAsync(this, stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
