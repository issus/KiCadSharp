using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic net label, mapped from <c>(label "TEXT" (at X Y ANGLE) (effects ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchNetLabel : ISchNetLabel
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public int Orientation { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public NetLabelType LabelType { get; set; }

    /// <summary>
    /// Gets or sets the font size height.
    /// </summary>
    public Coord FontSizeHeight { get; set; }

    /// <summary>
    /// Gets or sets the font size width.
    /// </summary>
    public Coord FontSizeWidth { get; set; }

    /// <summary>
    /// Gets or sets whether the label text is bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets or sets whether the label text is italic.
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// Gets or sets whether the label text is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Gets or sets the net label shape (e.g., input, output, bidirectional).
    /// Only meaningful for global and hierarchical labels.
    /// </summary>
    public string? Shape { get; set; }

    /// <summary>
    /// Gets or sets whether the label fields are auto-placed.
    /// </summary>
    public bool FieldsAutoplaced { get; set; }

    /// <summary>
    /// Gets or sets the properties associated with this label (for global/hierarchical labels).
    /// </summary>
    public List<KiCadSchParameter> Properties { get; set; } = [];

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
    /// Gets the UUID of the net label.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontH = FontSizeHeight != Coord.Zero ? FontSizeHeight : Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Text.Length * fontH.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontH);
        }
    }
}
