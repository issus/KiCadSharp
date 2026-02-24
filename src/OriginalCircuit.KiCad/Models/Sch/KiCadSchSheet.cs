using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

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
    /// Gets the UUID of the sheet.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, new CoordPoint(Location.X + Size.X, Location.Y + Size.Y));
}
