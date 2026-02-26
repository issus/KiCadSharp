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
            Uuid = SExpressionHelper.ParseUuid(root)
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
                        netLabels.Add(ParseNetLabel(child));
                        break;
                    case "global_label":
                        netLabels.Add(ParseNetLabel(child));
                        break;
                    case "hierarchical_label":
                        netLabels.Add(ParseNetLabel(child));
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
        sch.DiagnosticList.AddRange(diagnostics);

        return sch;
    }

    private static KiCadSchWire ParseWire(SExpr node)
    {
        var pts = SExpressionHelper.ParsePoints(node);
        var (width, _, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchWire
        {
            Vertices = pts,
            LineWidth = width,
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

    private static KiCadSchNetLabel ParseNetLabel(SExpr node)
    {
        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var (_, _, justification, _, _, _, _) = SExpressionHelper.ParseTextEffects(node);

        return new KiCadSchNetLabel
        {
            Text = node.GetString() ?? "",
            Location = loc,
            Orientation = (int)angle,
            Justification = justification,
            Uuid = SExpressionHelper.ParseUuid(node)
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
        var (width, _, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchBus
        {
            Vertices = pts,
            LineWidth = width,
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
        var (width, _, color) = SExpressionHelper.ParseStroke(node);

        return new KiCadSchBusEntry
        {
            Location = loc,
            Corner = new CoordPoint(loc.X + sx, loc.Y + sy),
            LineWidth = width,
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
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchSheet ParseSheet(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);
        var sizeNode = node.GetChild("size");
        var w = Coord.FromMm(sizeNode?.GetDouble(0) ?? 0);
        var h = Coord.FromMm(sizeNode?.GetDouble(1) ?? 0);
        var (strokeWidth, _, strokeColor) = SExpressionHelper.ParseStroke(node);
        var (_, _, fillColor) = SExpressionHelper.ParseFill(node);

        var sheetName = "";
        var fileName = "";
        foreach (var propNode in node.GetChildren("property"))
        {
            var key = propNode.GetString(0);
            var val = propNode.GetString(1) ?? "";
            if (key == "Sheetname" || key == "Sheet name") sheetName = val;
            else if (key == "Sheetfile" || key == "Sheet file") fileName = val;
        }

        var pins = new List<KiCadSchSheetPin>();
        foreach (var pinNode in node.GetChildren("pin"))
        {
            pins.Add(ParseSheetPin(pinNode));
        }

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
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadSchSheetPin ParseSheetPin(SExpr node)
    {
        var name = node.GetString(0) ?? "";
        var ioTypeStr = node.GetString(1) ?? "bidirectional";
        var ioType = ioTypeStr switch
        {
            "input" => 0,
            "output" => 1,
            "bidirectional" => 2,
            "tri_state" => 3,
            "passive" => 4,
            _ => 2
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        var side = ((int)angle % 360) switch
        {
            0 => 1,    // right
            90 => 2,   // top
            180 => 0,  // left
            270 => 3,  // bottom
            _ => 0
        };

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

        // Parse mirror
        var mirror = node.GetChild("mirror");
        if (mirror is not null)
        {
            var mirrorVal = mirror.GetString();
            component.IsMirroredX = mirrorVal == "x";
            component.IsMirroredY = mirrorVal == "y";
        }

        // Parse unit
        var unitNode = node.GetChild("unit");
        if (unitNode is not null)
            component.Unit = unitNode.GetInt() ?? 1;

        // Parse properties
        var parameters = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            parameters.Add(SymLibReader.ParseProperty(propNode));
        }
        component.ParameterList.AddRange(parameters);

        // Parse pins from the placed symbol
        var pins = new List<KiCadSchPin>();
        foreach (var pinNode in node.GetChildren("pin"))
        {
            pins.Add(SymLibReader.ParsePin(pinNode));
        }
        component.PinList.AddRange(pins);

        return component;
    }
}
