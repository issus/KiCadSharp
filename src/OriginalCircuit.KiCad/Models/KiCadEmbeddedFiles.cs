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

    /// <summary>Gets or sets the base64-encoded file data.</summary>
    public string Data { get; set; } = "";
}
