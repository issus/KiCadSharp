using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic text box, mapped from <c>(text_box "TEXT" (at X Y ANGLE) (size W H) ...)</c>.
/// </summary>
public sealed class KiCadSchTextBox
{
    /// <summary>Gets or sets the text content.</summary>
    public string Text { get; set; } = "";

    /// <summary>Gets or sets the position.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the rotation angle.</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the size (width, height).</summary>
    public CoordPoint Size { get; set; }

    /// <summary>Gets or sets the stroke width.</summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>Gets or sets the stroke type.</summary>
    public string? StrokeType { get; set; }

    /// <summary>Gets or sets the stroke color.</summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>Gets or sets whether stroke was present in source.</summary>
    public bool HasStroke { get; set; }

    /// <summary>Gets or sets the fill type.</summary>
    public string? FillType { get; set; }

    /// <summary>Gets or sets the fill color.</summary>
    public EdaColor FillColor { get; set; }

    /// <summary>Gets or sets whether exclude_from_sim was present.</summary>
    public bool ExcludeFromSimPresent { get; set; }

    /// <summary>Gets or sets the exclude_from_sim value.</summary>
    public bool ExcludeFromSim { get; set; }

    /// <summary>Gets or sets the font height.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width.</summary>
    public Coord FontWidth { get; set; }

    /// <summary>Gets or sets the font name.</summary>
    public string? FontName { get; set; }

    /// <summary>Gets or sets the font color.</summary>
    public EdaColor FontColor { get; set; }

    /// <summary>Gets or sets whether font is bold.</summary>
    public bool FontBold { get; set; }

    /// <summary>Gets or sets whether font is italic.</summary>
    public bool FontItalic { get; set; }

    /// <summary>Gets or sets the text justification.</summary>
    public TextJustification Justification { get; set; } = TextJustification.MiddleCenter;

    /// <summary>Gets or sets whether text is mirrored.</summary>
    public bool IsMirrored { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets whether the position includes angle.</summary>
    public bool PositionIncludesAngle { get; set; } = true;

    /// <summary>Gets or sets the margins.</summary>
    public (Coord Left, Coord Top, Coord Right, Coord Bottom)? Margins { get; set; }
}
