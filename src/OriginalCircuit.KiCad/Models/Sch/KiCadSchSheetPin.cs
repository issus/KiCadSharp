using OriginalCircuit.Eda.Enums;
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

    /// <summary>
    /// Gets or sets the font height for the sheet pin text effects.
    /// </summary>
    public Coord FontSizeHeight { get; set; }

    /// <summary>
    /// Gets or sets the font width for the sheet pin text effects.
    /// </summary>
    public Coord FontSizeWidth { get; set; }

    /// <summary>
    /// Gets or sets the text justification.
    /// </summary>
    public TextJustification Justification { get; set; }

    /// <summary>
    /// Gets or sets whether the text is bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets or sets whether the text is italic.
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// Gets or sets the font color.
    /// </summary>
    public EdaColor FontColor { get; set; }

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
