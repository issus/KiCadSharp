using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Reads KiCad symbol library files (<c>.kicad_sym</c>) into <see cref="KiCadSymLib"/> objects.
/// </summary>
public static class SymLibReader
{
    /// <summary>
    /// Reads a symbol library from a file path.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed symbol library.</returns>
    public static async ValueTask<KiCadSymLib> ReadAsync(string path, CancellationToken ct = default)
    {
        SExpression.SExpression root;
        try
        {
            root = await SExpressionReader.ReadAsync(path, ct).ConfigureAwait(false);
        }
        catch (FormatException ex)
        {
            throw new KiCadFileException($"Failed to parse S-expression: {ex.Message}", path, innerException: ex);
        }
        return Parse(root);
    }

    /// <summary>
    /// Reads a symbol library from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed symbol library.</returns>
    public static async ValueTask<KiCadSymLib> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        SExpression.SExpression root;
        try
        {
            root = await SExpressionReader.ReadAsync(stream, ct).ConfigureAwait(false);
        }
        catch (FormatException ex)
        {
            throw new KiCadFileException($"Failed to parse S-expression: {ex.Message}", ex);
        }
        return Parse(root);
    }

    private static KiCadSymLib Parse(SExpr root)
    {
        if (root.Token != "kicad_symbol_lib")
            throw new KiCadFileException($"Expected 'kicad_symbol_lib' root token, got '{root.Token}'.");

        var lib = new KiCadSymLib
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = root.GetChild("generator")?.GetString(),
            GeneratorVersion = root.GetChild("generator_version")?.GetString()
        };

        var diagnostics = new List<KiCadDiagnostic>();

        foreach (var symbolNode in root.GetChildren("symbol"))
        {
            try
            {
                var component = ParseSymbol(symbolNode, diagnostics);
                lib.Add(component);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Error,
                    $"Failed to parse symbol: {ex.Message}",
                    symbolNode.GetString()));
            }
        }

        lib.DiagnosticList.AddRange(diagnostics);
        return lib;
    }

    internal static KiCadSchComponent ParseSymbol(SExpr node, List<KiCadDiagnostic> diagnostics)
    {
        var component = new KiCadSchComponent
        {
            Name = node.GetString() ?? ""
        };

        // Parse pin_names
        var pinNames = node.GetChild("pin_names");
        if (pinNames is not null)
        {
            var offset = pinNames.GetChild("offset")?.GetDouble() ?? 0;
            component.PinNamesOffset = Coord.FromMm(offset);
            component.HidePinNames = pinNames.Values.Any(v => v is SExprSymbol s && s.Value == "hide");
        }

        // Parse pin_numbers
        var pinNumbers = node.GetChild("pin_numbers");
        if (pinNumbers is not null)
        {
            component.HidePinNumbers = pinNumbers.Values.Any(v => v is SExprSymbol s && s.Value == "hide");
        }

        component.InBom = node.GetChild("in_bom")?.GetBool() ?? true;
        component.OnBoard = node.GetChild("on_board")?.GetBool() ?? true;
        component.Extends = node.GetChild("extends")?.GetString();

        // Parse properties
        var parameters = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            parameters.Add(ParseProperty(propNode));
        }
        component.ParameterList.AddRange(parameters);

        // Find description from properties
        var descProp = parameters.FirstOrDefault(p => p.Name == "ki_description");
        component.Description = descProp?.Value;

        // Parse sub-symbols and collect primitives
        var pins = new List<KiCadSchPin>();
        var lines = new List<KiCadSchLine>();
        var rectangles = new List<KiCadSchRectangle>();
        var arcs = new List<KiCadSchArc>();
        var circles = new List<KiCadSchCircle>();
        var beziers = new List<KiCadSchBezier>();
        var polylines = new List<KiCadSchPolyline>();
        var polygons = new List<KiCadSchPolygon>();
        var labels = new List<KiCadSchLabel>();
        var subSymbols = new List<KiCadSchComponent>();

        foreach (var child in node.Children)
        {
            switch (child.Token)
            {
                case "symbol":
                    var sub = ParseSymbol(child, diagnostics);
                    subSymbols.Add(sub);
                    // Collect primitives from sub-symbols
                    pins.AddRange(sub.Pins.OfType<KiCadSchPin>());
                    lines.AddRange(sub.Lines.OfType<KiCadSchLine>());
                    rectangles.AddRange(sub.Rectangles.OfType<KiCadSchRectangle>());
                    arcs.AddRange(sub.Arcs.OfType<KiCadSchArc>());
                    circles.AddRange(sub.Circles.OfType<KiCadSchCircle>());
                    beziers.AddRange(sub.Beziers.OfType<KiCadSchBezier>());
                    polylines.AddRange(sub.Polylines.OfType<KiCadSchPolyline>());
                    polygons.AddRange(sub.Polygons.OfType<KiCadSchPolygon>());
                    labels.AddRange(sub.Labels.OfType<KiCadSchLabel>());
                    break;
                case "pin":
                    pins.Add(ParsePin(child));
                    break;
                case "polyline":
                    ParsePolylineOrLine(child, lines, polylines, polygons);
                    break;
                case "rectangle":
                    rectangles.Add(ParseRectangle(child));
                    break;
                case "arc":
                    arcs.Add(ParseSchArc(child));
                    break;
                case "circle":
                    circles.Add(ParseCircle(child));
                    break;
                case "bezier":
                    beziers.Add(ParseBezier(child));
                    break;
                case "text":
                    labels.Add(ParseTextLabel(child));
                    break;
                case "property":
                case "pin_names":
                case "pin_numbers":
                case "in_bom":
                case "on_board":
                case "extends":
                    // Known tokens handled elsewhere
                    break;
                default:
                    diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                        $"Unknown symbol token '{child.Token}' was ignored", child.Token));
                    break;
            }
        }

        component.SubSymbolList.AddRange(subSymbols);
        component.PinList.AddRange(pins);
        component.LineList.AddRange(lines);
        component.RectangleList.AddRange(rectangles);
        component.ArcList.AddRange(arcs);
        component.CircleList.AddRange(circles);
        component.BezierList.AddRange(beziers);
        component.PolylineList.AddRange(polylines);
        component.PolygonList.AddRange(polygons);
        component.LabelList.AddRange(labels);

        // Compute part count from sub-symbol naming convention
        var maxUnit = 0;
        foreach (var sub in subSymbols)
        {
            // Sub-symbol names are like "R_0_1", "R_1_1", etc.
            var parts = sub.Name.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[^2], out var unitNum))
            {
                maxUnit = Math.Max(maxUnit, unitNum);
            }
        }
        component.PartCount = Math.Max(1, maxUnit + 1);

        return component;
    }

    internal static KiCadSchPin ParsePin(SExpr node)
    {
        var pin = new KiCadSchPin
        {
            ElectricalType = SExpressionHelper.ParsePinElectricalType(node.GetString(0)),
            GraphicStyle = SExpressionHelper.ParsePinGraphicStyle(node.GetString(1))
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        pin.Location = loc;
        pin.Orientation = SExpressionHelper.AngleToPinOrientation(angle);

        pin.Length = Coord.FromMm(node.GetChild("length")?.GetDouble() ?? 0);

        var nameNode = node.GetChild("name");
        pin.Name = nameNode?.GetString();
        var numberNode = node.GetChild("number");
        pin.Designator = numberNode?.GetString();

        // Check name/number effects for hide
        var nameEffects = nameNode?.GetChild("effects");
        if (nameEffects is not null)
        {
            var nameFont = nameEffects.GetChild("font");
            var nameSize = nameFont?.GetChild("size");
            if (nameSize is not null && nameSize.GetDouble(0) == 0 && nameSize.GetDouble(1) == 0)
                pin.ShowName = false;
            if (nameEffects.Values.Any(v => v is SExprSymbol s && s.Value == "hide"))
                pin.ShowName = false;
        }

        var numEffects = numberNode?.GetChild("effects");
        if (numEffects is not null)
        {
            var numFont = numEffects.GetChild("font");
            var numSize = numFont?.GetChild("size");
            if (numSize is not null && numSize.GetDouble(0) == 0 && numSize.GetDouble(1) == 0)
                pin.ShowDesignator = false;
            if (numEffects.Values.Any(v => v is SExprSymbol s && s.Value == "hide"))
                pin.ShowDesignator = false;
        }

        // Check if pin is hidden
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "hide")
            {
                pin.IsHidden = true;
                break;
            }
        }

        return pin;
    }

    internal static KiCadSchParameter ParseProperty(SExpr node)
    {
        var param = new KiCadSchParameter
        {
            Name = node.GetString(0) ?? "",
            Value = node.GetString(1) ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        param.Location = loc;
        param.Orientation = (int)angle;

        var (fontH, fontW, justification, isHidden, isMirrored, _, _) = SExpressionHelper.ParseTextEffects(node);
        param.FontSizeHeight = fontH;
        param.FontSizeWidth = fontW;
        param.Justification = justification;
        param.IsVisible = !isHidden;
        param.IsMirrored = isMirrored;

        return param;
    }

    private static void ParsePolylineOrLine(SExpr node,
        List<KiCadSchLine> lines, List<KiCadSchPolyline> polylines, List<KiCadSchPolygon> polygons)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color) = SExpressionHelper.ParseStroke(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        if (isFilled && pts.Count >= 3)
        {
            polygons.Add(new KiCadSchPolygon
            {
                Vertices = pts,
                Color = color,
                FillColor = fillColor,
                LineWidth = width,
                IsFilled = true,
                FillType = fillType
            });
        }
        else if (pts.Count == 2)
        {
            lines.Add(new KiCadSchLine
            {
                Start = pts[0],
                End = pts[1],
                Color = color,
                Width = width,
                LineStyle = lineStyle
            });
        }
        else
        {
            polylines.Add(new KiCadSchPolyline
            {
                Vertices = pts,
                Color = color,
                LineWidth = width,
                LineStyle = lineStyle
            });
        }
    }

    private static KiCadSchRectangle ParseRectangle(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, _, color) = SExpressionHelper.ParseStroke(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadSchRectangle
        {
            Corner1 = start,
            Corner2 = end,
            Color = color,
            FillColor = fillColor,
            LineWidth = width,
            IsFilled = isFilled,
            FillType = fillType
        };
    }

    private static KiCadSchArc ParseSchArc(SExpr node)
    {
        var startNode = node.GetChild("start");
        var midNode = node.GetChild("mid");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var mid = midNode is not null ? SExpressionHelper.ParseXY(midNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, _, color) = SExpressionHelper.ParseStroke(node);

        var (center, radius, startAngle, endAngle) = SExpressionHelper.ComputeArcFromThreePoints(start, mid, end);

        return new KiCadSchArc
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Color = color,
            LineWidth = width,
            ArcStart = start,
            ArcMid = mid,
            ArcEnd = end
        };
    }

    private static KiCadSchCircle ParseCircle(SExpr node)
    {
        var centerNode = node.GetChild("center");
        var center = centerNode is not null ? SExpressionHelper.ParseXY(centerNode) : CoordPoint.Zero;
        var radius = Coord.FromMm(node.GetChild("radius")?.GetDouble() ?? 0);
        var (width, _, color) = SExpressionHelper.ParseStroke(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadSchCircle
        {
            Center = center,
            Radius = radius,
            Color = color,
            FillColor = fillColor,
            LineWidth = width,
            IsFilled = isFilled,
            FillType = fillType
        };
    }

    private static KiCadSchBezier ParseBezier(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, _, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchBezier
        {
            ControlPoints = pts,
            Color = color,
            LineWidth = width
        };
    }

    private static KiCadSchLabel ParseTextLabel(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var (_, _, justification, isHidden, isMirrored, _, _) = SExpressionHelper.ParseTextEffects(node);

        return new KiCadSchLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Rotation = angle,
            Justification = justification,
            IsHidden = isHidden,
            IsMirrored = isMirrored
        };
    }
}
