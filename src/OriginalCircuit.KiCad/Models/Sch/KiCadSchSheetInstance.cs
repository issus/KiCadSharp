namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// Top-level sheet instance, mapped from <c>(sheet_instances (path "/" (page "1")))</c>.
/// </summary>
public sealed class KiCadSchSheetInstance
{
    /// <summary>Gets or sets the instance path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Gets or sets the page number/name.</summary>
    public string Page { get; set; } = "";
}

/// <summary>
/// Top-level symbol instance (KiCad 7/8 format), mapped from
/// <c>(symbol_instances (path "/UUID" (reference "R1") (unit 1) (value "10k") (footprint "...")))</c>.
/// </summary>
public sealed class KiCadSchSymbolInstance
{
    /// <summary>Gets or sets the instance path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Gets or sets the reference designator.</summary>
    public string Reference { get; set; } = "";

    /// <summary>Gets or sets the unit number.</summary>
    public int Unit { get; set; }

    /// <summary>Gets or sets the component value.</summary>
    public string Value { get; set; } = "";

    /// <summary>Gets or sets the footprint.</summary>
    public string Footprint { get; set; } = "";
}

/// <summary>
/// Per-symbol instance (KiCad 9+ format), mapped from
/// <c>(instances (project "NAME" (path "PATH" (reference "REF") (unit N))))</c>.
/// </summary>
public sealed class KiCadSchComponentInstance
{
    /// <summary>Gets or sets the project name.</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>Gets or sets the instance path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Gets or sets the reference designator.</summary>
    public string Reference { get; set; } = "";

    /// <summary>Gets or sets the unit number.</summary>
    public int Unit { get; set; }
}

/// <summary>
/// Per-sheet instance entry, mapped from
/// <c>(instances (project "NAME" (path "PATH" (page "PAGE"))))</c>.
/// </summary>
public sealed class KiCadSchSheetInstanceEntry
{
    /// <summary>Gets or sets the project name.</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>Gets or sets the instance path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Gets or sets the page number/name.</summary>
    public string Page { get; set; } = "";
}
