using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Shared helper methods for parsing common S-expression patterns from KiCad files.
/// </summary>
internal static class SExpressionHelper
{
    /// <summary>
    /// Parses a position from an <c>(at X Y [ANGLE])</c> child node.
    /// </summary>
    public static (CoordPoint Location, double Angle) ParsePosition(SExpr parent)
    {
        var at = parent.GetChild("at");
        if (at is null)
            return (CoordPoint.Zero, 0);

        var x = at.GetDouble(0) ?? 0;
        var y = at.GetDouble(1) ?? 0;
        var angle = at.GetDouble(2) ?? 0;
        return (new CoordPoint(Coord.FromMm(x), Coord.FromMm(y)), angle);
    }

    /// <summary>
    /// Parses a coordinate point from an <c>(xy X Y)</c> node.
    /// </summary>
    public static CoordPoint ParseXY(SExpr node)
    {
        var x = node.GetDouble(0) ?? 0;
        var y = node.GetDouble(1) ?? 0;
        return new CoordPoint(Coord.FromMm(x), Coord.FromMm(y));
    }

    /// <summary>
    /// Parses all <c>(xy X Y)</c> children from a <c>(pts ...)</c> node.
    /// </summary>
    public static List<CoordPoint> ParsePoints(SExpr parent)
    {
        var pts = parent.GetChild("pts");
        if (pts is null) return [];

        var result = new List<CoordPoint>();
        foreach (var xy in pts.GetChildren("xy"))
        {
            result.Add(ParseXY(xy));
        }
        return result;
    }

    /// <summary>
    /// Parses stroke properties from a <c>(stroke (width W) (type T) (color R G B A))</c> child node.
    /// </summary>
    public static (Coord Width, LineStyle Style, EdaColor Color) ParseStroke(SExpr parent)
    {
        var (width, style, color, _) = ParseStrokeEx(parent);
        return (width, style, color);
    }

    /// <summary>
    /// Parses stroke properties including whether a color child node was present.
    /// </summary>
    public static (Coord Width, LineStyle Style, EdaColor Color, bool HasColor) ParseStrokeEx(SExpr parent)
    {
        var stroke = parent.GetChild("stroke");
        if (stroke is null)
            return (Coord.Zero, LineStyle.Solid, default, false);

        var width = Coord.FromMm(stroke.GetChild("width")?.GetDouble() ?? 0);
        var style = ParseLineStyle(stroke.GetChild("type")?.GetString());
        var colorNode = stroke.GetChild("color");
        var color = ParseColor(colorNode);
        return (width, style, color, colorNode is not null);
    }

    /// <summary>
    /// Parses text effects from an <c>(effects (font (size H W) ...) (justify ...))</c> child node.
    /// </summary>
    public static (Coord FontHeight, Coord FontWidth, TextJustification Justification, bool IsHidden, bool IsMirrored, bool IsBold, bool IsItalic, string? FontFace, Coord FontThickness, EdaColor FontColor) ParseTextEffects(SExpr parent)
    {
        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _) = ParseTextEffectsEx(parent);
        return (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor);
    }

    /// <summary>
    /// Parses text effects, also reporting whether the hide was a symbol value (KiCad 6) vs child node (KiCad 8).
    /// </summary>
    public static (Coord FontHeight, Coord FontWidth, TextJustification Justification, bool IsHidden, bool IsMirrored, bool IsBold, bool IsItalic, string? FontFace, Coord FontThickness, EdaColor FontColor, bool HideIsSymbolValue) ParseTextEffectsEx(SExpr parent)
    {
        var effects = parent.GetChild("effects");
        if (effects is null)
            return (Coord.FromMm(1.27), Coord.FromMm(1.27), TextJustification.MiddleCenter, false, false, false, false, null, Coord.Zero, default, false);

        var font = effects.GetChild("font");
        var sizeNode = font?.GetChild("size");
        var fontH = Coord.FromMm(sizeNode?.GetDouble(0) ?? 1.27);
        var fontW = Coord.FromMm(sizeNode?.GetDouble(1) ?? 1.27);
        // Bold/italic can appear as child elements (font (bold) (italic))
        // or as symbol values within the font node (font bold italic)
        var isBold = font?.GetChild("bold") is not null;
        var isItalic = font?.GetChild("italic") is not null;

        // Font face
        var fontFace = font?.GetChild("face")?.GetString();

        // Font thickness
        var fontThickness = Coord.FromMm(font?.GetChild("thickness")?.GetDouble() ?? 0);

        // Font color
        var fontColor = ParseColor(font?.GetChild("color"));

        if (font is not null)
        {
            foreach (var val in font.Values)
            {
                if (val is SExprSymbol sym)
                {
                    if (sym.Value == "bold") isBold = true;
                    if (sym.Value == "italic") isItalic = true;
                }
            }
        }

        var justification = TextJustification.MiddleCenter;
        var isMirrored = false;
        var justify = effects.GetChild("justify");
        if (justify is not null)
        {
            justification = ParseJustification(justify);
            isMirrored = justify.Values.Any(v => v is SExprSymbol s && s.Value == "mirror");
        }

        var hideIsSymbolValue = effects.Values.Any(v => v is SExprSymbol s && s.Value == "hide");
        var isHidden = effects.GetChild("hide") is not null || hideIsSymbolValue;

        return (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, hideIsSymbolValue);
    }

    /// <summary>
    /// Parses fill type from a <c>(fill (type TYPE))</c> or <c>(fill yes/no/solid)</c> child node.
    /// </summary>
    public static (SchFillType FillType, bool IsFilled, EdaColor FillColor) ParseFill(SExpr parent)
    {
        var (fillType, isFilled, fillColor, _) = ParseFillWithFormat(parent);
        return (fillType, isFilled, fillColor);
    }

    /// <summary>
    /// Parses fill type from a <c>(fill (type TYPE))</c> or <c>(fill yes/no/solid)</c> child node,
    /// also returning whether the PCB fill format was used.
    /// </summary>
    public static (SchFillType FillType, bool IsFilled, EdaColor FillColor, bool UsePcbFormat) ParseFillWithFormat(SExpr parent)
    {
        var fill = parent.GetChild("fill");
        if (fill is null)
            return (SchFillType.None, false, EdaColor.Transparent, false);

        // Check for KiCad 8 PCB format: (fill yes), (fill no), (fill solid)
        var boolVal = fill.GetBool();
        if (boolVal.HasValue)
        {
            var fillType = boolVal.Value ? SchFillType.Filled : SchFillType.None;
            return (fillType, boolVal.Value, EdaColor.Transparent, true);
        }

        var symVal = fill.GetString();
        if (symVal == "solid")
            return (SchFillType.Filled, true, EdaColor.Transparent, true);

        // Standard schematic format: (fill (type TYPE))
        var typeStr = fill.GetChild("type")?.GetString();
        var fillType2 = typeStr switch
        {
            "none" => SchFillType.None,
            "outline" => SchFillType.Filled,
            "background" => SchFillType.Background,
            "color" => SchFillType.Color,
            _ => SchFillType.None
        };

        var color = ParseColor(fill.GetChild("color"));
        return (fillType2, fillType2 != SchFillType.None, color, false);
    }

    /// <summary>
    /// Parses a color from a <c>(color R G B A)</c> node.
    /// </summary>
    public static EdaColor ParseColor(SExpr? node)
    {
        if (node is null) return default;

        var r = (byte)Math.Clamp(node.GetDouble(0) ?? 0, 0, 255);
        var g = (byte)Math.Clamp(node.GetDouble(1) ?? 0, 0, 255);
        var b = (byte)Math.Clamp(node.GetDouble(2) ?? 0, 0, 255);
        var a = (byte)Math.Clamp((node.GetDouble(3) ?? 1.0) * 255, 0, 255);
        return new EdaColor(r, g, b, a);
    }

    /// <summary>
    /// Checks whether a node has a specific symbol in its values list.
    /// </summary>
    public static bool HasSymbol(SExpr node, string symbol)
    {
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == symbol)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Parses a UUID from a <c>(uuid ...)</c> child node.
    /// </summary>
    public static string? ParseUuid(SExpr parent)
    {
        return parent.GetChild("uuid")?.GetString() ??
               parent.GetChild("tstamp")?.GetString();
    }

    /// <summary>
    /// Parses a UUID from a parent node, returning whether it was an unquoted symbol.
    /// </summary>
    public static (string? Uuid, bool IsSymbol) ParseUuidEx(SExpr parent)
    {
        var uuidNode = parent.GetChild("uuid") ?? parent.GetChild("tstamp");
        if (uuidNode is null)
            return (null, false);
        var value = uuidNode.GetString();
        var isSymbol = uuidNode.Values.Count > 0 && uuidNode.Values[0] is SExprSymbol;
        return (value, isSymbol);
    }

    /// <summary>
    /// Converts a KiCad electrical type string to the shared enum.
    /// </summary>
    public static PinElectricalType ParsePinElectricalType(string? type)
    {
        return type switch
        {
            "input" => PinElectricalType.Input,
            "output" => PinElectricalType.Output,
            "bidirectional" => PinElectricalType.Bidirectional,
            "passive" => PinElectricalType.Passive,
            "tri_state" => PinElectricalType.TriState,
            "power_in" => PinElectricalType.PowerIn,
            "power_out" => PinElectricalType.PowerOut,
            "open_collector" => PinElectricalType.OpenCollector,
            "open_emitter" => PinElectricalType.OpenEmitter,
            "unspecified" => PinElectricalType.Unspecified,
            "no_connect" => PinElectricalType.NoConnect,
            "free" => PinElectricalType.Free,
            _ => PinElectricalType.Unspecified
        };
    }

    /// <summary>
    /// Converts a KiCad pin graphic style string to the enum.
    /// </summary>
    public static PinGraphicStyle ParsePinGraphicStyle(string? style)
    {
        return style switch
        {
            "line" => PinGraphicStyle.Line,
            "inverted" => PinGraphicStyle.Inverted,
            "clock" => PinGraphicStyle.Clock,
            "inverted_clock" => PinGraphicStyle.InvertedClock,
            "input_low" => PinGraphicStyle.InputLow,
            "clock_low" => PinGraphicStyle.ClockLow,
            "output_low" => PinGraphicStyle.OutputLow,
            "edge_clock_high" => PinGraphicStyle.EdgeClockHigh,
            "non_logic" => PinGraphicStyle.NonLogic,
            _ => PinGraphicStyle.Line
        };
    }

    /// <summary>
    /// Converts a KiCad angle (degrees) to <see cref="PinOrientation"/>.
    /// </summary>
    public static PinOrientation AngleToPinOrientation(double angle)
    {
        var normalized = ((int)angle % 360 + 360) % 360;
        return normalized switch
        {
            0 => PinOrientation.Right,
            90 => PinOrientation.Up,
            180 => PinOrientation.Left,
            270 => PinOrientation.Down,
            _ => PinOrientation.Right
        };
    }

    /// <summary>
    /// Parses a <see cref="LineStyle"/> from a KiCad stroke type string.
    /// </summary>
    public static LineStyle ParseLineStyle(string? type)
    {
        return type switch
        {
            "solid" => LineStyle.Solid,
            "dash" => LineStyle.Dash,
            "dot" => LineStyle.Dot,
            "dash_dot" => LineStyle.DashDot,
            "dash_dot_dot" => LineStyle.DashDotDot,
            "default" => LineStyle.DefaultStyle,
            _ => LineStyle.Solid
        };
    }

    /// <summary>
    /// Parses <see cref="TextJustification"/> from a <c>(justify ...)</c> node.
    /// </summary>
    public static TextJustification ParseJustification(SExpr justifyNode)
    {
        var hasLeft = false;
        var hasRight = false;
        var hasTop = false;
        var hasBottom = false;

        foreach (var v in justifyNode.Values)
        {
            if (v is SExprSymbol s)
            {
                switch (s.Value)
                {
                    case "left": hasLeft = true; break;
                    case "right": hasRight = true; break;
                    case "top": hasTop = true; break;
                    case "bottom": hasBottom = true; break;
                }
            }
        }

        if (hasTop && hasLeft) return TextJustification.TopLeft;
        if (hasTop && hasRight) return TextJustification.TopRight;
        if (hasTop) return TextJustification.TopCenter;
        if (hasBottom && hasLeft) return TextJustification.BottomLeft;
        if (hasBottom && hasRight) return TextJustification.BottomRight;
        if (hasBottom) return TextJustification.BottomCenter;
        if (hasLeft) return TextJustification.MiddleLeft;
        if (hasRight) return TextJustification.MiddleRight;
        return TextJustification.MiddleCenter;
    }

    /// <summary>
    /// Computes center, radius, start angle, and end angle from three points on an arc (start, mid, end).
    /// </summary>
    public static (CoordPoint Center, Coord Radius, double StartAngle, double EndAngle) ComputeArcFromThreePoints(
        CoordPoint start, CoordPoint mid, CoordPoint end)
    {
        // Convert to doubles in mm for calculation
        var ax = start.X.ToMm();
        var ay = start.Y.ToMm();
        var bx = mid.X.ToMm();
        var by = mid.Y.ToMm();
        var cx = end.X.ToMm();
        var cy = end.Y.ToMm();

        // Compute center of circle through 3 points
        var d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Math.Abs(d) < 1e-10)
        {
            // Degenerate case: collinear points
            var midPt = new CoordPoint(Coord.FromMm((ax + cx) / 2), Coord.FromMm((ay + cy) / 2));
            return (midPt, Coord.Zero, 0, 0);
        }

        var ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        var uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

        var center = new CoordPoint(Coord.FromMm(ux), Coord.FromMm(uy));
        var radius = Coord.FromMm(Math.Sqrt((ax - ux) * (ax - ux) + (ay - uy) * (ay - uy)));

        var startAngle = Math.Atan2(ay - uy, ax - ux) * 180.0 / Math.PI;
        var endAngle = Math.Atan2(cy - uy, cx - ux) * 180.0 / Math.PI;

        return (center, radius, startAngle, endAngle);
    }

    /// <summary>
    /// Converts a KiCad pad shape string to <see cref="PadShape"/>.
    /// </summary>
    public static PadShape ParsePadShape(string? shape)
    {
        return shape switch
        {
            "circle" => PadShape.Circle,
            "rect" => PadShape.Rect,
            "oval" => PadShape.Oval,
            "roundrect" => PadShape.RoundRect,
            "trapezoid" => PadShape.Trapezoid,
            "custom" => PadShape.Custom,
            _ => PadShape.Circle
        };
    }

    /// <summary>
    /// Converts a KiCad pad type string to <see cref="PadType"/>.
    /// </summary>
    public static PadType ParsePadType(string? type)
    {
        return type switch
        {
            "thru_hole" => PadType.ThruHole,
            "smd" => PadType.Smd,
            "np_thru_hole" => PadType.NpThruHole,
            "connect" => PadType.Connect,
            _ => PadType.ThruHole
        };
    }

    /// <summary>
    /// Gets the <see cref="PinElectricalType"/> string representation for writing.
    /// </summary>
    public static string PinElectricalTypeToString(PinElectricalType type)
    {
        return type switch
        {
            PinElectricalType.Input => "input",
            PinElectricalType.Output => "output",
            PinElectricalType.Bidirectional => "bidirectional",
            PinElectricalType.Passive => "passive",
            PinElectricalType.TriState => "tri_state",
            PinElectricalType.PowerIn => "power_in",
            PinElectricalType.PowerOut => "power_out",
            PinElectricalType.OpenCollector => "open_collector",
            PinElectricalType.OpenEmitter => "open_emitter",
            PinElectricalType.Unspecified => "unspecified",
            PinElectricalType.NoConnect => "no_connect",
            PinElectricalType.Free => "free",
            _ => "unspecified"
        };
    }

    /// <summary>
    /// Gets the <see cref="PinGraphicStyle"/> string representation for writing.
    /// </summary>
    public static string PinGraphicStyleToString(PinGraphicStyle style)
    {
        return style switch
        {
            PinGraphicStyle.Line => "line",
            PinGraphicStyle.Inverted => "inverted",
            PinGraphicStyle.Clock => "clock",
            PinGraphicStyle.InvertedClock => "inverted_clock",
            PinGraphicStyle.InputLow => "input_low",
            PinGraphicStyle.ClockLow => "clock_low",
            PinGraphicStyle.OutputLow => "output_low",
            PinGraphicStyle.EdgeClockHigh => "edge_clock_high",
            PinGraphicStyle.NonLogic => "non_logic",
            _ => "line"
        };
    }

    /// <summary>
    /// Gets the <see cref="PinOrientation"/> angle in degrees for writing.
    /// </summary>
    public static double PinOrientationToAngle(PinOrientation orientation)
    {
        return orientation switch
        {
            PinOrientation.Right => 0,
            PinOrientation.Up => 90,
            PinOrientation.Left => 180,
            PinOrientation.Down => 270,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the <see cref="LineStyle"/> string representation for writing.
    /// </summary>
    public static string LineStyleToString(LineStyle style)
    {
        return style switch
        {
            LineStyle.Solid => "solid",
            LineStyle.Dash => "dash",
            LineStyle.Dot => "dot",
            LineStyle.DashDot => "dash_dot",
            LineStyle.DashDotDot => "dash_dot_dot",
            LineStyle.DefaultStyle => "default",
            _ => "solid"
        };
    }

    /// <summary>
    /// Gets the <see cref="PadShape"/> string representation for writing.
    /// </summary>
    public static string PadShapeToString(PadShape shape)
    {
        return shape switch
        {
            PadShape.Circle => "circle",
            PadShape.Rect => "rect",
            PadShape.Oval => "oval",
            PadShape.RoundRect => "roundrect",
            PadShape.Trapezoid => "trapezoid",
            PadShape.Custom => "custom",
            _ => "circle"
        };
    }

    /// <summary>
    /// Gets the <see cref="PadType"/> string representation for writing.
    /// </summary>
    public static string PadTypeToString(PadType type)
    {
        return type switch
        {
            PadType.ThruHole => "thru_hole",
            PadType.Smd => "smd",
            PadType.NpThruHole => "np_thru_hole",
            PadType.Connect => "connect",
            _ => "thru_hole"
        };
    }

    /// <summary>
    /// Gets the <see cref="SchFillType"/> string representation for writing.
    /// </summary>
    public static string SchFillTypeToString(SchFillType fillType)
    {
        return fillType switch
        {
            SchFillType.None => "none",
            SchFillType.Filled => "outline",
            SchFillType.Background => "background",
            SchFillType.Color => "color",
            _ => "none"
        };
    }

    /// <summary>
    /// Converts a sheet pin I/O type integer to its KiCad string representation.
    /// </summary>
    public static string SheetPinIoTypeToString(int ioType) => ioType switch
    {
        0 => "input",
        1 => "output",
        2 => "bidirectional",
        3 => "tri_state",
        4 => "passive",
        _ => "bidirectional"
    };

    /// <summary>
    /// Converts a KiCad sheet pin I/O type string to its integer representation.
    /// </summary>
    public static int StringToSheetPinIoType(string value) => value switch
    {
        "input" => 0,
        "output" => 1,
        "bidirectional" => 2,
        "tri_state" => 3,
        "passive" => 4,
        _ => 2
    };

    /// <summary>
    /// Converts a sheet pin side integer to angle in degrees.
    /// </summary>
    public static double SheetPinSideToAngle(int side) => side switch
    {
        0 => 180.0,
        1 => 0.0,
        2 => 90.0,
        3 => 270.0,
        _ => 0.0
    };

    /// <summary>
    /// Converts an angle in degrees to a sheet pin side integer.
    /// </summary>
    public static int AngleToSheetPinSide(double angle) => ((int)angle % 360) switch
    {
        180 => 0,
        0 => 1,
        90 => 2,
        270 => 3,
        _ => 1
    };
}
