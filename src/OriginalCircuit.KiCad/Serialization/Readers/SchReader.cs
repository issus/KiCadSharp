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
        var root = await SExpressionReader.ReadAsync(path, ct).ConfigureAwait(false);
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
        var root = await SExpressionReader.ReadAsync(stream, ct).ConfigureAwait(false);
        return Parse(root);
    }

    private static KiCadSch Parse(SExpr root)
    {
        if (root.Token != "kicad_sch")
            throw new KiCadFileException($"Expected 'kicad_sch' root token, got '{root.Token}'.");

        var sch = new KiCadSch
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = root.GetChild("generator")?.GetString(),
            GeneratorVersion = root.GetChild("generator_version")?.GetString(),
            Uuid = SExpressionHelper.ParseUuid(root)
        };

        var diagnostics = new List<KiCadDiagnostic>();
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
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                    $"Failed to parse {child.Token}: {ex.Message}", child.GetString()));
            }
        }

        sch.Wires = wires;
        sch.Junctions = junctions;
        sch.NetLabels = netLabels;
        sch.Labels = labels;
        sch.NoConnects = noConnects;
        sch.Buses = buses;
        sch.BusEntries = busEntries;
        sch.PowerObjects = powerObjects;
        sch.Components = components;
        sch.Sheets = sheets;
        sch.LibSymbols = libSymbols;
        sch.Diagnostics = diagnostics;

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

        var (loc, _) = SExpressionHelper.ParsePosition(node);

        // Parse properties
        var parameters = new List<KiCadSchParameter>();
        foreach (var propNode in node.GetChildren("property"))
        {
            parameters.Add(SymLibReader.ParseProperty(propNode));
        }
        component.Parameters = parameters;

        // Parse pins from the placed symbol
        var pins = new List<KiCadSchPin>();
        foreach (var pinNode in node.GetChildren("pin"))
        {
            pins.Add(SymLibReader.ParsePin(pinNode));
        }
        component.Pins = pins;

        return component;
    }
}
