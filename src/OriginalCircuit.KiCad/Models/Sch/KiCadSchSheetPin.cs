using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic sheet pin, mapped from <c>(pin "NAME" input|output|... (at X Y ANGLE) (effects ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchSheetPin : ISchSheetPin
{
    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public int IoType { get; set; }

    /// <inheritdoc />
    public int Side { get; set; }

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Gets the UUID of the sheet pin.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontSize = Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Name.Length * fontSize.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontSize);
        }
    }
}
