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
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public CoordPoint Size { get; internal set; }

    /// <inheritdoc />
    public string SheetName { get; internal set; } = "";

    /// <inheritdoc />
    public string FileName { get; internal set; } = "";

    /// <inheritdoc />
    public IReadOnlyList<ISchSheetPin> Pins { get; internal set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public EdaColor FillColor { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <summary>
    /// Gets the UUID of the sheet.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, new CoordPoint(Location.X + Size.X, Location.Y + Size.Y));
}
