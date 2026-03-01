namespace OriginalCircuit.KiCad.Models;

/// <summary>
/// Embedded files section, shared between PCB, schematic, and symbol library documents.
/// Mapped from <c>(embedded_files ...)</c>.
/// </summary>
public sealed class KiCadEmbeddedFiles
{
    /// <summary>Gets or sets the embedded file entries.</summary>
    public List<KiCadEmbeddedFile> Files { get; set; } = [];
}

/// <summary>
/// A single embedded file entry.
/// </summary>
public sealed class KiCadEmbeddedFile
{
    /// <summary>Gets or sets the file name/path.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the file type (e.g., "font", "image").</summary>
    public string Type { get; set; } = "";

    /// <summary>Gets or sets the checksum value (e.g., SHA-256 hash).</summary>
    public string? Checksum { get; set; }

    /// <summary>Gets or sets the base64-encoded file data segments (one per line in the file).</summary>
    public List<string> DataSegments { get; set; } = [];

    /// <summary>Gets or sets whether data segments are unquoted symbols (vs quoted strings).</summary>
    public bool DataSegmentsAreSymbols { get; set; }

    /// <summary>Gets the concatenated base64-encoded file data.</summary>
    public string Data => string.Concat(DataSegments);
}
