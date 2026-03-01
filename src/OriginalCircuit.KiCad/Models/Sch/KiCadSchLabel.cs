using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic text label, mapped from <c>(text "TEXT" (at X Y ANGLE) (effects ...))</c>.
/// </summary>
public sealed class KiCadSchLabel : ISchLabel
{
    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public double Rotation { get; set; }

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <inheritdoc />
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets the font size height.
    /// </summary>
    public Coord FontSizeHeight { get; set; }

    /// <summary>
    /// Gets or sets the font size width.
    /// </summary>
    public Coord FontSizeWidth { get; set; }

    /// <summary>
    /// Gets or sets whether the text is bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets or sets whether the text is italic.
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// Gets or sets whether bold was a bare symbol (KiCad 6 format: <c>bold</c>)
    /// rather than a child node (KiCad 8 format: <c>(bold yes)</c>).
    /// </summary>
    public bool BoldIsSymbol { get; set; }

    /// <summary>
    /// Gets or sets whether italic was a bare symbol (KiCad 6 format: <c>italic</c>)
    /// rather than a child node (KiCad 8 format: <c>(italic yes)</c>).
    /// </summary>
    public bool ItalicIsSymbol { get; set; }

    /// <summary>
    /// Gets or sets whether the stroke node was present in the source file.
    /// </summary>
    public bool HasStroke { get; set; }

    /// <summary>
    /// Gets the stroke width for the text label.
    /// </summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>
    /// Gets the stroke line style for the text label.
    /// </summary>
    public LineStyle StrokeLineStyle { get; set; }

    /// <summary>
    /// Gets the stroke color for the text label.
    /// </summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>
    /// Gets or sets the font face name.
    /// </summary>
    public string? FontFace { get; set; }

    /// <summary>
    /// Gets or sets the font thickness (stroke width of text glyphs).
    /// </summary>
    public Coord FontThickness { get; set; }

    /// <summary>
    /// Gets or sets the font color.
    /// </summary>
    public EdaColor FontColor { get; set; }

    /// <summary>
    /// Gets or sets whether the position node included an explicit angle value in the source file.
    /// </summary>
    public bool PositionIncludesAngle { get; set; } = true;

    /// <summary>
    /// Gets or sets the hyperlink URL from the text effects (KiCad 9+).
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets whether the text has an <c>(exclude_from_sim)</c> flag (KiCad 9+).
    /// </summary>
    public bool ExcludeFromSimPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>(exclude_from_sim)</c> flag.
    /// </summary>
    public bool ExcludeFromSim { get; set; }

    /// <summary>
    /// Gets or sets the line spacing multiplier.
    /// </summary>
    public double? LineSpacing { get; set; }

    /// <summary>
    /// Gets the UUID of the text label.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether the UUID was stored as a bare symbol (unquoted).
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>
    /// Gets or sets whether this item is marked as private (KiCad 9+).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontH = FontSizeHeight != Coord.Zero ? FontSizeHeight : Coord.FromMm(1.27);
            var fontW = FontSizeWidth != Coord.Zero ? FontSizeWidth : Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Text.Length * fontW.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontH);
        }
    }
}
