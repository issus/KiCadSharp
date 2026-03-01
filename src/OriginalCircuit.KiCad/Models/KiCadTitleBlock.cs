namespace OriginalCircuit.KiCad.Models;

/// <summary>
/// KiCad title block, shared between PCB and schematic documents.
/// Mapped from <c>(title_block ...)</c>.
/// </summary>
public sealed class KiCadTitleBlock
{
    /// <summary>Gets or sets the title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the date.</summary>
    public string? Date { get; set; }

    /// <summary>Gets or sets the revision.</summary>
    public string? Revision { get; set; }

    /// <summary>Gets or sets the company.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the comments, keyed by comment number (1-9).</summary>
    public Dictionary<int, string> Comments { get; set; } = new();
}
