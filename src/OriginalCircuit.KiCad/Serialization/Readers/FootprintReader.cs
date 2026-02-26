using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Reads KiCad footprint files (<c>.kicad_mod</c>) into <see cref="KiCadPcbComponent"/> objects.
/// </summary>
public static class FootprintReader
{
    /// <summary>
    /// Reads a footprint from a file path.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed footprint component.</returns>
    public static async ValueTask<KiCadPcbComponent> ReadAsync(string path, CancellationToken ct = default)
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
        return ParseFootprint(root);
    }

    /// <summary>
    /// Reads a footprint from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed footprint component.</returns>
    public static async ValueTask<KiCadPcbComponent> ReadAsync(Stream stream, CancellationToken ct = default)
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
        return ParseFootprint(root);
    }

    internal static KiCadPcbComponent ParseFootprint(SExpr node)
    {
        var token = node.Token;
        if (token != "footprint" && token != "module")
            throw new KiCadFileException($"Expected 'footprint' or 'module' token, got '{token}'.");

        var component = new KiCadPcbComponent
        {
            Name = node.GetString() ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        component.Location = loc;
        component.Rotation = angle;

        component.LayerName = node.GetChild("layer")?.GetString();
        component.Description = node.GetChild("descr")?.GetString();
        component.Tags = node.GetChild("tags")?.GetString();
        component.Path = node.GetChild("path")?.GetString();
        component.Uuid = SExpressionHelper.ParseUuid(node);

        // Parse attr
        var attrNode = node.GetChild("attr");
        if (attrNode is not null)
        {
            var attrs = FootprintAttribute.None;
            foreach (var v in attrNode.Values)
            {
                if (v is SExprSymbol s)
                {
                    switch (s.Value)
                    {
                        case "smd": attrs |= FootprintAttribute.Smd; break;
                        case "through_hole": attrs |= FootprintAttribute.ThroughHole; break;
                        case "board_only": attrs |= FootprintAttribute.BoardOnly; break;
                        case "exclude_from_pos_files": attrs |= FootprintAttribute.ExcludeFromPosFiles; break;
                        case "exclude_from_bom": attrs |= FootprintAttribute.ExcludeFromBom; break;
                    }
                }
            }
            component.Attributes = attrs;
        }

        // Parse locked
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "locked")
            {
                component.IsLocked = true;
                break;
            }
        }

        // Parse clearance, solder mask margin, etc.
        component.Clearance = Coord.FromMm(node.GetChild("clearance")?.GetDouble() ?? 0);
        component.SolderMaskMargin = Coord.FromMm(node.GetChild("solder_mask_margin")?.GetDouble() ?? 0);
        component.SolderPasteMargin = Coord.FromMm(node.GetChild("solder_paste_margin")?.GetDouble() ?? 0);
        component.SolderPasteRatio = node.GetChild("solder_paste_ratio")?.GetDouble() ?? 0;
        component.ThermalWidth = Coord.FromMm(node.GetChild("thermal_width")?.GetDouble() ?? 0);
        component.ThermalGap = Coord.FromMm(node.GetChild("thermal_gap")?.GetDouble() ?? 0);

        // Parse zone_connect
        var zcNode = node.GetChild("zone_connect");
        if (zcNode is not null)
        {
            component.ZoneConnect = (ZoneConnectionType)(zcNode.GetInt() ?? 0);
        }

        var diagnostics = new List<KiCadDiagnostic>();
        var pads = new List<KiCadPcbPad>();
        var texts = new List<KiCadPcbText>();
        var tracks = new List<KiCadPcbTrack>();
        var arcs = new List<KiCadPcbArc>();
        var properties = new List<KiCadSchParameter>();

        foreach (var child in node.Children)
        {
            try
            {
                switch (child.Token)
                {
                    case "pad":
                        pads.Add(ParsePad(child));
                        break;
                    case "fp_text":
                        texts.Add(ParseFpText(child));
                        break;
                    case "fp_line":
                        tracks.Add(ParseFpLine(child));
                        break;
                    case "fp_arc":
                        arcs.Add(ParseFpArc(child));
                        break;
                    case "fp_circle":
                        // Treat as an arc for simplicity
                        break;
                    case "fp_rect":
                        // Could add as multiple tracks
                        break;
                    case "model":
                        component.Model3D = child.GetString();
                        break;
                    case "property":
                        properties.Add(SymLibReader.ParseProperty(child));
                        break;
                    case "fp_poly":
                        diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                            "Footprint polygon (fp_poly) parsing not yet implemented", child.Token));
                        break;
                    case "layer":
                    case "descr":
                    case "tags":
                    case "path":
                    case "uuid":
                    case "tstamp":
                    case "at":
                    case "attr":
                    case "clearance":
                    case "solder_mask_margin":
                    case "solder_paste_margin":
                    case "solder_paste_ratio":
                    case "thermal_width":
                    case "thermal_gap":
                    case "zone_connect":
                    case "fp_text_private":
                        // Known tokens handled elsewhere or intentionally skipped
                        break;
                    default:
                        diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                            $"Unknown footprint token '{child.Token}' was ignored", child.Token));
                        break;
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Error,
                    $"Failed to parse {child.Token}: {ex.Message}", child.GetString()));
            }
        }

        component.PadList.AddRange(pads);
        component.TextList.AddRange(texts);
        component.TrackList.AddRange(tracks);
        component.ArcList.AddRange(arcs);
        component.PropertyList.AddRange(properties);
        component.DiagnosticList.AddRange(diagnostics);

        return component;
    }

    internal static KiCadPcbPad ParsePad(SExpr node)
    {
        var pad = new KiCadPcbPad
        {
            Designator = node.GetString(0),
            PadType = SExpressionHelper.ParsePadType(node.GetString(1)),
            Shape = SExpressionHelper.ParsePadShape(node.GetString(2))
        };

        pad.IsPlated = pad.PadType != PadType.NpThruHole;

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        pad.Location = loc;
        pad.Rotation = angle;

        var sizeNode = node.GetChild("size");
        if (sizeNode is not null)
        {
            pad.Size = new CoordPoint(
                Coord.FromMm(sizeNode.GetDouble(0) ?? 0),
                Coord.FromMm(sizeNode.GetDouble(1) ?? 0));
        }

        var drillNode = node.GetChild("drill");
        if (drillNode is not null)
        {
            // Check for oval drill
            var firstVal = drillNode.GetString(0);
            if (firstVal == "oval")
            {
                pad.HoleType = PadHoleType.Slot;
                pad.HoleSize = Coord.FromMm(drillNode.GetDouble(1) ?? 0);
            }
            else
            {
                pad.HoleSize = Coord.FromMm(drillNode.GetDouble(0) ?? 0);
            }
        }

        var layersNode = node.GetChild("layers");
        if (layersNode is not null)
        {
            var layers = new List<string>();
            foreach (var v in layersNode.Values)
            {
                if (v is SExprString s) layers.Add(s.Value);
                else if (v is SExprSymbol sym) layers.Add(sym.Value);
            }
            pad.Layers = layers;
        }

        // Net
        var netNode = node.GetChild("net");
        if (netNode is not null)
        {
            pad.Net = netNode.GetInt(0) ?? 0;
            pad.NetName = netNode.GetString(1);
        }

        // Corner radius
        pad.CornerRadiusPercentage = (int)((node.GetChild("roundrect_rratio")?.GetDouble() ?? 0) * 100);

        // Clearances
        pad.SolderMaskExpansion = Coord.FromMm(node.GetChild("solder_mask_margin")?.GetDouble() ?? 0);
        pad.SolderPasteMargin = Coord.FromMm(node.GetChild("solder_paste_margin")?.GetDouble() ?? 0);
        pad.SolderPasteRatio = node.GetChild("solder_paste_margin_ratio")?.GetDouble() ?? 0;
        pad.Clearance = Coord.FromMm(node.GetChild("clearance")?.GetDouble() ?? 0);
        pad.ThermalWidth = Coord.FromMm(node.GetChild("thermal_width")?.GetDouble() ?? 0);
        pad.ThermalGap = Coord.FromMm(node.GetChild("thermal_gap")?.GetDouble() ?? 0);

        // Zone connect
        var zcNode = node.GetChild("zone_connect");
        if (zcNode is not null)
        {
            pad.ZoneConnect = (ZoneConnectionType)(zcNode.GetInt() ?? 0);
        }

        pad.PinFunction = node.GetChild("pinfunction")?.GetString();
        pad.PinType = node.GetChild("pintype")?.GetString();
        pad.DieLength = Coord.FromMm(node.GetChild("die_length")?.GetDouble() ?? 0);

        pad.Uuid = SExpressionHelper.ParseUuid(node);

        return pad;
    }

    private static KiCadPcbText ParseFpText(SExpr node)
    {
        var text = new KiCadPcbText
        {
            TextType = node.GetString(0),
            Text = node.GetString(1) ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        text.Location = loc;
        text.Rotation = angle;

        text.LayerName = node.GetChild("layer")?.GetString();

        // Check for hide
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "hide")
            {
                text.IsHidden = true;
                break;
            }
        }

        var (fontH, _, _, _, _, isBold, isItalic) = SExpressionHelper.ParseTextEffects(node);
        text.Height = fontH;
        text.FontBold = isBold;
        text.FontItalic = isItalic;

        text.Uuid = SExpressionHelper.ParseUuid(node);

        return text;
    }

    private static KiCadPcbTrack ParseFpLine(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, _, _) = SExpressionHelper.ParseStroke(node);

        // If no stroke, try legacy width
        if (width == Coord.Zero)
        {
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);
        }

        return new KiCadPcbTrack
        {
            Start = start,
            End = end,
            Width = width,
            LayerName = node.GetChild("layer")?.GetString(),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcbArc ParseFpArc(SExpr node)
    {
        var startNode = node.GetChild("start");
        var midNode = node.GetChild("mid");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var mid = midNode is not null ? SExpressionHelper.ParseXY(midNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, _, _) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
        {
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);
        }

        var (center, radius, startAngle, endAngle) = SExpressionHelper.ComputeArcFromThreePoints(start, mid, end);

        return new KiCadPcbArc
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Width = width,
            LayerName = node.GetChild("layer")?.GetString(),
            ArcStart = start,
            ArcMid = mid,
            ArcEnd = end,
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }
}
