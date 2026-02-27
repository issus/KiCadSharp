using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic sheet, mapped from <c>(sheet (at X Y) (size W H) (stroke ...) (fill ...) (uuid ...) (property ...) (pin ...))</c>.
/// </summary>
public sealed class KiCadSchSheet : ISchSheet
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public CoordPoint Size { get; set; }

    /// <inheritdoc />
    public string SheetName { get; set; } = "";

    /// <inheritdoc />
    public string FileName { get; set; } = "";

    /// <inheritdoc />
    public IReadOnlyList<ISchSheetPin> Pins { get; set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public EdaColor FillColor { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Gets or sets the stroke line style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets or sets the fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets or sets whether the fill uses color-only format (KiCad 9+: <c>(fill (color ...))</c>)
    /// without a <c>(type ...)</c> child.
    /// </summary>
    public bool FillColorOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the sheet fields are auto-placed.
    /// </summary>
    public bool FieldsAutoplaced { get; set; }

    /// <summary>
    /// Gets or sets the sheet properties (Sheetname, Sheetfile, etc.) with their per-property
    /// positions and font sizes.
    /// </summary>
    public List<KiCadSchParameter> SheetProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the sheet has an <c>(exclude_from_sim)</c> flag (KiCad 9+).
    /// </summary>
    public bool ExcludeFromSimPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>(exclude_from_sim)</c> flag.
    /// </summary>
    public bool ExcludeFromSim { get; set; }

    /// <summary>
    /// Gets or sets whether the sheet is included in the BOM (KiCad 9+).
    /// </summary>
    public bool InBomPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>(in_bom)</c> flag.
    /// </summary>
    public bool InBom { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the sheet is included on the board (KiCad 9+).
    /// </summary>
    public bool OnBoardPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>(on_board)</c> flag.
    /// </summary>
    public bool OnBoard { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the sheet has a <c>(dnp)</c> flag (KiCad 9+).
    /// </summary>
    public bool DnpPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>(dnp)</c> flag.
    /// </summary>
    public bool Dnp { get; set; }

    /// <summary>
    /// Gets or sets the raw instances S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpr? Instances { get; set; }

    /// <summary>
    /// Gets the UUID of the sheet.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, new CoordPoint(Location.X + Size.X, Location.Y + Size.Y));
}
