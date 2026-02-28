using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB text, mapped from <c>(gr_text "TEXT" (at X Y [ANGLE]) (layer L) (effects ...) (uuid UUID))</c>
/// and footprint-level <c>(fp_text TYPE "TEXT" ...)</c>.
/// </summary>
public sealed class KiCadPcbText : IPcbText
{
    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Height { get; set; }

    /// <summary>
    /// Gets or sets the font width (may differ from height).
    /// </summary>
    public Coord FontWidth { get; set; }

    /// <inheritdoc />
    public Coord StrokeWidth { get; set; }

    /// <summary>
    /// Gets or sets the font stroke thickness.
    /// </summary>
    public Coord FontThickness { get; set; }

    /// <summary>
    /// Gets or sets the text justification.
    /// </summary>
    public TextJustification Justification { get; set; } = TextJustification.MiddleCenter;

    /// <summary>
    /// Gets or sets the font color (if specified).
    /// </summary>
    public EdaColor FontColor { get; set; }

    /// <inheritdoc />
    public double Rotation { get; set; }

    /// <inheritdoc />
    public int Layer { get; set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; set; }

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <inheritdoc />
    public string? FontName { get; set; }

    /// <inheritdoc />
    public bool FontBold { get; set; }

    /// <inheritdoc />
    public bool FontItalic { get; set; }

    /// <summary>
    /// Gets the text type (for fp_text: reference, value, user).
    /// </summary>
    public string? TextType { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the token name used for the UUID node (<c>uuid</c> or <c>tstamp</c>).
    /// </summary>
    public string UuidToken { get; set; } = "uuid";

    /// <summary>
    /// Gets or sets whether the UUID value is a bare symbol (unquoted) vs a quoted string.
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>
    /// Gets whether this text is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets whether the hide flag uses child node format <c>(hide yes)</c> instead of bare symbol.
    /// </summary>
    public bool HideIsChildNode { get; set; }

    /// <summary>
    /// Gets the raw render_cache node for round-trip fidelity.
    /// </summary>
    public SExpr? RenderCache { get; set; }

    /// <summary>
    /// Gets whether this fp_text is unlocked (placement can differ from footprint).
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// Gets whether the unlocked flag uses child node format <c>(unlocked yes)</c> instead of bare symbol.
    /// </summary>
    public bool UnlockedIsChildNode { get; set; }

    /// <summary>
    /// Gets whether this text uses knockout rendering.
    /// </summary>
    public bool IsKnockout { get; set; }

    /// <summary>
    /// Gets whether the position node included the angle value in the original file.
    /// When false, <c>(at X Y)</c> is emitted; when true, <c>(at X Y ANGLE)</c> is emitted.
    /// </summary>
    public bool PositionIncludesAngle { get; set; } = true;

    /// <summary>
    /// Gets whether the <c>unlocked</c> keyword appears inside the at node (KiCad 7+ format).
    /// When true, the position is emitted as <c>(at X Y unlocked)</c> or <c>(at X Y ANGLE unlocked)</c>.
    /// </summary>
    public bool UnlockedInAtNode { get; set; }

    /// <summary>
    /// Gets whether the UUID/tstamp comes after effects in the original file ordering.
    /// KiCad 6 files put tstamp after effects; KiCad 8+ puts uuid before effects.
    /// </summary>
    public bool UuidAfterEffects { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontSize = Height != Coord.Zero ? Height : Coord.FromMm(1.27);
            var textWidth = Coord.FromMm((Text?.Length ?? 0) * fontSize.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontSize);
        }
    }
}
