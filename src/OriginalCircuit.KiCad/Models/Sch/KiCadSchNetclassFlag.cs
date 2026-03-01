using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic netclass flag, mapped from <c>(netclass_flag "name" (length N) (shape S) (at X Y A) ...)</c>.
/// </summary>
public sealed class KiCadSchNetclassFlag
{
    /// <summary>Gets or sets the netclass name (may be empty).</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the length.</summary>
    public double Length { get; set; }

    /// <summary>Gets or sets the shape (round, diamond).</summary>
    public string? Shape { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the rotation angle.</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the font height.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width.</summary>
    public Coord FontWidth { get; set; }

    /// <summary>Gets or sets the text justification values.</summary>
    public List<string> Justification { get; set; } = [];

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets the properties.</summary>
    public List<KiCadSchNetclassFlagProperty> Properties { get; set; } = [];
}

/// <summary>
/// A property on a netclass flag.
/// </summary>
public sealed class KiCadSchNetclassFlagProperty
{
    /// <summary>Gets or sets the property key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Gets or sets the property value.</summary>
    public string Value { get; set; } = "";

    /// <summary>Gets or sets the location.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the rotation.</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the font height.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width.</summary>
    public Coord FontWidth { get; set; }

    /// <summary>Gets or sets whether the text is hidden.</summary>
    public bool IsHidden { get; set; }

    /// <summary>Gets or sets the text justification values.</summary>
    public List<string> Justification { get; set; } = [];

    /// <summary>Gets or sets whether font italic.</summary>
    public bool FontItalic { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }
}
