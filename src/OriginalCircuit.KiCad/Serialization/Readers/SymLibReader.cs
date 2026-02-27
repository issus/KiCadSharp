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

        const int MaxTestedVersion = 20231120;

        var generatorNode = root.GetChild("generator");
        var lib = new KiCadSymLib
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = generatorNode?.GetString(),
            GeneratorIsSymbol = generatorNode?.Values.Count > 0 && generatorNode.Values[0] is SExprSymbol,
            GeneratorVersion = root.GetChild("generator_version")?.GetString()
        };

        var diagnostics = new List<KiCadDiagnostic>();

        if (lib.Version > MaxTestedVersion)
        {
            diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                $"File format version {lib.Version} is newer than the maximum tested version {MaxTestedVersion}. Some features may not be parsed correctly."));
        }

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
        lib.SourceTree = root;
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
            component.PinNamesPresent = true;
            var offsetChild = pinNames.GetChild("offset");
            component.PinNamesHasOffset = offsetChild is not null;
            var offset = offsetChild?.GetDouble() ?? 0;
            component.PinNamesOffset = Coord.FromMm(offset);
            // KiCad 6: (pin_names ... hide) — symbol value
            // KiCad 8: (pin_names ... (hide yes)) — child node
            component.HidePinNames = pinNames.Values.Any(v => v is SExprSymbol s && s.Value == "hide")
                                     || pinNames.GetChild("hide")?.GetBool() == true;
        }

        // Parse pin_numbers
        var pinNumbers = node.GetChild("pin_numbers");
        if (pinNumbers is not null)
        {
            component.PinNumbersPresent = true;
            // KiCad 6: (pin_numbers hide) — symbol value
            // KiCad 8: (pin_numbers (hide yes)) — child node
            component.HidePinNumbers = pinNumbers.Values.Any(v => v is SExprSymbol s && s.Value == "hide")
                                       || pinNumbers.GetChild("hide")?.GetBool() == true;
        }

        component.InBom = node.GetChild("in_bom")?.GetBool() ?? true;
        component.OnBoard = node.GetChild("on_board")?.GetBool() ?? true;

        var dupPinNode = node.GetChild("duplicate_pin_numbers_are_jumpers");
        if (dupPinNode is not null)
        {
            component.DuplicatePinNumbersAreJumpersPresent = true;
            component.DuplicatePinNumbersAreJumpers = dupPinNode.GetBool() ?? false;
        }

        var excludeFromSimNode = node.GetChild("exclude_from_sim");
        if (excludeFromSimNode is not null)
        {
            component.ExcludeFromSimPresent = true;
            component.ExcludeFromSim = excludeFromSimNode.GetBool() ?? false;
        }

        var embeddedFontsNode = node.GetChild("embedded_fonts");
        component.EmbeddedFonts = embeddedFontsNode is not null ? embeddedFontsNode.GetBool() : null;
        var powerNode = node.GetChild("power");
        component.IsPower = powerNode is not null;
        if (powerNode is not null)
            component.PowerType = powerNode.GetString();
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
                    var pin = ParsePin(child);
                    pins.Add(pin);
                    component.OrderedPrimitivesList.Add(pin);
                    break;
                case "polyline":
                    {
                        var beforeLines = lines.Count;
                        var beforePolylines = polylines.Count;
                        var beforePolygons = polygons.Count;
                        ParsePolylineOrLine(child, lines, polylines, polygons);
                        if (lines.Count > beforeLines)
                            component.OrderedPrimitivesList.Add(lines[^1]);
                        else if (polylines.Count > beforePolylines)
                            component.OrderedPrimitivesList.Add(polylines[^1]);
                        else if (polygons.Count > beforePolygons)
                            component.OrderedPrimitivesList.Add(polygons[^1]);
                    }
                    break;
                case "rectangle":
                    var rect = ParseRectangle(child);
                    rectangles.Add(rect);
                    component.OrderedPrimitivesList.Add(rect);
                    break;
                case "arc":
                    var arc = ParseSchArc(child);
                    arcs.Add(arc);
                    component.OrderedPrimitivesList.Add(arc);
                    break;
                case "circle":
                    var circle = ParseCircle(child);
                    circles.Add(circle);
                    component.OrderedPrimitivesList.Add(circle);
                    break;
                case "bezier":
                    var bez = ParseBezier(child);
                    beziers.Add(bez);
                    component.OrderedPrimitivesList.Add(bez);
                    break;
                case "text":
                    var lbl = ParseTextLabel(child);
                    labels.Add(lbl);
                    component.OrderedPrimitivesList.Add(lbl);
                    break;
                case "property":
                case "pin_names":
                case "pin_numbers":
                case "in_bom":
                case "on_board":
                case "exclude_from_sim":
                case "embedded_fonts":
                case "power":
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

        // Check name effects for hide and parse font sizes
        var nameEffects = nameNode?.GetChild("effects");
        if (nameEffects is not null)
        {
            var nameFont = nameEffects.GetChild("font");
            var nameSize = nameFont?.GetChild("size");
            if (nameSize is not null)
            {
                var h = nameSize.GetDouble(0) ?? 0;
                var w = nameSize.GetDouble(1) ?? 0;
                pin.NameFontSizeHeight = Coord.FromMm(h);
                pin.NameFontSizeWidth = Coord.FromMm(w);
                if (h == 0 && w == 0)
                    pin.ShowName = false;
            }
            if (nameEffects.GetChild("hide") is not null ||
                nameEffects.Values.Any(v => v is SExprSymbol s && s.Value == "hide"))
                pin.ShowName = false;
        }

        // Check number effects for hide and parse font sizes
        var numEffects = numberNode?.GetChild("effects");
        if (numEffects is not null)
        {
            var numFont = numEffects.GetChild("font");
            var numSize = numFont?.GetChild("size");
            if (numSize is not null)
            {
                var h = numSize.GetDouble(0) ?? 0;
                var w = numSize.GetDouble(1) ?? 0;
                pin.NumberFontSizeHeight = Coord.FromMm(h);
                pin.NumberFontSizeWidth = Coord.FromMm(w);
                if (h == 0 && w == 0)
                    pin.ShowDesignator = false;
            }
            if (numEffects.GetChild("hide") is not null ||
                numEffects.Values.Any(v => v is SExprSymbol s && s.Value == "hide"))
                pin.ShowDesignator = false;
        }

        // Check if pin is hidden
        // KiCad 6: (pin ... hide) — symbol value
        // KiCad 8: (pin ... (hide yes)) — child node
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "hide")
            {
                pin.IsHidden = true;
                pin.HideIsSymbolValue = true;
                break;
            }
        }
        if (!pin.IsHidden)
        {
            var hideChild = node.GetChild("hide");
            if (hideChild is not null)
            {
                pin.IsHidden = hideChild.GetBool() ?? true;
                pin.HideIsSymbolValue = false;
            }
        }

        // Parse alternates
        foreach (var altNode in node.GetChildren("alternate"))
        {
            pin.Alternates.Add(new KiCadSchPinAlternate
            {
                Name = altNode.GetString(0) ?? "",
                ElectricalType = SExpressionHelper.ParsePinElectricalType(altNode.GetString(1)),
                GraphicStyle = SExpressionHelper.ParsePinGraphicStyle(altNode.GetString(2))
            });
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

        // Detect inline property format (no at, no effects — e.g., "(property ki_fp_filters ...)")
        var hasAt = node.GetChild("at") is not null;
        var hasEffects = node.GetChild("effects") is not null;
        param.IsInline = !hasAt && !hasEffects;

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        param.Location = loc;
        param.Orientation = (int)angle;

        // Parse id
        var idNode = node.GetChild("id");
        if (idNode is not null)
        {
            var idVal = idNode.GetInt();
            if (idVal.HasValue)
                param.Id = idVal.Value;
        }

        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, _, fontThickness, _, hideIsSymbolValue) = SExpressionHelper.ParseTextEffectsEx(node);
        param.FontSizeHeight = fontH;
        param.FontSizeWidth = fontW;
        param.Justification = justification;
        param.IsVisible = !isHidden;
        param.IsMirrored = isMirrored;
        param.IsBold = isBold;
        param.IsItalic = isItalic;
        param.FontThickness = fontThickness;
        param.HideIsSymbolValue = hideIsSymbolValue;

        // Parse font face and color from effects -> font
        var effects = node.GetChild("effects");
        if (effects is not null)
        {
            var font = effects.GetChild("font");
            if (font is not null)
            {
                var faceNode = font.GetChild("face");
                if (faceNode is not null)
                    param.FontFace = faceNode.GetString();

                var colorNode = font.GetChild("color");
                if (colorNode is not null)
                    param.FontColor = SExpressionHelper.ParseColor(colorNode);
            }

            // Parse line_spacing (sibling of font in effects)
            var lineSpacingNode = effects.GetChild("line_spacing");
            if (lineSpacingNode is not null)
            {
                param.LineSpacing = lineSpacingNode.GetDouble();
            }
        }

        // KiCad 8 footprint property attributes
        param.LayerName = node.GetChild("layer")?.GetString();
        param.Uuid = SExpressionHelper.ParseUuid(node);

        // (unlocked yes)
        var unlockedNode = node.GetChild("unlocked");
        if (unlockedNode is not null)
            param.IsUnlocked = unlockedNode.GetBool() ?? true;

        // (hide yes) - separate from effects hide, this is a direct child
        var hideNode = node.GetChild("hide");
        if (hideNode is not null)
        {
            param.HideIsDirectChild = true;
            var hideVal = hideNode.GetBool();
            if (hideVal.HasValue && hideVal.Value)
                param.IsVisible = false;
        }

        // (do_not_autoplace) or (do_not_autoplace yes) - KiCad 9+
        var doNotAutoplaceNode = node.GetChild("do_not_autoplace");
        if (doNotAutoplaceNode is not null)
        {
            param.DoNotAutoplace = true;
            // Placed symbol properties use (do_not_autoplace yes), lib symbols use bare (do_not_autoplace)
            param.DoNotAutoplaceHasValue = doNotAutoplaceNode.Values.Count > 0;
        }

        return param;
    }

    private static void ParsePolylineOrLine(SExpr node,
        List<KiCadSchLine> lines, List<KiCadSchPolyline> polylines, List<KiCadSchPolygon> polygons)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color, hasColor) = SExpressionHelper.ParseStrokeEx(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        if (isFilled && pts.Count >= 3)
        {
            polygons.Add(new KiCadSchPolygon
            {
                Vertices = pts,
                Color = color,
                FillColor = fillColor,
                LineWidth = width,
                LineStyle = lineStyle,
                HasStrokeColor = hasColor,
                IsFilled = true,
                FillType = fillType
            });
        }
        else if (pts.Count == 2 && !isFilled)
        {
            lines.Add(new KiCadSchLine
            {
                Start = pts[0],
                End = pts[1],
                Color = color,
                Width = width,
                LineStyle = lineStyle,
                HasStrokeColor = hasColor
            });
        }
        else
        {
            polylines.Add(new KiCadSchPolyline
            {
                Vertices = pts,
                Color = color,
                LineWidth = width,
                LineStyle = lineStyle,
                HasStrokeColor = hasColor,
                FillType = fillType,
                FillColor = fillColor
            });
        }
    }

    private static KiCadSchRectangle ParseRectangle(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, lineStyle, color, hasColor) = SExpressionHelper.ParseStrokeEx(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadSchRectangle
        {
            Corner1 = start,
            Corner2 = end,
            Color = color,
            FillColor = fillColor,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasColor,
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
        var (width, lineStyle, color, hasColor) = SExpressionHelper.ParseStrokeEx(node);
        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        var (center, radius, startAngle, endAngle) = SExpressionHelper.ComputeArcFromThreePoints(start, mid, end);

        return new KiCadSchArc
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasColor,
            FillType = fillType,
            FillColor = fillColor,
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
        var (width, lineStyle, color, hasColor) = SExpressionHelper.ParseStrokeEx(node);
        var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadSchCircle
        {
            Center = center,
            Radius = radius,
            Color = color,
            FillColor = fillColor,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasColor,
            IsFilled = isFilled,
            FillType = fillType
        };
    }

    private static KiCadSchBezier ParseBezier(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color, hasColor) = SExpressionHelper.ParseStrokeEx(node);
        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadSchBezier
        {
            ControlPoints = pts,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasColor,
            FillType = fillType,
            FillColor = fillColor
        };
    }

    private static KiCadSchLabel ParseTextLabel(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, _, fontThickness, _) = SExpressionHelper.ParseTextEffects(node);
        var hasStroke = node.GetChild("stroke") is not null;
        var (strokeWidth, strokeStyle, strokeColor) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Rotation = angle,
            FontSizeHeight = fontH,
            FontSizeWidth = fontW,
            Justification = justification,
            IsHidden = isHidden,
            IsMirrored = isMirrored,
            IsBold = isBold,
            IsItalic = isItalic,
            HasStroke = hasStroke,
            StrokeWidth = strokeWidth,
            StrokeLineStyle = strokeStyle,
            StrokeColor = strokeColor,
            FontThickness = fontThickness
        };
    }
}
