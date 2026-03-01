using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB text box, mapped from <c>(gr_text_box "TEXT" ...)</c> and <c>(fp_text_box "TEXT" ...)</c>.
/// </summary>
public sealed class KiCadPcbTextBox
{
    /// <summary>Gets or sets the text content.</summary>
    public string Text { get; set; } = "";

    /// <summary>Gets or sets whether the text box is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Gets or sets whether locked uses child node format.</summary>
    public bool LockedIsChildNode { get; set; }

    /// <summary>Gets or sets the start corner (for rectangular text boxes).</summary>
    public CoordPoint? Start { get; set; }

    /// <summary>Gets or sets the end corner (for rectangular text boxes).</summary>
    public CoordPoint? End { get; set; }

    /// <summary>Gets or sets the polygon points (for non-rectangular text boxes).</summary>
    public List<CoordPoint>? Points { get; set; }

    /// <summary>Gets or sets the rotation angle.</summary>
    public double Angle { get; set; }

    /// <summary>Gets or sets the layer name.</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets the UUID token name.</summary>
    public string UuidToken { get; set; } = "uuid";

    /// <summary>Gets or sets whether the UUID is a bare symbol.</summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>Gets or sets the font height.</summary>
    public Coord FontHeight { get; set; }

    /// <summary>Gets or sets the font width.</summary>
    public Coord FontWidth { get; set; }

    /// <summary>Gets or sets the font stroke thickness.</summary>
    public Coord FontThickness { get; set; }

    /// <summary>Gets or sets the font name.</summary>
    public string? FontName { get; set; }

    /// <summary>Gets or sets whether font is bold.</summary>
    public bool FontBold { get; set; }

    /// <summary>Gets or sets whether font is italic.</summary>
    public bool FontItalic { get; set; }

    /// <summary>Gets or sets whether bold was a bare symbol.</summary>
    public bool BoldIsSymbol { get; set; }

    /// <summary>Gets or sets whether italic was a bare symbol.</summary>
    public bool ItalicIsSymbol { get; set; }

    /// <summary>Gets or sets the font color.</summary>
    public EdaColor FontColor { get; set; }

    /// <summary>Gets or sets the text justification.</summary>
    public TextJustification Justification { get; set; } = TextJustification.MiddleCenter;

    /// <summary>Gets or sets whether the text is mirrored.</summary>
    public bool IsMirrored { get; set; }

    /// <summary>Gets or sets the stroke width.</summary>
    public Coord StrokeWidth { get; set; }

    /// <summary>Gets or sets the stroke style.</summary>
    public LineStyle StrokeStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the stroke color.</summary>
    public EdaColor StrokeColor { get; set; }

    /// <summary>Gets or sets whether stroke was explicitly present.</summary>
    public bool HasStroke { get; set; }

    /// <summary>Gets or sets whether stroke color was explicitly present.</summary>
    public bool HasStrokeColor { get; set; }

    /// <summary>Gets or sets the render cache.</summary>
    public KiCadTextRenderCache? RenderCache { get; set; }

    /// <summary>Gets or sets whether the position angle was present in source.</summary>
    public bool PositionIncludesAngle { get; set; }

    /// <summary>Gets or sets the margin values (left, top, right, bottom).</summary>
    public (Coord Left, Coord Top, Coord Right, Coord Bottom)? Margins { get; set; }
}
