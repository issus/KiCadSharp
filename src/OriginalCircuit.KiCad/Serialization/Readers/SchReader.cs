using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Reads KiCad schematic files (<c>.kicad_sch</c>) into <see cref="KiCadSch"/> objects.
/// </summary>
public static class SchReader
{
    /// <summary>
    /// Reads a schematic document from a file path.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed schematic document.</returns>
    public static async ValueTask<KiCadSch> ReadAsync(string path, CancellationToken ct = default)
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
    /// Reads a schematic document from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed schematic document.</returns>
    public static async ValueTask<KiCadSch> ReadAsync(Stream stream, CancellationToken ct = default)
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

    private static KiCadSch Parse(SExpr root)
    {
        if (root.Token != "kicad_sch")
            throw new KiCadFileException($"Expected 'kicad_sch' root token, got '{root.Token}'.");

        const int MaxTestedVersion = 20231120;

        var sch = new KiCadSch
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = root.GetChild("generator")?.GetString(),
            GeneratorVersion = root.GetChild("generator_version")?.GetString(),
            Uuid = SExpressionHelper.ParseUuid(root),
            Paper = root.GetChild("paper")?.GetString(),
            TitleBlock = root.GetChild("title_block"),
            SheetInstances = root.GetChild("sheet_instances"),
            SymbolInstances = root.GetChild("symbol_instances")
        };

        var diagnostics = new List<KiCadDiagnostic>();

        if (sch.Version > MaxTestedVersion)
        {
            diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                $"File format version {sch.Version} is newer than the maximum tested version {MaxTestedVersion}. Some features may not be parsed correctly."));
        }
        var wires = new List<KiCadSchWire>();
        var junctions = new List<KiCadSchJunction>();
        var netLabels = new List<KiCadSchNetLabel>();
        var labels = new List<KiCadSchLabel>();
        var noConnects = new List<KiCadSchNoConnect>();
        var buses = new List<KiCadSchBus>();
        var busEntries = new List<KiCadSchBusEntry>();
        var powerObjects = new List<KiCadSchPowerObject>();
        var components = new List<KiCadSchComponent>();
        var sheets = new List<KiCadSchSheet>();
        var libSymbols = new List<KiCadSchComponent>();
        var polylines = new List<KiCadSchPolyline>();
        var lines = new List<KiCadSchLine>();
        var circles = new List<KiCadSchCircle>();
        var rectangles = new List<KiCadSchRectangle>();
        var arcs = new List<KiCadSchArc>();
        var beziers = new List<KiCadSchBezier>();

        // Parse lib_symbols section
        var libSymbolsNode = root.GetChild("lib_symbols");
        if (libSymbolsNode is not null)
        {
            foreach (var symNode in libSymbolsNode.GetChildren("symbol"))
            {
                try
                {
                    libSymbols.Add(SymLibReader.ParseSymbol(symNode, diagnostics));
                }
                catch (Exception ex)
                {
                    diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Error,
                        $"Failed to parse lib symbol: {ex.Message}", symNode.GetString()));
                }
            }
        }

        foreach (var child in root.Children)
        {
            try
            {
                switch (child.Token)
                {
                    case "wire":
                        wires.Add(ParseWire(child));
                        break;
                    case "junction":
                        junctions.Add(ParseJunction(child));
                        break;
                    case "label":
                        netLabels.Add(ParseNetLabel(child, NetLabelType.Local));
                        break;
                    case "global_label":
                        netLabels.Add(ParseNetLabel(child, NetLabelType.Global));
                        break;
                    case "hierarchical_label":
                        netLabels.Add(ParseNetLabel(child, NetLabelType.Hierarchical));
                        break;
                    case "text":
                        labels.Add(ParseTextLabel(child));
                        break;
                    case "no_connect":
                        noConnects.Add(ParseNoConnect(child));
                        break;
                    case "bus":
                        buses.Add(ParseBus(child));
                        break;
                    case "bus_entry":
                        busEntries.Add(ParseBusEntry(child));
                        break;
                    case "symbol":
                        var comp = ParsePlacedSymbol(child, diagnostics);
                        components.Add(comp);
                        break;
                    case "sheet":
                        sheets.Add(ParseSheet(child));
                        break;
                    case "power_port":
                        powerObjects.Add(ParsePowerPort(child));
                        break;
                    case "polyline":
                        ParseSchPolylineOrLine(child, lines, polylines);
                        break;
                    case "circle":
                        circles.Add(ParseSchCircle(child));
                        break;
                    case "rectangle":
                        rectangles.Add(ParseSchRectangle(child));
                        break;
                    case "arc":
                        arcs.Add(ParseSchArc(child));
                        break;
                    case "bezier":
                        beziers.Add(ParseSchBezier(child));
                        break;
                    case "version":
                    case "generator":
                    case "generator_version":
                    case "uuid":
                    case "lib_symbols":
                    case "paper":
                    case "title_block":
                    case "sheet_instances":
                    case "symbol_instances":
                        // Known tokens handled elsewhere or intentionally skipped
                        break;
                    default:
                        diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                            $"Unknown schematic token '{child.Token}' was ignored", child.Token));
                        break;
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Error,
                    $"Failed to parse {child.Token}: {ex.Message}", child.GetString()));
            }
        }

        sch.WireList.AddRange(wires);
        sch.JunctionList.AddRange(junctions);
        sch.NetLabelList.AddRange(netLabels);
        sch.LabelList.AddRange(labels);
        sch.NoConnectList.AddRange(noConnects);
        sch.BusList.AddRange(buses);
        sch.BusEntryList.AddRange(busEntries);
        sch.PowerObjectList.AddRange(powerObjects);
        sch.ComponentList.AddRange(components);
        sch.SheetList.AddRange(sheets);
        sch.LibSymbolList.AddRange(libSymbols);
        sch.PolylineList.AddRange(polylines);
        sch.LineList.AddRange(lines);
        sch.CircleList.AddRange(circles);
        sch.RectangleList.AddRange(rectangles);
        sch.ArcList.AddRange(arcs);
        sch.BezierList.AddRange(beziers);
        sch.DiagnosticList.AddRange(diagnostics);
        sch.SourceTree = root;

        return sch;
    }

    private static KiCadSchWire ParseWire(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchWire
        {
            Vertices = pts,
            LineWidth = width,
            LineStyle = style,
            Color = color,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchJunction ParseJunction(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);
        var diameter = Coord.FromMm(node.GetChild("diameter")?.GetDouble() ?? 0);
        var colorNode = node.GetChild("color");
        var color = SExpressionHelper.ParseColor(colorNode);

        return new KiCadSchJunction
        {
            Location = loc,
            Size = diameter,
            Color = color,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchNetLabel ParseNetLabel(SExpr node, NetLabelType labelType)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var (fontH, fontW, justification, _, isMirrored, isBold, isItalic, _, _, _) = SExpressionHelper.ParseTextEffects(node);

        // Parse shape for global/hierarchical labels
        var shape = node.GetChild("shape")?.GetString();

        // Parse fields_autoplaced
        var fieldsAutoplaced = node.GetChild("fields_autoplaced")?.GetBool() ?? false;

        // Parse properties (for global/hierarchical labels)
        var properties = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            properties.Add(SymLibReader.ParseProperty(propNode));
        }

        return new KiCadSchNetLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Orientation = (int)angle,
            Justification = justification,
            LabelType = labelType,
            FontSizeHeight = fontH,
            FontSizeWidth = fontW,
            IsBold = isBold,
            IsItalic = isItalic,
            IsMirrored = isMirrored,
            Shape = shape,
            FieldsAutoplaced = fieldsAutoplaced,
            Properties = properties,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchLabel ParseTextLabel(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, _, _, _) = SExpressionHelper.ParseTextEffects(node);

        return new KiCadSchLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Rotation = angle,
            Justification = justification,
            IsHidden = isHidden,
            IsMirrored = isMirrored,
            FontSizeHeight = fontH,
            FontSizeWidth = fontW,
            IsBold = isBold,
            IsItalic = isItalic,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchNoConnect ParseNoConnect(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);

        return new KiCadSchNoConnect
        {
            Location = loc,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchBus ParseBus(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchBus
        {
            Vertices = pts,
            LineWidth = width,
            LineStyle = style,
            Color = color,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchBusEntry ParseBusEntry(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);
        var sizeNode = node.GetChild("size");
        var sx = Coord.FromMm(sizeNode?.GetDouble(0) ?? 0);
        var sy = Coord.FromMm(sizeNode?.GetDouble(1) ?? 0);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchBusEntry
        {
            Location = loc,
            Corner = new CoordPoint(loc.X + sx, loc.Y + sy),
            LineWidth = width,
            LineStyle = style,
            Color = color,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchPowerObject ParsePowerPort(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);

        return new KiCadSchPowerObject
        {
            Location = loc,
            Text = node.GetString(),
            Rotation = angle,
            Uuid = SExpressionHelper.ParseUuid(node),
            RawNode = node
        };
    }

    private static KiCadSchSheet ParseSheet(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);
        var sizeNode = node.GetChild("size");
        var w = Coord.FromMm(sizeNode?.GetDouble(0) ?? 0);
        var h = Coord.FromMm(sizeNode?.GetDouble(1) ?? 0);
        var (strokeWidth, strokeStyle, strokeColor) = SExpressionHelper.ParseStroke(node);
        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        var sheetName = "";
        var fileName = "";
        var sheetProperties = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            var prop = SymLibReader.ParseProperty(propNode);
            sheetProperties.Add(prop);

            var key = propNode.GetString(0);
            if (key == "Sheetname" || key == "Sheet name") sheetName = prop.Value;
            else if (key == "Sheetfile" || key == "Sheet file") fileName = prop.Value;
        }

        var pins = new List<KiCadSchSheetPin>();
        foreach (var pinNode in node.GetChildren("pin"))
        {
            pins.Add(ParseSheetPin(pinNode));
        }

        var fieldsAutoplaced = node.GetChild("fields_autoplaced")?.GetBool() ?? false;
        var instances = node.GetChild("instances");

        return new KiCadSchSheet
        {
            Location = loc,
            Size = new CoordPoint(w, h),
            SheetName = sheetName,
            FileName = fileName,
            Pins = pins,
            Color = strokeColor,
            FillColor = fillColor,
            LineWidth = strokeWidth,
            LineStyle = strokeStyle,
            FillType = fillType,
            FieldsAutoplaced = fieldsAutoplaced,
            SheetProperties = sheetProperties,
            Instances = instances,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchSheetPin ParseSheetPin(SExpr node)
    {
        var name = node.GetString(0) ?? "";
        var ioTypeStr = node.GetString(1) ?? "bidirectional";
        var ioType = SExpressionHelper.StringToSheetPinIoType(ioTypeStr);

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var side = SExpressionHelper.AngleToSheetPinSide(angle);

        return new KiCadSchSheetPin
        {
            Name = name,
            IoType = ioType,
            Side = side,
            Location = loc,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchComponent ParsePlacedSymbol(SExpr node, List<KiCadDiagnostic> diagnostics)
    {
        var component = new KiCadSchComponent
        {
            Name = node.GetChild("lib_id")?.GetString() ?? node.GetString() ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        component.Location = loc;
        component.Rotation = angle;
        component.Uuid = SExpressionHelper.ParseUuid(node);

        // Parse mirror - support "x", "y", and "xy"
        var mirror = node.GetChild("mirror");
        if (mirror is not null)
        {
            var mirrorVal = mirror.GetString();
            if (mirrorVal == "xy" || mirrorVal == "yx")
            {
                component.IsMirroredX = true;
                component.IsMirroredY = true;
            }
            else
            {
                component.IsMirroredX = mirrorVal == "x";
                component.IsMirroredY = mirrorVal == "y";
            }
        }

        // Parse unit
        var unitNode = node.GetChild("unit");
        if (unitNode is not null)
            component.Unit = unitNode.GetInt() ?? 1;

        // Parse in_bom / on_board for placed symbols
        var inBomNode = node.GetChild("in_bom");
        if (inBomNode is not null)
            component.InBom = inBomNode.GetBool() ?? true;

        var onBoardNode = node.GetChild("on_board");
        if (onBoardNode is not null)
            component.OnBoard = onBoardNode.GetBool() ?? true;

        // Parse convert / body_style
        var convertNode = node.GetChild("convert");
        if (convertNode is not null)
            component.BodyStyle = convertNode.GetInt() ?? 0;
        var bodyStyleNode = node.GetChild("body_style");
        if (bodyStyleNode is not null)
            component.BodyStyle = bodyStyleNode.GetInt() ?? 0;

        // Parse fields_autoplaced
        component.FieldsAutoplaced = node.GetChild("fields_autoplaced")?.GetBool() ?? false;

        // Parse lib_name
        component.LibName = node.GetChild("lib_name")?.GetString();

        // Parse instances
        component.InstancesRaw = node.GetChild("instances");

        // Parse properties
        var parameters = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            parameters.Add(SymLibReader.ParseProperty(propNode));
        }
        component.ParameterList.AddRange(parameters);

        // Parse pins from the placed symbol
        // Placed symbol pins have simplified format: (pin "name" (uuid "..."))
        var pins = new List<KiCadSchPin>();
        foreach (var pinNode in node.GetChildren("pin"))
        {
            var pin = new KiCadSchPin
            {
                Name = pinNode.GetString(0),
                Uuid = SExpressionHelper.ParseUuid(pinNode)
            };
            pins.Add(pin);
        }
        component.PinList.AddRange(pins);

        return component;
    }

    private static void ParseSchPolylineOrLine(SExpr node, List<KiCadSchLine> lines, List<KiCadSchPolyline> polylines)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color) = SExpressionHelper.ParseStroke(node);
        var uuid = SExpressionHelper.ParseUuid(node);

        if (pts.Count == 2)
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

    private static KiCadSchCircle ParseSchCircle(SExpr node)
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

    private static KiCadSchRectangle ParseSchRectangle(SExpr node)
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

    private static KiCadSchBezier ParseSchBezier(SExpr node)
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
}
