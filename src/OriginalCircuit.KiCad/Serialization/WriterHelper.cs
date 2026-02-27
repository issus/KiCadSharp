using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
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

    /// <summary>
    /// Builds an <c>(at X Y [ANGLE])</c> node.
    /// </summary>
    public static SExpr BuildPosition(CoordPoint location, double angle = 0)
    {
        var b = new SExpressionBuilder("at")
            .AddValue(location.X.ToMm())
            .AddValue(location.Y.ToMm());
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
            .AddValue(point.X.ToMm())
            .AddValue(point.Y.ToMm())
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
    /// Builds a <c>(stroke (width W) (type T))</c> node.
    /// </summary>
    public static SExpr BuildStroke(Coord width, LineStyle style = LineStyle.Solid, EdaColor color = default)
    {
        var b = new SExpressionBuilder("stroke")
            .AddChild("width", w => w.AddValue(width.ToMm()))
            .AddChild("type", t => t.AddSymbol(SExpressionHelper.LineStyleToString(style)));
        if (color != default)
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
    /// Builds an <c>(effects (font (size H W)))</c> node.
    /// </summary>
    public static SExpr BuildTextEffects(Coord fontH, Coord fontW, TextJustification justification = TextJustification.MiddleCenter, bool hide = false, bool isMirrored = false, bool isBold = false, bool isItalic = false, string? fontFace = null, Coord fontThickness = default, EdaColor fontColor = default)
    {
        var b = new SExpressionBuilder("effects")
            .AddChild("font", f =>
            {
                if (fontFace is not null)
                    f.AddChild("face", fc => fc.AddValue(fontFace));
                f.AddChild("size", s =>
                {
                    s.AddValue(fontH.ToMm());
                    s.AddValue(fontW.ToMm());
                });
                if (fontThickness != Coord.Zero)
                    f.AddChild("thickness", t => t.AddValue(fontThickness.ToMm()));
                if (isBold) f.AddChild("bold", _ => { });
                if (isItalic) f.AddChild("italic", _ => { });
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

        if (hide)
            b.AddChild("hide", _ => { });

        return b.Build();
    }

    /// <summary>
    /// Builds a <c>(uuid ...)</c> node.
    /// </summary>
    public static SExpr BuildUuid(string uuid)
    {
        return new SExpressionBuilder("uuid").AddValue(uuid).Build();
    }

    /// <summary>
    /// Builds a <c>(color R G B A)</c> node.
    /// </summary>
    public static SExpr BuildColor(EdaColor color)
    {
        return new SExpressionBuilder("color")
            .AddValue(color.R)
            .AddValue(color.G)
            .AddValue(color.B)
            .AddValue(color.A / 255.0)
            .Build();
    }
}
