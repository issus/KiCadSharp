using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic parameter (property), mapped from <c>(property "KEY" "VALUE" (at X Y ANGLE) (effects ...))</c>.
/// </summary>
public sealed class KiCadSchParameter : ISchParameter
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string Value { get; set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public int Orientation { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Gets the font size width.
    /// </summary>
    public Coord FontSizeWidth { get; set; }

    /// <summary>
    /// Gets the font size height.
    /// </summary>
    public Coord FontSizeHeight { get; set; }

    /// <summary>
    /// Gets the parameter identifier from the KiCad file.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets the font face name.
    /// </summary>
    public string? FontFace { get; set; }

    /// <summary>
    /// Gets the line spacing value from text effects.
    /// </summary>
    public double? LineSpacing { get; set; }

    /// <summary>
    /// Gets the font color.
    /// </summary>
    public EdaColor FontColor { get; set; }

    /// <summary>
    /// Gets whether the text is bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets whether the text is italic.
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// Gets the font thickness from text effects.
    /// </summary>
    public Coord FontThickness { get; set; }

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
    /// Gets the layer name for this property (KiCad 8 footprint properties).
    /// </summary>
    public string? LayerName { get; set; }

    /// <summary>
    /// Gets the UUID for this property (KiCad 8 footprint properties).
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets whether this property is unlocked (KiCad 8 footprint properties).
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// Gets or sets whether the hide in effects was a symbol value (KiCad 6 format: <c>(effects ... hide)</c>)
    /// rather than a child node (KiCad 8 format: <c>(effects ... (hide yes))</c>).
    /// </summary>
    public bool HideIsSymbolValue { get; set; }

    /// <summary>
    /// Gets or sets whether the <c>(hide yes)</c> node is a direct child of the property node
    /// (KiCad 8 footprint / KiCad 9 lib symbol format) rather than inside effects.
    /// </summary>
    public bool HideIsDirectChild { get; set; }

    /// <summary>
    /// Gets whether this is an inline property without position or text effects
    /// (e.g., <c>(property ki_fp_filters "...")</c>).
    /// </summary>
    public bool IsInline { get; set; }

    /// <summary>
    /// Gets or sets whether the property has a <c>(do_not_autoplace)</c> flag (KiCad 9+).
    /// </summary>
    public bool DoNotAutoplace { get; set; }

    /// <summary>
    /// Gets or sets whether the <c>do_not_autoplace</c> flag uses the value format
    /// <c>(do_not_autoplace yes)</c> (placed symbols) versus the bare format
    /// <c>(do_not_autoplace)</c> (lib symbols).
    /// </summary>
    public bool DoNotAutoplaceHasValue { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontH = FontSizeHeight != Coord.Zero ? FontSizeHeight : Coord.FromMm(1.27);
            var fontW = FontSizeWidth != Coord.Zero ? FontSizeWidth : Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Value.Length * fontW.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontH);
        }
    }
}
