using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Reads KiCad PCB files (<c>.kicad_pcb</c>) into <see cref="KiCadPcb"/> objects.
/// </summary>
public static class PcbReader
{
    /// <summary>
    /// Reads a PCB document from a file path.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed PCB document.</returns>
    public static async ValueTask<KiCadPcb> ReadAsync(string path, CancellationToken ct = default)
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
    /// Reads a PCB document from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed PCB document.</returns>
    public static async ValueTask<KiCadPcb> ReadAsync(Stream stream, CancellationToken ct = default)
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

    private static KiCadPcb Parse(SExpr root)
    {
        if (root.Token != "kicad_pcb")
            throw new KiCadFileException($"Expected 'kicad_pcb' root token, got '{root.Token}'.");

        const int MaxTestedVersion = 20231120;

        var pcb = new KiCadPcb
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = root.GetChild("generator")?.GetString(),
            GeneratorVersion = root.GetChild("generator_version")?.GetString()
        };

        var diagnostics = new List<KiCadDiagnostic>();

        if (pcb.Version > MaxTestedVersion)
        {
            diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                $"File format version {pcb.Version} is newer than the maximum tested version {MaxTestedVersion}. Some features may not be parsed correctly."));
        }
        var components = new List<KiCadPcbComponent>();
        var tracks = new List<KiCadPcbTrack>();
        var vias = new List<KiCadPcbVia>();
        var arcs = new List<KiCadPcbArc>();
        var texts = new List<KiCadPcbText>();
        var regions = new List<KiCadPcbRegion>();
        var nets = new List<(int, string)>();
        var graphicLines = new List<KiCadPcbGraphicLine>();
        var graphicArcs = new List<KiCadPcbGraphicArc>();
        var graphicCircles = new List<KiCadPcbGraphicCircle>();
        var graphicRects = new List<KiCadPcbGraphicRect>();
        var graphicPolys = new List<KiCadPcbGraphicPoly>();
        var graphicBeziers = new List<KiCadPcbGraphicBezier>();
        var zones = new List<KiCadPcbZone>();
        var netClasses = new List<KiCadPcbNetClass>();

        // Parse setup / board thickness
        var setup = root.GetChild("setup");
        double? boardThicknessMm = setup?.GetChild("board_thickness")?.GetDouble();
        if (boardThicknessMm is null)
        {
            boardThicknessMm = root.GetChild("general")?.GetChild("thickness")?.GetDouble();
        }
        pcb.BoardThickness = Coord.FromMm(boardThicknessMm ?? 1.6);

        // Store raw metadata subtrees for round-trip
        pcb.SetupRaw = root.GetChild("setup");
        pcb.LayersRaw = root.GetChild("layers");
        pcb.PaperRaw = root.GetChild("paper");
        pcb.TitleBlockRaw = root.GetChild("title_block");
        pcb.GeneralRaw = root.GetChild("general");

        foreach (var child in root.Children)
        {
            try
            {
                switch (child.Token)
                {
                    case "net":
                        var netNum = child.GetInt(0) ?? 0;
                        var netName = child.GetString(1) ?? "";
                        nets.Add((netNum, netName));
                        break;
                    case "footprint":
                        components.Add(FootprintReader.ParseFootprint(child));
                        break;
                    case "segment":
                        tracks.Add(ParseSegment(child));
                        break;
                    case "via":
                        vias.Add(ParseVia(child));
                        break;
                    case "arc":
                        arcs.Add(ParseArc(child));
                        break;
                    case "gr_text":
                        texts.Add(ParseGrText(child));
                        break;
                    case "zone":
                        zones.Add(ParseZoneStructured(child));
                        break;
                    case "gr_line":
                        graphicLines.Add(ParseGrLine(child));
                        break;
                    case "gr_arc":
                        graphicArcs.Add(ParseGrArc(child));
                        break;
                    case "gr_circle":
                        graphicCircles.Add(ParseGrCircle(child));
                        break;
                    case "gr_rect":
                        graphicRects.Add(ParseGrRect(child));
                        break;
                    case "gr_poly":
                        graphicPolys.Add(ParseGrPoly(child));
                        break;
                    case "gr_curve":
                    case "bezier":
                        graphicBeziers.Add(ParseGrBezier(child));
                        break;
                    case "net_class":
                        netClasses.Add(ParseNetClass(child));
                        break;
                    case "dimension":
                    case "target":
                    case "group":
                        // Store raw for round-trip
                        pcb.RawElementList.Add(child);
                        break;
                    case "version":
                    case "generator":
                    case "generator_version":
                    case "general":
                    case "paper":
                    case "title_block":
                    case "setup":
                    case "layers":
                        // Known tokens handled elsewhere
                        break;
                    default:
                        diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
                            $"Unknown PCB token '{child.Token}' was ignored", child.Token));
                        break;
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Error,
                    $"Failed to parse {child.Token}: {ex.Message}", child.GetString()));
            }
        }

        // Collect all pads from footprints
        var allPads = new List<IPcbPad>();
        foreach (var fp in components)
        {
            allPads.AddRange(fp.Pads);
        }

        pcb.ComponentList.AddRange(components);
        pcb.TrackList.AddRange(tracks);
        pcb.ViaList.AddRange(vias);
        pcb.ArcList.AddRange(arcs);
        pcb.TextList.AddRange(texts);
        pcb.RegionList.AddRange(regions);
        pcb.PadList.AddRange(allPads.OfType<KiCadPcbPad>());
        pcb.NetList.AddRange(nets);
        pcb.GraphicLineList.AddRange(graphicLines);
        pcb.GraphicArcList.AddRange(graphicArcs);
        pcb.GraphicCircleList.AddRange(graphicCircles);
        pcb.GraphicRectList.AddRange(graphicRects);
        pcb.GraphicPolyList.AddRange(graphicPolys);
        pcb.GraphicBezierList.AddRange(graphicBeziers);
        pcb.ZoneList.AddRange(zones);
        pcb.NetClassList.AddRange(netClasses);
        pcb.DiagnosticList.AddRange(diagnostics);
        pcb.SourceTree = root;

        return pcb;
    }

    private static KiCadPcbTrack ParseSegment(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;

        return new KiCadPcbTrack
        {
            Start = start,
            End = end,
            Width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0),
            LayerName = node.GetChild("layer")?.GetString(),
            Net = node.GetChild("net")?.GetInt() ?? 0,
            Uuid = SExpressionHelper.ParseUuid(node),
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            Status = node.GetChild("status")?.GetInt()
        };
    }

    private static KiCadPcbVia ParseVia(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);

        var via = new KiCadPcbVia
        {
            Location = loc,
            Diameter = Coord.FromMm(node.GetChild("size")?.GetDouble() ?? 0),
            HoleSize = Coord.FromMm(node.GetChild("drill")?.GetDouble() ?? 0),
            Net = node.GetChild("net")?.GetInt() ?? 0,
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        // Parse via type
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s)
            {
                switch (s.Value)
                {
                    case "blind": via.ViaType = ViaType.BlindBuried; break;
                    case "micro": via.ViaType = ViaType.Micro; break;
                    case "locked": via.IsLocked = true; break;
                }
            }
        }

        // Parse layers
        var layersNode = node.GetChild("layers");
        if (layersNode is not null && layersNode.Values.Count >= 2)
        {
            via.StartLayerName = layersNode.GetString(0);
            via.EndLayerName = layersNode.GetString(1);
        }

        via.IsFree = node.GetChild("free")?.GetBool() ?? false;
        via.RemoveUnusedLayers = node.GetChild("remove_unused_layers") is not null;
        via.KeepEndLayers = node.GetChild("keep_end_layers") is not null;
        via.Status = node.GetChild("status")?.GetInt();

        // Parse teardrop (store raw for round-trip)
        var teardropNode = node.GetChild("teardrops");
        if (teardropNode is null) teardropNode = node.GetChild("teardrop");
        if (teardropNode is not null)
        {
            via.TeardropRaw = teardropNode;
            via.TeardropEnabled = true;
        }

        return via;
    }

    private static KiCadPcbArc ParseArc(SExpr node)
    {
        var startNode = node.GetChild("start");
        var midNode = node.GetChild("mid");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var mid = midNode is not null ? SExpressionHelper.ParseXY(midNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;

        var (center, radius, startAngle, endAngle) = SExpressionHelper.ComputeArcFromThreePoints(start, mid, end);

        return new KiCadPcbArc
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0),
            LayerName = node.GetChild("layer")?.GetString(),
            Net = node.GetChild("net")?.GetInt() ?? 0,
            ArcStart = start,
            ArcMid = mid,
            ArcEnd = end,
            Uuid = SExpressionHelper.ParseUuid(node),
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            Status = node.GetChild("status")?.GetInt()
        };
    }

    private static KiCadPcbText ParseGrText(SExpr node)
    {
        var text = new KiCadPcbText
        {
            Text = node.GetString() ?? ""
        };

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        text.Location = loc;
        text.Rotation = angle;
        text.LayerName = node.GetChild("layer")?.GetString();
        text.Uuid = SExpressionHelper.ParseUuid(node);

        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor) = SExpressionHelper.ParseTextEffects(node);
        text.Height = fontH;
        text.FontWidth = fontW;
        text.FontBold = isBold;
        text.FontItalic = isItalic;
        text.FontName = fontFace;
        text.FontThickness = fontThickness;
        text.FontColor = fontColor;
        text.Justification = justification;
        text.IsMirrored = isMirrored;

        // Check for hide both from effects and as a top-level symbol on the gr_text node
        text.IsHidden = isHidden || node.Values.Any(v => v is SExprSymbol s && s.Value == "hide");

        return text;
    }

    // -- Board-level graphic parsers --

    private static void ParseGraphicCommon(SExpr node, KiCadPcbGraphic graphic)
    {
        graphic.LayerName = node.GetChild("layer")?.GetString();
        graphic.Uuid = SExpressionHelper.ParseUuid(node);
        graphic.IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked");

        var (strokeWidth, strokeStyle, strokeColor) = SExpressionHelper.ParseStroke(node);
        graphic.StrokeWidth = strokeWidth;
        graphic.StrokeStyle = strokeStyle;
        graphic.StrokeColor = strokeColor;

        // Legacy width fallback
        if (graphic.StrokeWidth == Coord.Zero)
            graphic.StrokeWidth = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);
        graphic.FillType = fillType;
        graphic.FillColor = fillColor;
    }

    private static KiCadPcbGraphicLine ParseGrLine(SExpr node)
    {
        var line = new KiCadPcbGraphicLine();
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        line.Start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        line.End = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        ParseGraphicCommon(node, line);
        return line;
    }

    private static KiCadPcbGraphicArc ParseGrArc(SExpr node)
    {
        var arc = new KiCadPcbGraphicArc();
        var startNode = node.GetChild("start");
        var midNode = node.GetChild("mid");
        var endNode = node.GetChild("end");
        arc.Start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        arc.Mid = midNode is not null ? SExpressionHelper.ParseXY(midNode) : CoordPoint.Zero;
        arc.End = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        ParseGraphicCommon(node, arc);
        return arc;
    }

    private static KiCadPcbGraphicCircle ParseGrCircle(SExpr node)
    {
        var circle = new KiCadPcbGraphicCircle();
        var centerNode = node.GetChild("center");
        var endNode = node.GetChild("end");
        circle.Center = centerNode is not null ? SExpressionHelper.ParseXY(centerNode) : CoordPoint.Zero;
        circle.End = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        ParseGraphicCommon(node, circle);
        return circle;
    }

    private static KiCadPcbGraphicRect ParseGrRect(SExpr node)
    {
        var rect = new KiCadPcbGraphicRect();
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        rect.Start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        rect.End = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        ParseGraphicCommon(node, rect);
        return rect;
    }

    private static KiCadPcbGraphicPoly ParseGrPoly(SExpr node)
    {
        var poly = new KiCadPcbGraphicPoly();
        poly.Points = SExpressionHelper.ParsePoints(node);
        ParseGraphicCommon(node, poly);
        return poly;
    }

    private static KiCadPcbGraphicBezier ParseGrBezier(SExpr node)
    {
        var bezier = new KiCadPcbGraphicBezier();
        bezier.Points = SExpressionHelper.ParsePoints(node);
        ParseGraphicCommon(node, bezier);
        return bezier;
    }

    // -- Zone structured parser (Phase D) --

    private static KiCadPcbZone ParseZoneStructured(SExpr node)
    {
        var zone = new KiCadPcbZone
        {
            Net = node.GetChild("net")?.GetInt() ?? 0,
            NetName = node.GetChild("net_name")?.GetString(),
            Uuid = SExpressionHelper.ParseUuid(node),
            Priority = node.GetChild("priority")?.GetInt() ?? 0,
            Name = node.GetChild("name")?.GetString(),
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            MinThickness = Coord.FromMm(node.GetChild("min_thickness")?.GetDouble() ?? 0)
        };

        // Layer(s)
        zone.LayerName = node.GetChild("layer")?.GetString();
        var layersNode = node.GetChild("layers");
        if (layersNode is not null)
        {
            var layers = new List<string>();
            foreach (var v in layersNode.Values)
            {
                if (v is SExprString s) layers.Add(s.Value);
                else if (v is SExprSymbol sym) layers.Add(sym.Value);
            }
            zone.LayerNames = layers;
        }

        // Hatch
        var hatchNode = node.GetChild("hatch");
        if (hatchNode is not null)
        {
            zone.HatchStyle = hatchNode.GetString(0);
            zone.HatchPitch = hatchNode.GetDouble(1) ?? 0;
        }

        // Connect pads (store raw for complex round-trip)
        var connectPadsNode = node.GetChild("connect_pads");
        zone.ConnectPadsRaw = connectPadsNode;
        if (connectPadsNode is not null)
        {
            zone.ConnectPadsMode = connectPadsNode.GetString(0);
            zone.ConnectPadsClearance = Coord.FromMm(connectPadsNode.GetChild("clearance")?.GetDouble() ?? 0);
        }

        // Keepout (store raw)
        var keepoutNode = node.GetChild("keepout");
        zone.KeepoutRaw = keepoutNode;
        if (keepoutNode is not null)
        {
            zone.IsKeepout = true;
            zone.KeepoutTracks = keepoutNode.GetChild("tracks")?.GetString();
            zone.KeepoutVias = keepoutNode.GetChild("vias")?.GetString();
            zone.KeepoutPads = keepoutNode.GetChild("pads")?.GetString();
            zone.KeepoutCopperpour = keepoutNode.GetChild("copperpour")?.GetString();
            zone.KeepoutFootprints = keepoutNode.GetChild("footprints")?.GetString();
        }

        // Fill settings (store raw for round-trip)
        var fillNode = node.GetChild("fill");
        zone.FillRaw = fillNode;
        if (fillNode is not null)
        {
            zone.IsFilled = fillNode.GetBool(0) ?? false;
            zone.ThermalGap = Coord.FromMm(fillNode.GetChild("thermal_gap")?.GetDouble() ?? 0);
            zone.ThermalBridgeWidth = Coord.FromMm(fillNode.GetChild("thermal_bridge_width")?.GetDouble() ?? 0);
            zone.SmoothingType = fillNode.GetChild("smoothing")?.GetString();
            zone.SmoothingRadius = Coord.FromMm(fillNode.GetChild("radius")?.GetDouble() ?? 0);
            zone.IslandRemovalMode = fillNode.GetChild("island_removal_mode")?.GetInt();
            zone.IslandAreaMin = fillNode.GetChild("island_area_min")?.GetDouble();
            zone.HatchThickness = Coord.FromMm(fillNode.GetChild("hatch_thickness")?.GetDouble() ?? 0);
            zone.HatchGap = Coord.FromMm(fillNode.GetChild("hatch_gap")?.GetDouble() ?? 0);
            zone.HatchOrientation = fillNode.GetChild("hatch_orientation")?.GetDouble() ?? 0;
            zone.HatchSmoothingLevel = fillNode.GetChild("hatch_smoothing_level")?.GetInt() ?? 0;
            zone.HatchSmoothingValue = fillNode.GetChild("hatch_smoothing_value")?.GetDouble() ?? 0;
            zone.HatchBorderAlgorithm = fillNode.GetChild("hatch_border_algorithm")?.GetInt() ?? 0;
            zone.HatchMinHoleArea = fillNode.GetChild("hatch_min_hole_area")?.GetDouble() ?? 0;
        }

        // Zone outline polygon
        var polygon = node.GetChild("polygon");
        if (polygon is not null)
        {
            zone.Outline = SExpressionHelper.ParsePoints(polygon);
        }

        // Filled polygons (store raw for round-trip)
        foreach (var filledPoly in node.GetChildren("filled_polygon"))
        {
            zone.FilledPolygonsRaw.Add(filledPoly);
        }

        // Fill segments (store raw for round-trip)
        foreach (var fillSeg in node.GetChildren("fill_segments"))
        {
            zone.FillSegmentsRaw.Add(fillSeg);
        }

        return zone;
    }

    // -- Net class parser (Phase C) --

    private static KiCadPcbNetClass ParseNetClass(SExpr node)
    {
        var nc = new KiCadPcbNetClass
        {
            Name = node.GetString(0) ?? "",
            Description = node.GetString(1) ?? "",
            Clearance = Coord.FromMm(node.GetChild("clearance")?.GetDouble() ?? 0),
            TraceWidth = Coord.FromMm(node.GetChild("trace_width")?.GetDouble() ?? 0),
            ViaDia = Coord.FromMm(node.GetChild("via_dia")?.GetDouble() ?? 0),
            ViaDrill = Coord.FromMm(node.GetChild("via_drill")?.GetDouble() ?? 0),
            UViaDia = Coord.FromMm(node.GetChild("uvia_dia")?.GetDouble() ?? 0),
            UViaDrill = Coord.FromMm(node.GetChild("uvia_drill")?.GetDouble() ?? 0)
        };

        // Parse add_net children
        foreach (var netChild in node.GetChildren("add_net"))
        {
            var name = netChild.GetString();
            if (name is not null)
                nc.NetNames.Add(name);
        }

        return nc;
    }
}
