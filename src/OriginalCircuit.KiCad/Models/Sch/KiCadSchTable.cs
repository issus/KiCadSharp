using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic table, mapped from <c>(table ...)</c>.
/// </summary>
public sealed class KiCadSchTable
{
    /// <summary>Gets or sets the column count.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Gets or sets whether external border is enabled.</summary>
    public bool BorderExternal { get; set; }

    /// <summary>Gets or sets whether header border is enabled.</summary>
    public bool BorderHeader { get; set; }

    /// <summary>Gets or sets the border stroke width.</summary>
    public Coord BorderStrokeWidth { get; set; }

    /// <summary>Gets or sets the border stroke type.</summary>
    public string? BorderStrokeType { get; set; }

    /// <summary>Gets or sets whether row separators are enabled.</summary>
    public bool SeparatorRows { get; set; }

    /// <summary>Gets or sets whether column separators are enabled.</summary>
    public bool SeparatorCols { get; set; }

    /// <summary>Gets or sets the separator stroke width.</summary>
    public Coord SeparatorStrokeWidth { get; set; }

    /// <summary>Gets or sets the separator stroke type.</summary>
    public string? SeparatorStrokeType { get; set; }

    /// <summary>Gets or sets the column widths.</summary>
    public List<double> ColumnWidths { get; set; } = [];

    /// <summary>Gets or sets the row heights.</summary>
    public List<double> RowHeights { get; set; } = [];

    /// <summary>Gets or sets the table cells.</summary>
    public List<KiCadSchTableCell> Cells { get; set; } = [];
}

/// <summary>
/// A cell in a KiCad schematic table.
/// </summary>
public sealed class KiCadSchTableCell
{
    /// <summary>Gets or sets the cell text.</summary>
    public string Text { get; set; } = "";

    /// <summary>Gets or sets whether this cell is excluded from simulation.</summary>
    public bool ExcludeFromSim { get; set; }

    /// <summary>Gets or sets whether exclude_from_sim was present.</summary>
    public bool HasExcludeFromSim { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the rotation angle.</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the cell size.</summary>
    public CoordPoint Size { get; set; }

    /// <summary>Gets or sets the margins (left, right, top, bottom).</summary>
    public double MarginLeft { get; set; }
    /// <summary>Gets or sets the right margin.</summary>
    public double MarginRight { get; set; }
    /// <summary>Gets or sets the top margin.</summary>
    public double MarginTop { get; set; }
    /// <summary>Gets or sets the bottom margin.</summary>
    public double MarginBottom { get; set; }

    /// <summary>Gets or sets the column span.</summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>Gets or sets the row span.</summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>Gets or sets the fill type.</summary>
    public string? FillType { get; set; }

    /// <summary>Gets or sets the font height.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width.</summary>
    public Coord FontWidth { get; set; }

    /// <summary>Gets or sets the text justification values.</summary>
    public List<string> Justification { get; set; } = [];

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }
}
