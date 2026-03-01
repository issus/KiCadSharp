using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Shared helper methods for building S-expression trees for KiCad file writing.
/// </summary>
internal static class WriterHelper
{
    /// <summary>KiCad's default text size in mm.</summary>
    internal static readonly Coord DefaultTextSize = Coord.FromMm(1.27);

    /// <summary>Number of decimal places used when rounding mm values for output.</summary>
    private const int MmDecimalPlaces = 6;

    /// <summary>
    /// Maximum quantization error from the Coord fixed-point representation.
    /// The Coord system uses ~393,701 units/mm (irrational factor 10,000,000/25.4).
    /// Pure metric values (e.g. 1.0, 0.5, 0.8) can have up to ~2.5e-6 mm error.
    /// </summary>
    private const double CoordQuantizationTolerance = 3e-6;

    /// <summary>Format string for mm values.</summary>
    private static readonly string MmFormat = $"0.######";

    /// <summary>
    /// Rounds a millimeter value to compensate for floating-point precision loss
    /// in the Coord int-to-mm conversion. KiCad files use up to 6 decimal places.
    /// </summary>
    internal static double RoundMm(double mm) => Math.Round(mm, MmDecimalPlaces);

    /// <summary>
    /// Converts a Coord to mm, snapping to the simplest representation within
    /// the Coord quantization tolerance. This finds the number with the fewest
    /// decimal places that is within ~3e-6 mm of the true value, preventing
    /// artifacts like 0.999998 instead of 1.0 or 1.249997 instead of 1.25.
    /// </summary>
    internal static double ToRoundedMm(this Coord c)
    {
        var mm = c.ToMm();
        // Try progressively more decimal places; pick the simplest within tolerance
        for (int decimals = 0; decimals <= MmDecimalPlaces; decimals++)
        {
            var rounded = Math.Round(mm, decimals);
            if (Math.Abs(rounded - mm) <= CoordQuantizationTolerance)
                return rounded;
        }
        return Math.Round(mm, MmDecimalPlaces);
    }

    /// <summary>
    /// Formats a rounded mm value as a string with controlled decimal places.
    /// This avoids IEEE 754 representation artifacts like 0.000998 becoming 0.00099799999999999997.
    /// </summary>
    internal static string FormatMm(double roundedMm)
    {
        return roundedMm.ToString(MmFormat, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Adds a Coord value as a properly formatted mm number to the builder.
    /// </summary>
    internal static SExpressionBuilder AddMm(this SExpressionBuilder builder, Coord c)
    {
        var rounded = RoundMm(c.ToMm());
        return builder.AddFormattedValue(rounded, FormatMm(rounded));
    }

    /// <summary>
    /// Adds a pre-rounded mm value as a properly formatted number to the builder.
    /// </summary>
    internal static SExpressionBuilder AddMm(this SExpressionBuilder builder, double roundedMm)
    {
        return builder.AddFormattedValue(roundedMm, FormatMm(roundedMm));
    }

    /// <summary>
    /// Builds an <c>(at X Y ANGLE)</c> node, always including the angle value.
    /// Used for property and text positions where KiCad always emits the angle.
    /// </summary>
    public static SExpr BuildPosition(CoordPoint location, double angle = 0)
    {
        return new SExpressionBuilder("at")
            .AddMm(location.X)
            .AddMm(location.Y)
            .AddValue(angle)
            .Build();
    }

    /// <summary>
    /// Builds an <c>(at X Y [ANGLE])</c> node, omitting angle when zero.
    /// Used for footprint and pad positions where KiCad omits angle=0.
    /// </summary>
    public static SExpr BuildPositionCompact(CoordPoint location, double angle = 0)
    {
        var b = new SExpressionBuilder("at")
            .AddMm(location.X)
            .AddMm(location.Y);
        if (angle != 0)
            b.AddValue(angle);
        return b.Build();
    }

    /// <summary>
    /// Builds an <c>(xy X Y)</c> node.
    /// </summary>
    public static SExpr BuildXY(CoordPoint point)
    {
        return new SExpressionBuilder("xy")
            .AddMm(point.X)
            .AddMm(point.Y)
            .Build();
    }

    /// <summary>
    /// Builds a <c>(pts (xy ...) (xy ...) ...)</c> node.
    /// </summary>
    public static SExpr BuildPoints(IReadOnlyList<CoordPoint> points)
    {
        var b = new SExpressionBuilder("pts");
        foreach (var pt in points)
            b.AddChild(BuildXY(pt));
        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(pts ...)</c> node from polygon vertices that may include arc segments.
    /// </summary>
    public static SExpr BuildPolygonVertices(IReadOnlyList<PolygonVertex> vertices)
    {
        var b = new SExpressionBuilder("pts");
        foreach (var v in vertices)
        {
            if (v.IsArc)
            {
                b.AddChild("arc", a =>
                {
                    a.AddChild("start", s => s.AddMm(v.ArcStart.X).AddMm(v.ArcStart.Y));
                    a.AddChild("mid", m => m.AddMm(v.ArcMid.X).AddMm(v.ArcMid.Y));
                    a.AddChild("end", e => e.AddMm(v.ArcEnd.X).AddMm(v.ArcEnd.Y));
                });
            }
            else
            {
                b.AddChild(BuildXY(v.Point));
            }
        }
        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(stroke (width W) (type T))</c> node.
    /// </summary>
    public static SExpr BuildStroke(Coord width, LineStyle style = LineStyle.Solid, EdaColor color = default, bool emitColor = false)
    {
        var b = new SExpressionBuilder("stroke")
            .AddChild("width", w => w.AddMm(width))
            .AddChild("type", t => t.AddSymbol(SExpressionHelper.LineStyleToString(style)));
        if (color != default || emitColor)
            b.AddChild(BuildColor(color));
        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(fill (type T))</c> node.
    /// </summary>
    public static SExpr BuildFill(SchFillType fillType, EdaColor color = default)
    {
        var b = new SExpressionBuilder("fill")
            .AddChild("type", t => t.AddSymbol(SExpressionHelper.SchFillTypeToString(fillType)));
        if (color != default)
            b.AddChild(BuildColor(color));
        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(fill (color R G B A))</c> node with color only, no type child.
    /// Used for KiCad 9+ sheet fill format.
    /// </summary>
    public static SExpr BuildFillColorOnly(EdaColor color)
    {
        return new SExpressionBuilder("fill")
            .AddChild(BuildColor(color))
            .Build();
    }

    /// <summary>
    /// Builds a <c>(fill yes)</c> or <c>(fill no)</c> node using PCB fill format.
    /// </summary>
    public static SExpr BuildPcbFill(SchFillType fillType)
    {
        return new SExpressionBuilder("fill")
            .AddBool(fillType != SchFillType.None)
            .Build();
    }

    /// <summary>
    /// Builds an <c>(effects (font (size H W)))</c> node.
    /// </summary>
    public static SExpr BuildTextEffects(Coord fontH, Coord fontW, TextJustification justification = TextJustification.MiddleCenter, bool hide = false, bool isMirrored = false, bool isBold = false, bool isItalic = false, string? fontFace = null, Coord fontThickness = default, EdaColor fontColor = default, string? href = null, bool boldIsSymbol = false, bool italicIsSymbol = false, double? lineSpacing = null)
    {
        var b = new SExpressionBuilder("effects")
            .AddChild("font", f =>
            {
                if (fontFace is not null)
                    f.AddChild("face", fc => fc.AddValue(fontFace));
                f.AddChild("size", s =>
                {
                    s.AddMm(fontH);
                    s.AddMm(fontW);
                });
                if (fontThickness != Coord.Zero)
                    f.AddChild("thickness", t => t.AddMm(fontThickness));
                if (isBold)
                {
                    if (boldIsSymbol)
                        f.AddSymbol("bold");
                    else
                        f.AddChild("bold", bl => bl.AddBool(true));
                }
                if (isItalic)
                {
                    if (italicIsSymbol)
                        f.AddSymbol("italic");
                    else
                        f.AddChild("italic", it => it.AddBool(true));
                }
                if (fontColor != default)
                    f.AddChild(BuildColor(fontColor));
            });

        if (justification != TextJustification.MiddleCenter || isMirrored)
        {
            b.AddChild("justify", j =>
            {
                switch (justification)
                {
                    case TextJustification.MiddleLeft:
                    case TextJustification.TopLeft:
                    case TextJustification.BottomLeft:
                        j.AddSymbol("left");
                        break;
                    case TextJustification.MiddleRight:
                    case TextJustification.TopRight:
                    case TextJustification.BottomRight:
                        j.AddSymbol("right");
                        break;
                }
                switch (justification)
                {
                    case TextJustification.TopLeft:
                    case TextJustification.TopCenter:
                    case TextJustification.TopRight:
                        j.AddSymbol("top");
                        break;
                    case TextJustification.BottomLeft:
                    case TextJustification.BottomCenter:
                    case TextJustification.BottomRight:
                        j.AddSymbol("bottom");
                        break;
                }
                if (isMirrored)
                    j.AddSymbol("mirror");
            });
        }

        if (href is not null)
            b.AddChild("href", h => h.AddValue(href));

        if (lineSpacing.HasValue)
            b.AddChild("line_spacing", ls => ls.AddValue(lineSpacing.Value));

        if (hide)
            b.AddChild("hide", h => h.AddBool(true));

        return b.Build();
    }

    /// <summary>
    /// Builds a text effects node for a <see cref="KiCadSchParameter"/>, including
    /// font face, font color, line spacing, bold, and italic properties.
    /// </summary>
    public static SExpr BuildPropertyTextEffects(KiCadSchParameter param)
    {
        var b = new SExpressionBuilder("effects")
            .AddChild("font", f =>
            {
                if (param.FontFace is not null)
                    f.AddChild("face", face => face.AddValue(param.FontFace));
                f.AddChild("size", s =>
                {
                    s.AddMm(param.FontSizeHeight);
                    s.AddMm(param.FontSizeWidth);
                });
                if (param.FontThickness != Coord.Zero)
                    f.AddChild("thickness", t => t.AddMm(param.FontThickness));
                if (param.IsBold)
                {
                    if (param.BoldIsSymbol)
                        f.AddSymbol("bold");
                    else
                        f.AddChild("bold", bl => bl.AddBool(true));
                }
                if (param.IsItalic)
                {
                    if (param.ItalicIsSymbol)
                        f.AddSymbol("italic");
                    else
                        f.AddChild("italic", it => it.AddBool(true));
                }
                if (param.FontColor != default)
                    f.AddChild(BuildColor(param.FontColor));
            });

        if (param.LineSpacing.HasValue)
            b.AddChild("line_spacing", ls => ls.AddValue(param.LineSpacing.Value));

        if (param.Justification != TextJustification.MiddleCenter || param.IsMirrored)
        {
            b.AddChild("justify", j =>
            {
                switch (param.Justification)
                {
                    case TextJustification.MiddleLeft:
                    case TextJustification.TopLeft:
                    case TextJustification.BottomLeft:
                        j.AddSymbol("left");
                        break;
                    case TextJustification.MiddleRight:
                    case TextJustification.TopRight:
                    case TextJustification.BottomRight:
                        j.AddSymbol("right");
                        break;
                }
                switch (param.Justification)
                {
                    case TextJustification.TopLeft:
                    case TextJustification.TopCenter:
                    case TextJustification.TopRight:
                        j.AddSymbol("top");
                        break;
                    case TextJustification.BottomLeft:
                    case TextJustification.BottomCenter:
                    case TextJustification.BottomRight:
                        j.AddSymbol("bottom");
                        break;
                }
                if (param.IsMirrored)
                    j.AddSymbol("mirror");
            });
        }

        // For KiCad 8 footprint properties and KiCad 9 lib symbol properties,
        // hide is emitted as a direct child of the property node (HideIsDirectChild).
        // For older symbol library properties, hide goes inside effects.
        if (!param.IsVisible && !param.HideIsDirectChild)
        {
            if (param.HideIsSymbolValue)
                b.AddSymbol("hide"); // KiCad 6 format: (effects ... hide)
            else
                b.AddChild("hide", h => h.AddBool(true)); // KiCad 8 format: (effects ... (hide yes))
        }

        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(effects ...)</c> node with full PCB text details. Delegates to <see cref="BuildTextEffects"/>.
    /// </summary>
    public static SExpr BuildPcbTextEffects(
        Coord fontH, Coord fontW,
        TextJustification justification = TextJustification.MiddleCenter,
        bool hide = false,
        bool isMirrored = false,
        bool isBold = false,
        bool isItalic = false,
        Coord thickness = default,
        string? fontFace = null,
        EdaColor fontColor = default,
        bool boldIsSymbol = false,
        bool italicIsSymbol = false)
    {
        return BuildTextEffects(fontH, fontW, justification, hide, isMirrored, isBold, isItalic, fontFace, thickness, fontColor, boldIsSymbol: boldIsSymbol, italicIsSymbol: italicIsSymbol);
    }

    /// <summary>
    /// Builds a <c>(uuid ...)</c> node.
    /// </summary>
    public static SExpr BuildUuid(string uuid)
    {
        return new SExpressionBuilder("uuid").AddValue(uuid).Build();
    }

    /// <summary>
    /// Builds a <c>(uuid ...)</c> node, optionally using symbol format (unquoted).
    /// </summary>
    public static SExpr BuildUuid(string uuid, bool asSymbol)
    {
        var b = new SExpressionBuilder("uuid");
        if (asSymbol)
            b.AddSymbol(uuid);
        else
            b.AddValue(uuid);
        return b.Build();
    }

    /// <summary>
    /// Builds a UUID node with a configurable token name (e.g. "uuid" or "tstamp") for round-trip fidelity.
    /// When <paramref name="asSymbol"/> is true, the value is emitted as a bare symbol (unquoted).
    /// </summary>
    public static SExpr BuildUuidToken(string uuid, string tokenName, bool asSymbol = false)
    {
        var b = new SExpressionBuilder(tokenName);
        if (asSymbol)
            b.AddSymbol(uuid);
        else
            b.AddValue(uuid);
        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(color R G B A)</c> node.
    /// Alpha is snapped to the simplest clean fraction that maps back to the same
    /// byte value, preventing byte-quantization drift during round-trip
    /// (e.g. 0.5 -> byte 128 -> 0.501960... is snapped back to 0.5).
    /// </summary>
    public static SExpr BuildColor(EdaColor color)
    {
        return new SExpressionBuilder("color")
            .AddValue(color.R)
            .AddValue(color.G)
            .AddValue(color.B)
            .AddValue(SnapAlphaToClean(color.A / 255.0))
            .Build();
    }

    /// <summary>
    /// Snaps a [0,1] alpha value to the simplest clean fraction whose byte
    /// representation matches the original byte.  Tries common denominators
    /// (1, 2, 4, 5, 10, 20, 50, 100) so values like 0.5, 0.25, 0.1, etc.
    /// survive a byte round-trip unchanged.
    /// </summary>
    private static double SnapAlphaToClean(double alpha)
    {
        int[] denominators = [1, 2, 4, 5, 10, 20, 50, 100];
        var originalByte = (byte)Math.Clamp(Math.Round(alpha * 255), 0, 255);
        foreach (var d in denominators)
        {
            var candidate = Math.Round(alpha * d) / d;
            var candidateByte = (byte)Math.Clamp(Math.Round(candidate * 255), 0, 255);
            if (candidateByte == originalByte)
                return candidate;
        }
        // Fall back to 6 decimal places
        return Math.Round(alpha, 6);
    }
}
