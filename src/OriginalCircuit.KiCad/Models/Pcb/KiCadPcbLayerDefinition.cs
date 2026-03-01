namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB layer definition, mapped from <c>(layers (N "name" type ["username"]) ...)</c>.
/// </summary>
public sealed class KiCadPcbLayerDefinition
{
    /// <summary>Gets or sets the layer ordinal number.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the canonical layer name (e.g., "F.Cu", "B.SilkS").</summary>
    public string CanonicalName { get; set; } = "";

    /// <summary>Gets or sets the layer type (signal, power, mixed, jumper, user).</summary>
    public string LayerType { get; set; } = "";

    /// <summary>Gets or sets the optional user-defined name for the layer.</summary>
    public string? UserName { get; set; }
}
