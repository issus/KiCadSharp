namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic bus alias, mapped from <c>(bus_alias "name" (members "sig1" "sig2" ...))</c>.
/// </summary>
public sealed class KiCadSchBusAlias
{
    /// <summary>Gets or sets the bus alias name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the member signal names.</summary>
    public List<string> Members { get; set; } = [];
}
