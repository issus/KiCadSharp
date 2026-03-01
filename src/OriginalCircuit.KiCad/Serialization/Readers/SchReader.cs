using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models;
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

        var genNode = root.GetChild("generator");
        var sch = new KiCadSch
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = genNode?.GetString(),
            GeneratorIsSymbol = genNode?.Values.FirstOrDefault() is SExprSymbol,
            GeneratorVersion = root.GetChild("generator_version")?.GetString(),
            EmbeddedFonts = root.GetChild("embedded_fonts") is { } ef ? ef.GetBool() : null,
            Uuid = SExpressionHelper.ParseUuid(root),
            Paper = root.GetChild("paper")?.GetString()
        };

        // Parse paper dimensions
        var paperNode = root.GetChild("paper");
        if (paperNode is not null)
        {
            // Check for custom dimensions: (paper 297 210)
            if (sch.Paper is null)
            {
                sch.PaperWidth = paperNode.GetDouble(0);
                sch.PaperHeight = paperNode.GetDouble(1);
            }
            // Check for portrait flag
            foreach (var v in paperNode.Values)
            {
                if (v is SExprSymbol sym && sym.Value == "portrait")
                    sch.PaperPortrait = true;
            }
        }

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
        // Note: KiCadSchLine is only used for API-created lines; the reader always
        // creates KiCadSchPolyline (even for 2-point polylines) to preserve type fidelity.
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
                        var wire = ParseWire(child);
                        wires.Add(wire);
                        sch.OrderedElementsList.Add(wire);
                        break;
                    case "junction":
                        var junc = ParseJunction(child);
                        junctions.Add(junc);
                        sch.OrderedElementsList.Add(junc);
                        break;
                    case "label":
                        var localLabel = ParseNetLabel(child, NetLabelType.Local);
                        netLabels.Add(localLabel);
                        sch.OrderedElementsList.Add(localLabel);
                        break;
                    case "global_label":
                        var globalLabel = ParseNetLabel(child, NetLabelType.Global);
                        netLabels.Add(globalLabel);
                        sch.OrderedElementsList.Add(globalLabel);
                        break;
                    case "hierarchical_label":
                        var hierLabel = ParseNetLabel(child, NetLabelType.Hierarchical);
                        netLabels.Add(hierLabel);
                        sch.OrderedElementsList.Add(hierLabel);
                        break;
                    case "text":
                        var textLabel = ParseTextLabel(child);
                        labels.Add(textLabel);
                        sch.OrderedElementsList.Add(textLabel);
                        break;
                    case "no_connect":
                        var nc = ParseNoConnect(child);
                        noConnects.Add(nc);
                        sch.OrderedElementsList.Add(nc);
                        break;
                    case "bus":
                        var bus = ParseBus(child);
                        buses.Add(bus);
                        sch.OrderedElementsList.Add(bus);
                        break;
                    case "bus_entry":
                        var busEntry = ParseBusEntry(child);
                        busEntries.Add(busEntry);
                        sch.OrderedElementsList.Add(busEntry);
                        break;
                    case "symbol":
                        var comp = ParsePlacedSymbol(child, diagnostics);
                        components.Add(comp);
                        sch.OrderedElementsList.Add(comp);
                        break;
                    case "sheet":
                        var sheet = ParseSheet(child);
                        sheets.Add(sheet);
                        sch.OrderedElementsList.Add(sheet);
                        break;
                    case "power_port":
                        var power = ParsePowerPort(child);
                        powerObjects.Add(power);
                        sch.OrderedElementsList.Add(power);
                        break;
                    case "polyline":
                        var polyline = ParseSchPolyline(child);
                        polylines.Add(polyline);
                        sch.OrderedElementsList.Add(polyline);
                        break;
                    case "circle":
                        var circle = ParseSchCircle(child);
                        circles.Add(circle);
                        sch.OrderedElementsList.Add(circle);
                        break;
                    case "rectangle":
                        var rect = ParseSchRectangle(child);
                        rectangles.Add(rect);
                        sch.OrderedElementsList.Add(rect);
                        break;
                    case "arc":
                        var arc = ParseSchArc(child);
                        arcs.Add(arc);
                        sch.OrderedElementsList.Add(arc);
                        break;
                    case "bezier":
                        var bez = ParseSchBezier(child);
                        beziers.Add(bez);
                        sch.OrderedElementsList.Add(bez);
                        break;
                    case "image":
                        var img = ParseSchImage(child);
                        sch.ImageList.Add(img);
                        sch.OrderedElementsList.Add(img);
                        break;
                    case "table":
                        var tbl = ParseSchTable(child);
                        sch.TableList.Add(tbl);
                        sch.OrderedElementsList.Add(tbl);
                        break;
                    case "rule_area":
                        var ra = ParseSchRuleArea(child);
                        sch.RuleAreaList.Add(ra);
                        sch.OrderedElementsList.Add(ra);
                        break;
                    case "netclass_flag":
                        var ncf = ParseSchNetclassFlag(child);
                        sch.NetclassFlagList.Add(ncf);
                        sch.OrderedElementsList.Add(ncf);
                        break;
                    case "bus_alias":
                        var ba = ParseSchBusAlias(child);
                        sch.BusAliasList.Add(ba);
                        sch.OrderedElementsList.Add(ba);
                        break;
                    case "text_box":
                    case "group":
                        break;
                    case "embedded_files":
                        sch.EmbeddedFiles = FootprintReader.ParseEmbeddedFiles(child);
                        break;
                    case "title_block":
                        sch.TitleBlock = PcbReader.ParseTitleBlock(child);
                        break;
                    case "version":
                    case "generator":
                    case "generator_version":
                    case "embedded_fonts":
                    case "uuid":
                    case "lib_symbols":
                    case "paper":
                        // Known tokens handled elsewhere
                        break;
                    case "sheet_instances":
                        ParseSheetInstances(child, sch);
                        break;
                    case "symbol_instances":
                        ParseSymbolInstances(child, sch);
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

        sch.CircleList.AddRange(circles);
        sch.RectangleList.AddRange(rectangles);
        sch.ArcList.AddRange(arcs);
        sch.BezierList.AddRange(beziers);
        sch.DiagnosticList.AddRange(diagnostics);

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
        var atNode = node.GetChild("at");
        var (fontH, fontW, justification, _, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _, boldIsSymbol, italicIsSymbol) = SExpressionHelper.ParseTextEffectsEx(node);

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
            PositionIncludesAngle = atNode is not null && atNode.Values.Count >= 3,
            Justification = justification,
            LabelType = labelType,
            FontSizeHeight = fontH,
            FontSizeWidth = fontW,
            IsBold = isBold,
            IsItalic = isItalic,
            BoldIsSymbol = boldIsSymbol,
            ItalicIsSymbol = italicIsSymbol,
            IsMirrored = isMirrored,
            Shape = shape,
            FieldsAutoplaced = fieldsAutoplaced,
            Properties = properties,
            FontFace = fontFace,
            FontThickness = fontThickness,
            FontColor = fontColor,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchLabel ParseTextLabel(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var atNode = node.GetChild("at");
        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _, boldIsSymbol, italicIsSymbol) = SExpressionHelper.ParseTextEffectsEx(node);

        var label = new KiCadSchLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Rotation = angle,
            PositionIncludesAngle = atNode is not null && atNode.Values.Count >= 3,
            Justification = justification,
            IsHidden = isHidden,
            IsMirrored = isMirrored,
            FontSizeHeight = fontH,
            FontSizeWidth = fontW,
            IsBold = isBold,
            IsItalic = isItalic,
            BoldIsSymbol = boldIsSymbol,
            ItalicIsSymbol = italicIsSymbol,
            FontFace = fontFace,
            FontThickness = fontThickness,
            FontColor = fontColor,
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        // Parse exclude_from_sim (KiCad 9+)
        var excludeFromSimNode = node.GetChild("exclude_from_sim");
        if (excludeFromSimNode is not null)
        {
            label.ExcludeFromSimPresent = true;
            label.ExcludeFromSim = excludeFromSimNode.GetBool() ?? false;
        }

        // Parse href from effects (KiCad 9+)
        var effectsNode = node.GetChild("effects");
        if (effectsNode is not null)
        {
            var hrefNode = effectsNode.GetChild("href");
            if (hrefNode is not null)
                label.Href = hrefNode.GetString();
        }

        return label;
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

        // Detect color-only fill format (KiCad 9+: fill has color but no type)
        var fillNode = node.GetChild("fill");
        var fillColorOnly = fillNode is not null && fillNode.GetChild("color") is not null && fillNode.GetChild("type") is null;

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

        var sheet = new KiCadSchSheet
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
            FillColorOnly = fillColorOnly,
            FieldsAutoplaced = fieldsAutoplaced,
            SheetProperties = sheetProperties,
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        // Parse KiCad 9+ flags
        var excludeFromSimNode = node.GetChild("exclude_from_sim");
        if (excludeFromSimNode is not null)
        {
            sheet.ExcludeFromSimPresent = true;
            sheet.ExcludeFromSim = excludeFromSimNode.GetBool() ?? false;
        }

        var inBomNode = node.GetChild("in_bom");
        if (inBomNode is not null)
        {
            sheet.InBomPresent = true;
            sheet.InBom = inBomNode.GetBool() ?? true;
        }

        var onBoardNode = node.GetChild("on_board");
        if (onBoardNode is not null)
        {
            sheet.OnBoardPresent = true;
            sheet.OnBoard = onBoardNode.GetBool() ?? true;
        }

        var dnpNode = node.GetChild("dnp");
        if (dnpNode is not null)
        {
            sheet.DnpPresent = true;
            sheet.Dnp = dnpNode.GetBool() ?? false;
        }

        // Parse instances (KiCad 9+)
        var instancesNode = node.GetChild("instances");
        if (instancesNode is not null)
        {
            foreach (var projNode in instancesNode.GetChildren("project"))
            {
                var projectName = projNode.GetString() ?? "";
                foreach (var pathNode in projNode.GetChildren("path"))
                {
                    var entry = new KiCadSchSheetInstanceEntry
                    {
                        ProjectName = projectName,
                        Path = pathNode.GetString() ?? "",
                        Page = pathNode.GetChild("page")?.GetString() ?? ""
                    };
                    sheet.Instances.Add(entry);
                }
            }
        }

        return sheet;
    }

    private static KiCadSchSheetPin ParseSheetPin(SExpr node)
    {
        var name = node.GetString(0) ?? "";
        var ioTypeStr = node.GetString(1) ?? "bidirectional";
        var ioType = SExpressionHelper.StringToSheetPinIoType(ioTypeStr);

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var side = SExpressionHelper.AngleToSheetPinSide(angle);

        var pin = new KiCadSchSheetPin
        {
            Name = name,
            IoType = ioType,
            Side = side,
            Location = loc,
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        // Parse text effects (font size, justification, bold, italic, color, face, thickness, mirror)
        var effectsNode = node.GetChild("effects");
        if (effectsNode is not null)
        {
            var (fontH, fontW, justification, _, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _, boldIsSymbol, italicIsSymbol) =
                SExpressionHelper.ParseTextEffectsEx(node);
            pin.FontSizeHeight = fontH;
            pin.FontSizeWidth = fontW;
            pin.Justification = justification;
            pin.IsBold = isBold;
            pin.IsItalic = isItalic;
            pin.BoldIsSymbol = boldIsSymbol;
            pin.ItalicIsSymbol = italicIsSymbol;
            pin.FontColor = fontColor;
            pin.FontFace = fontFace;
            pin.FontThickness = fontThickness;
            pin.IsMirrored = isMirrored;
        }

        return pin;
    }

    private static KiCadSchComponent ParsePlacedSymbol(SExpr node, List<KiCadDiagnostic> diagnostics)
    {
        var component = new KiCadSchComponent
        {
            Name = node.GetChild("lib_id")?.GetString() ?? node.GetString() ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var atNode = node.GetChild("at");
        component.Location = loc;
        component.Rotation = angle;
        component.PositionIncludesAngle = atNode is not null && atNode.Values.Count >= 3;
        component.Uuid = SExpressionHelper.ParseUuid(node);

        // Parse mirror - support "x", "y", and "xy"
        var mirror = node.GetChild("mirror");
        if (mirror is not null)
        {
            component.MirrorPresent = true;
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

        // Parse exclude_from_sim for placed symbols
        var excludeFromSimNode = node.GetChild("exclude_from_sim");
        if (excludeFromSimNode is not null)
        {
            component.ExcludeFromSimPresent = true;
            component.ExcludeFromSim = excludeFromSimNode.GetBool() ?? false;
        }

        // Parse in_bom / on_board for placed symbols
        var inBomNode = node.GetChild("in_bom");
        if (inBomNode is not null)
            component.InBom = inBomNode.GetBool() ?? true;

        var onBoardNode = node.GetChild("on_board");
        if (onBoardNode is not null)
            component.OnBoard = onBoardNode.GetBool() ?? true;

        // Parse dnp for placed symbols
        var dnpNode = node.GetChild("dnp");
        if (dnpNode is not null)
        {
            component.DnpPresent = true;
            component.Dnp = dnpNode.GetBool() ?? false;
        }

        // Parse convert / body_style
        var convertNode = node.GetChild("convert");
        if (convertNode is not null)
            component.BodyStyle = convertNode.GetInt() ?? 0;
        var bodyStyleNode = node.GetChild("body_style");
        if (bodyStyleNode is not null)
        {
            component.BodyStyle = bodyStyleNode.GetInt() ?? 0;
            component.UseBodyStyleToken = true;
        }

        // Parse fields_autoplaced
        component.FieldsAutoplaced = node.GetChild("fields_autoplaced")?.GetBool() ?? false;

        // Parse lib_name
        component.LibName = node.GetChild("lib_name")?.GetString();

        // Parse instances (KiCad 9+ format)
        var instancesNode = node.GetChild("instances");
        if (instancesNode is not null)
        {
            foreach (var projNode in instancesNode.GetChildren("project"))
            {
                var projectName = projNode.GetString() ?? "";
                foreach (var pathNode in projNode.GetChildren("path"))
                {
                    var inst = new KiCadSchComponentInstance
                    {
                        ProjectName = projectName,
                        Path = pathNode.GetString() ?? "",
                        Reference = pathNode.GetChild("reference")?.GetString() ?? "",
                        Unit = pathNode.GetChild("unit")?.GetInt() ?? 1
                    };
                    component.InstanceList.Add(inst);
                }
            }
        }

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

    private static KiCadSchPolyline ParseSchPolyline(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color) = SExpressionHelper.ParseStroke(node);
        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        var fillNode = node.GetChild("fill");

        var poly = new KiCadSchPolyline
        {
            Vertices = pts,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasFill = fillNode is not null,
            Uuid = uuid,
            UuidIsSymbol = uuidIsSymbol
        };

        if (fillNode is not null)
        {
            var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);
            poly.FillType = fillType;
            poly.FillColor = fillColor;
        }

        return poly;
    }

    private static KiCadSchCircle ParseSchCircle(SExpr node)
    {
        var centerNode = node.GetChild("center");
        var center = centerNode is not null ? SExpressionHelper.ParseXY(centerNode) : CoordPoint.Zero;
        var radius = Coord.FromMm(node.GetChild("radius")?.GetDouble() ?? 0);
        var (width, lineStyle, color, hasStrokeColor) = SExpressionHelper.ParseStrokeEx(node);
        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        var fillNode = node.GetChild("fill");

        var circle = new KiCadSchCircle
        {
            Center = center,
            Radius = radius,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasStrokeColor,
            Uuid = uuid,
            UuidIsSymbol = uuidIsSymbol,
            HasFill = fillNode is not null
        };

        if (fillNode is not null)
        {
            var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);
            circle.FillType = fillType;
            circle.IsFilled = isFilled;
            circle.FillColor = fillColor;
        }

        return circle;
    }

    private static KiCadSchRectangle ParseSchRectangle(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, lineStyle, color, hasStrokeColor) = SExpressionHelper.ParseStrokeEx(node);
        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        var fillNode = node.GetChild("fill");

        var rect = new KiCadSchRectangle
        {
            Corner1 = start,
            Corner2 = end,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasStrokeColor,
            Uuid = uuid,
            UuidIsSymbol = uuidIsSymbol,
            HasFill = fillNode is not null
        };

        if (fillNode is not null)
        {
            var (fillType, isFilled, fillColor) = SExpressionHelper.ParseFill(node);
            rect.FillType = fillType;
            rect.IsFilled = isFilled;
            rect.FillColor = fillColor;
        }

        return rect;
    }

    private static KiCadSchArc ParseSchArc(SExpr node)
    {
        var startNode = node.GetChild("start");
        var midNode = node.GetChild("mid");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var mid = midNode is not null ? SExpressionHelper.ParseXY(midNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, lineStyle, color, hasStrokeColor) = SExpressionHelper.ParseStrokeEx(node);
        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        var fillNode = node.GetChild("fill");

        var (center, radius, startAngle, endAngle) = SExpressionHelper.ComputeArcFromThreePoints(start, mid, end);

        var arc = new KiCadSchArc
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasStrokeColor,
            ArcStart = start,
            ArcMid = mid,
            ArcEnd = end,
            Uuid = uuid,
            UuidIsSymbol = uuidIsSymbol,
            HasFill = fillNode is not null
        };

        if (fillNode is not null)
        {
            var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);
            arc.FillType = fillType;
            arc.FillColor = fillColor;
        }

        return arc;
    }

    private static KiCadSchBezier ParseSchBezier(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, lineStyle, color, hasStrokeColor) = SExpressionHelper.ParseStrokeEx(node);
        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        var fillNode = node.GetChild("fill");

        var bezier = new KiCadSchBezier
        {
            ControlPoints = pts,
            Color = color,
            LineWidth = width,
            LineStyle = lineStyle,
            HasStrokeColor = hasStrokeColor,
            Uuid = uuid,
            UuidIsSymbol = uuidIsSymbol,
            HasFill = fillNode is not null
        };

        if (fillNode is not null)
        {
            var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);
            bezier.FillType = fillType;
            bezier.FillColor = fillColor;
        }

        return bezier;
    }

    private static void ParseSheetInstances(SExpr node, KiCadSch sch)
    {
        foreach (var pathNode in node.GetChildren("path"))
        {
            var inst = new KiCadSchSheetInstance
            {
                Path = pathNode.GetString() ?? "",
                Page = pathNode.GetChild("page")?.GetString() ?? ""
            };
            sch.SheetInstanceList.Add(inst);
        }
    }

    private static void ParseSymbolInstances(SExpr node, KiCadSch sch)
    {
        foreach (var pathNode in node.GetChildren("path"))
        {
            var inst = new KiCadSchSymbolInstance
            {
                Path = pathNode.GetString() ?? "",
                Reference = pathNode.GetChild("reference")?.GetString() ?? "",
                Unit = pathNode.GetChild("unit")?.GetInt() ?? 1,
                Value = pathNode.GetChild("value")?.GetString() ?? "",
                Footprint = pathNode.GetChild("footprint")?.GetString() ?? ""
            };
            sch.SymbolInstanceList.Add(inst);
        }
    }

    private static KiCadSchImage ParseSchImage(SExpr node)
    {
        var img = new KiCadSchImage();
        var atNode = node.GetChild("at");
        if (atNode is not null)
        {
            var loc = SExpressionHelper.ParseXY(atNode);
            img.Corner1 = loc;
            img.Corner2 = loc;
        }
        img.Scale = node.GetChild("scale")?.GetDouble() ?? 1.0;
        img.Uuid = SExpressionHelper.ParseUuid(node);

        var dataNode = node.GetChild("data");
        if (dataNode is not null)
        {
            // Collect all string values as the base64 data
            var parts = new List<string>();
            foreach (var v in dataNode.Values)
            {
                if (v is SExprString s) parts.Add(s.Value);
                else if (v is SExprSymbol sym) parts.Add(sym.Value);
            }
            img.DataString = string.Join("\n", parts);
            try
            {
                img.ImageData = Convert.FromBase64String(string.Concat(parts));
            }
            catch (FormatException)
            {
                // Not valid base64 — leave ImageData null
            }
        }

        return img;
    }

    private static KiCadSchTable ParseSchTable(SExpr node)
    {
        var table = new KiCadSchTable
        {
            ColumnCount = node.GetChild("column_count")?.GetInt() ?? 0
        };

        var borderNode = node.GetChild("border");
        if (borderNode is not null)
        {
            table.BorderExternal = borderNode.GetChild("external")?.GetBool() ?? false;
            table.BorderHeader = borderNode.GetChild("header")?.GetBool() ?? false;
            var strokeNode = borderNode.GetChild("stroke");
            if (strokeNode is not null)
            {
                table.BorderStrokeWidth = Coord.FromMm(strokeNode.GetChild("width")?.GetDouble() ?? 0);
                table.BorderStrokeType = strokeNode.GetChild("type")?.GetString();
            }
        }

        var sepNode = node.GetChild("separators");
        if (sepNode is not null)
        {
            table.SeparatorRows = sepNode.GetChild("rows")?.GetBool() ?? false;
            table.SeparatorCols = sepNode.GetChild("cols")?.GetBool() ?? false;
            var strokeNode = sepNode.GetChild("stroke");
            if (strokeNode is not null)
            {
                table.SeparatorStrokeWidth = Coord.FromMm(strokeNode.GetChild("width")?.GetDouble() ?? 0);
                table.SeparatorStrokeType = strokeNode.GetChild("type")?.GetString();
            }
        }

        var cwNode = node.GetChild("column_widths");
        if (cwNode is not null)
        {
            foreach (var v in cwNode.Values)
            {
                if (v is SExprNumber n) table.ColumnWidths.Add(n.Value);
                else if (v is SExprString s && double.TryParse(s.Value, System.Globalization.CultureInfo.InvariantCulture, out var d)) table.ColumnWidths.Add(d);
            }
        }

        var rhNode = node.GetChild("row_heights");
        if (rhNode is not null)
        {
            foreach (var v in rhNode.Values)
            {
                if (v is SExprNumber n) table.RowHeights.Add(n.Value);
                else if (v is SExprString s && double.TryParse(s.Value, System.Globalization.CultureInfo.InvariantCulture, out var d)) table.RowHeights.Add(d);
            }
        }

        var cellsNode = node.GetChild("cells");
        if (cellsNode is not null)
        {
            foreach (var cellNode in cellsNode.GetChildren("table_cell"))
            {
                var cell = new KiCadSchTableCell
                {
                    Text = cellNode.GetString() ?? ""
                };

                var efsNode = cellNode.GetChild("exclude_from_sim");
                if (efsNode is not null)
                {
                    cell.HasExcludeFromSim = true;
                    cell.ExcludeFromSim = efsNode.GetBool() ?? false;
                }

                var catNode = cellNode.GetChild("at");
                if (catNode is not null)
                {
                    cell.Location = SExpressionHelper.ParseXY(catNode);
                    cell.Rotation = catNode.GetDouble(2) ?? 0;
                }

                var sizeNode = cellNode.GetChild("size");
                if (sizeNode is not null)
                    cell.Size = new CoordPoint(Coord.FromMm(sizeNode.GetDouble(0) ?? 0), Coord.FromMm(sizeNode.GetDouble(1) ?? 0));

                var marginsNode = cellNode.GetChild("margins");
                if (marginsNode is not null)
                {
                    cell.MarginLeft = marginsNode.GetDouble(0) ?? 0;
                    cell.MarginRight = marginsNode.GetDouble(1) ?? 0;
                    cell.MarginTop = marginsNode.GetDouble(2) ?? 0;
                    cell.MarginBottom = marginsNode.GetDouble(3) ?? 0;
                }

                var spanNode = cellNode.GetChild("span");
                if (spanNode is not null)
                {
                    cell.ColSpan = spanNode.GetInt(0) ?? 1;
                    cell.RowSpan = spanNode.GetInt(1) ?? 1;
                }

                var fillNode = cellNode.GetChild("fill");
                if (fillNode is not null)
                    cell.FillType = fillNode.GetChild("type")?.GetString();

                var effectsNode = cellNode.GetChild("effects");
                if (effectsNode is not null)
                {
                    var fontNode = effectsNode.GetChild("font");
                    if (fontNode is not null)
                    {
                        var fsNode = fontNode.GetChild("size");
                        if (fsNode is not null)
                        {
                            cell.FontHeight = Coord.FromMm(fsNode.GetDouble(0) ?? 1.27);
                            cell.FontWidth = Coord.FromMm(fsNode.GetDouble(1) ?? 1.27);
                        }
                    }
                    var justNode = effectsNode.GetChild("justify");
                    if (justNode is not null)
                    {
                        foreach (var v in justNode.Values)
                        {
                            if (v is SExprSymbol sym) cell.Justification.Add(sym.Value);
                            else if (v is SExprString s) cell.Justification.Add(s.Value);
                        }
                    }
                }

                cell.Uuid = SExpressionHelper.ParseUuid(cellNode);
                table.Cells.Add(cell);
            }
        }

        return table;
    }

    private static KiCadSchRuleArea ParseSchRuleArea(SExpr node)
    {
        var ra = new KiCadSchRuleArea();
        var polyNode = node.GetChild("polyline");
        if (polyNode is not null)
        {
            ra.Points = SExpressionHelper.ParsePoints(polyNode);
            var strokeNode = polyNode.GetChild("stroke");
            if (strokeNode is not null)
            {
                ra.StrokeWidth = Coord.FromMm(strokeNode.GetChild("width")?.GetDouble() ?? 0);
                ra.StrokeType = strokeNode.GetChild("type")?.GetString();
            }
            var fillNode = polyNode.GetChild("fill");
            if (fillNode is not null)
                ra.FillType = fillNode.GetChild("type")?.GetString();
            var (uuid, isSymbol) = SExpressionHelper.ParseUuidEx(polyNode);
            ra.Uuid = uuid;
            ra.UuidIsSymbol = isSymbol;
        }
        return ra;
    }

    private static KiCadSchNetclassFlag ParseSchNetclassFlag(SExpr node)
    {
        var ncf = new KiCadSchNetclassFlag
        {
            Name = node.GetString() ?? "",
            Length = node.GetChild("length")?.GetDouble() ?? 0,
            Shape = node.GetChild("shape")?.GetString()
        };

        var atNode = node.GetChild("at");
        if (atNode is not null)
        {
            ncf.Location = SExpressionHelper.ParseXY(atNode);
            ncf.Rotation = atNode.GetDouble(2) ?? 0;
        }

        var effectsNode = node.GetChild("effects");
        if (effectsNode is not null)
        {
            var fontNode = effectsNode.GetChild("font");
            if (fontNode is not null)
            {
                var fsNode = fontNode.GetChild("size");
                if (fsNode is not null)
                {
                    ncf.FontHeight = Coord.FromMm(fsNode.GetDouble(0) ?? 1.27);
                    ncf.FontWidth = Coord.FromMm(fsNode.GetDouble(1) ?? 1.27);
                }
            }
            var justNode = effectsNode.GetChild("justify");
            if (justNode is not null)
            {
                foreach (var v in justNode.Values)
                {
                    if (v is SExprSymbol sym) ncf.Justification.Add(sym.Value);
                    else if (v is SExprString s) ncf.Justification.Add(s.Value);
                }
            }
        }

        ncf.Uuid = SExpressionHelper.ParseUuid(node);

        foreach (var propNode in node.GetChildren("property"))
        {
            var prop = new KiCadSchNetclassFlagProperty
            {
                Key = propNode.GetString(0) ?? "",
                Value = propNode.GetString(1) ?? ""
            };
            var propAt = propNode.GetChild("at");
            if (propAt is not null)
            {
                prop.Location = SExpressionHelper.ParseXY(propAt);
                prop.Rotation = propAt.GetDouble(2) ?? 0;
            }
            var propEffects = propNode.GetChild("effects");
            if (propEffects is not null)
            {
                var fontNode = propEffects.GetChild("font");
                if (fontNode is not null)
                {
                    var fsNode = fontNode.GetChild("size");
                    if (fsNode is not null)
                    {
                        prop.FontHeight = Coord.FromMm(fsNode.GetDouble(0) ?? 1.27);
                        prop.FontWidth = Coord.FromMm(fsNode.GetDouble(1) ?? 1.27);
                    }
                    prop.FontItalic = SExpressionHelper.HasSymbol(fontNode, "italic")
                        || fontNode.GetChild("italic")?.GetBool() == true;
                }
                var justNode = propEffects.GetChild("justify");
                if (justNode is not null)
                {
                    foreach (var v in justNode.Values)
                    {
                        if (v is SExprSymbol sym) prop.Justification.Add(sym.Value);
                        else if (v is SExprString s) prop.Justification.Add(s.Value);
                    }
                }
                prop.IsHidden = SExpressionHelper.HasSymbol(propEffects, "hide")
                    || propEffects.GetChild("hide")?.GetBool() == true;
            }
            prop.Uuid = SExpressionHelper.ParseUuid(propNode);
            ncf.Properties.Add(prop);
        }

        return ncf;
    }

    private static KiCadSchBusAlias ParseSchBusAlias(SExpr node)
    {
        var ba = new KiCadSchBusAlias
        {
            Name = node.GetString() ?? ""
        };
        var membersNode = node.GetChild("members");
        if (membersNode is not null)
        {
            foreach (var v in membersNode.Values)
            {
                if (v is SExprString s) ba.Members.Add(s.Value);
                else if (v is SExprSymbol sym) ba.Members.Add(sym.Value);
            }
        }
        return ba;
    }
}
