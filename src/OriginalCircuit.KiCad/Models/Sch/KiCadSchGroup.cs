namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic group, mapped from <c>(group "NAME" (uuid UUID) (members UUID ...))</c>.
/// </summary>
public sealed class KiCadSchGroup
{
    /// <summary>Gets or sets the group name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the group UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets the member UUIDs.</summary>
    public List<string> Members { get; set; } = [];
}
