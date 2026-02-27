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
    /// Gets the UUID of the text label.
    /// </summary>
    public string? Uuid { get; set; }

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
