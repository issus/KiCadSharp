namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB group, mapped from <c>(group "name" (id "uuid") (members "uuid" ...))</c>.
/// </summary>
public sealed class KiCadPcbGroup
{
    /// <summary>Gets or sets the group name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the group ID (UUID).</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the member UUIDs.</summary>
    public List<string> Members { get; set; } = [];

    /// <summary>Gets or sets whether this group is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Gets or sets whether the locked flag uses child node format.</summary>
    public bool LockedIsChildNode { get; set; }
}
