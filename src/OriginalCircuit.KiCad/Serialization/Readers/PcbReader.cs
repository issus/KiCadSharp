using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models;
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
            GeneratorVersion = root.GetChild("generator_version")?.GetString()
        };

        var genNode = root.GetChild("generator");
        if (genNode is not null)
        {
            pcb.Generator = genNode.GetString();
            pcb.GeneratorIsSymbol = genNode.Values.Count > 0 && genNode.Values[0] is SExprSymbol;
        }

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

        // Board thickness and legacy_teardrops are extracted from general section
        var generalNode = root.GetChild("general");
        pcb.BoardThickness = Coord.FromMm(generalNode?.GetChild("thickness")?.GetDouble() ?? 1.6);
        var ltNode = generalNode?.GetChild("legacy_teardrops");
        if (ltNode is not null)
        {
            pcb.HasLegacyTeardrops = true;
            pcb.LegacyTeardrops = ltNode.GetBool() ?? false;
        }

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
                        var seg = ParseSegment(child);
                        tracks.Add(seg);
                        pcb.BoardElementOrderList.Add(seg);
                        break;
                    case "via":
                        var via = ParseVia(child);
                        vias.Add(via);
                        pcb.BoardElementOrderList.Add(via);
                        break;
                    case "arc":
                        var pcbArc = ParseArc(child);
                        arcs.Add(pcbArc);
                        pcb.BoardElementOrderList.Add(pcbArc);
                        break;
                    case "gr_text":
                        var grText = ParseGrText(child);
                        texts.Add(grText);
                        pcb.BoardElementOrderList.Add(grText);
                        break;
                    case "zone":
                        var zone = ParseZoneStructured(child);
                        zones.Add(zone);
                        pcb.BoardElementOrderList.Add(zone);
                        break;
                    case "gr_line":
                        var grLine = ParseGrLine(child);
                        graphicLines.Add(grLine);
                        pcb.BoardElementOrderList.Add(grLine);
                        break;
                    case "gr_arc":
                        var grArc = ParseGrArc(child);
                        graphicArcs.Add(grArc);
                        pcb.BoardElementOrderList.Add(grArc);
                        break;
                    case "gr_circle":
                        var grCircle = ParseGrCircle(child);
                        graphicCircles.Add(grCircle);
                        pcb.BoardElementOrderList.Add(grCircle);
                        break;
                    case "gr_rect":
                        var grRect = ParseGrRect(child);
                        graphicRects.Add(grRect);
                        pcb.BoardElementOrderList.Add(grRect);
                        break;
                    case "gr_poly":
                        var grPoly = ParseGrPoly(child);
                        graphicPolys.Add(grPoly);
                        pcb.BoardElementOrderList.Add(grPoly);
                        break;
                    case "gr_curve":
                    case "bezier":
                        var grBezier = ParseGrBezier(child);
                        graphicBeziers.Add(grBezier);
                        pcb.BoardElementOrderList.Add(grBezier);
                        break;
                    case "net_class":
                        netClasses.Add(ParseNetClass(child));
                        break;
                    case "group":
                        var group = FootprintReader.ParseGroup(child);
                        pcb.GroupList.Add(group);
                        pcb.BoardElementOrderList.Add(group);
                        break;
                    case "embedded_files":
                        pcb.EmbeddedFiles = FootprintReader.ParseEmbeddedFiles(child);
                        break;
                    case "embedded_fonts":
                        pcb.EmbeddedFonts = child.GetBool();
                        break;
                    case "dimension":
                        var dim = ParseDimension(child);
                        pcb.DimensionList.Add(dim);
                        pcb.BoardElementOrderList.Add(dim);
                        break;
                    case "generated":
                        var gen = ParseGeneratedElement(child);
                        pcb.GeneratedElementList.Add(gen);
                        pcb.BoardElementOrderList.Add(gen);
                        break;
                    case "target":
                    case "gr_text_box":
                    case "image":
                    case "gr_bbox":
                        // Not yet fully implemented
                        break;
                    case "layers":
                        ParseLayers(child, pcb);
                        break;
                    case "paper":
                        ParsePaper(child, pcb);
                        break;
                    case "title_block":
                        pcb.TitleBlock = ParseTitleBlock(child);
                        break;
                    case "setup":
                        pcb.Setup = ParseSetup(child);
                        // Also set board thickness from setup
                        if (pcb.Setup.HasBoardThickness)
                            pcb.BoardThickness = pcb.Setup.BoardThickness;
                        break;
                    case "property":
                        var propKey = child.GetString(0);
                        var propVal = child.GetString(1);
                        if (propKey is not null && propVal is not null)
                            pcb.PropertyList.Add(new KeyValuePair<string, string>(propKey, propVal));
                        break;
                    case "version":
                    case "generator":
                    case "generator_version":
                    case "general":
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

        return pcb;
    }

    private static KiCadPcbTrack ParseSegment(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;

        var (segUuid, segUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var segUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var segUuidIsSymbol = segUuidNode?.Values.Count > 0 && segUuidNode.Values[0] is SExprSymbol;

        var track = new KiCadPcbTrack
        {
            Start = start,
            End = end,
            Width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0),
            LayerName = node.GetChild("layer")?.GetString(),
            Net = node.GetChild("net")?.GetInt() ?? 0,
            Uuid = segUuid,
            UuidToken = segUuidToken,
            UuidIsSymbol = segUuidIsSymbol,
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            Status = node.GetChild("status")?.GetInt()
        };

        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            track.IsLocked = lockedChild.GetBool() ?? true;
            track.LockedIsChildNode = true;
        }

        return track;
    }

    private static KiCadPcbVia ParseVia(SExpr node)
    {
        var (loc, _) = SExpressionHelper.ParsePosition(node);

        var (viaUuid, viaUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var viaUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var viaUuidIsSymbol = viaUuidNode?.Values.Count > 0 && viaUuidNode.Values[0] is SExprSymbol;

        var via = new KiCadPcbVia
        {
            Location = loc,
            Diameter = Coord.FromMm(node.GetChild("size")?.GetDouble() ?? 0),
            HoleSize = Coord.FromMm(node.GetChild("drill")?.GetDouble() ?? 0),
            Net = node.GetChild("net")?.GetInt() ?? 0,
            Uuid = viaUuid,
            UuidToken = viaUuidToken,
            UuidIsSymbol = viaUuidIsSymbol
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

        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            via.IsLocked = lockedChild.GetBool() ?? true;
            via.LockedIsChildNode = true;
        }

        // Parse layers
        var layersNode = node.GetChild("layers");
        if (layersNode is not null && layersNode.Values.Count >= 2)
        {
            via.StartLayerName = layersNode.GetString(0);
            via.EndLayerName = layersNode.GetString(1);
        }

        via.IsFree = node.GetChild("free")?.GetBool() ?? false;

        var removeUnusedNode = node.GetChild("remove_unused_layers");
        if (removeUnusedNode is not null)
            via.RemoveUnusedLayers = removeUnusedNode.GetBool() ?? true;

        var keepEndNode = node.GetChild("keep_end_layers");
        if (keepEndNode is not null)
            via.KeepEndLayers = keepEndNode.GetBool() ?? true;
        via.Status = node.GetChild("status")?.GetInt();

        // Parse teardrops with full sub-properties
        var teardropNode = node.GetChild("teardrops") ?? node.GetChild("teardrop");
        if (teardropNode is not null)
        {
            via.TeardropEnabled = true;
            var enabledNode = teardropNode.GetChild("enabled");
            if (enabledNode is not null)
                via.TeardropEnabled = enabledNode.GetBool() ?? true;
            via.TeardropBestLengthRatio = teardropNode.GetChild("best_length_ratio")?.GetDouble();
            var maxLenNode = teardropNode.GetChild("max_length");
            if (maxLenNode is not null)
                via.TeardropMaxLength = Coord.FromMm(maxLenNode.GetDouble() ?? 0);
            via.TeardropBestWidthRatio = teardropNode.GetChild("best_width_ratio")?.GetDouble();
            var maxWidthNode = teardropNode.GetChild("max_width");
            if (maxWidthNode is not null)
                via.TeardropMaxWidth = Coord.FromMm(maxWidthNode.GetDouble() ?? 0);
            via.TeardropCurvedEdges = teardropNode.GetChild("curved_edges")?.GetBool();
            via.TeardropFilterRatio = teardropNode.GetChild("filter_ratio")?.GetDouble();
            via.TeardropAllowTwoSegments = teardropNode.GetChild("allow_two_segments")?.GetBool();
            via.TeardropPreferZoneConnections = teardropNode.GetChild("prefer_zone_connections")?.GetBool();
        }

        // Parse tenting
        var tentingNode = node.GetChild("tenting");
        if (tentingNode is not null)
        {
            via.HasTenting = true;
            var frontNode = tentingNode.GetChild("front");
            var backNode = tentingNode.GetChild("back");
            if (frontNode is not null || backNode is not null)
            {
                // Child node format: (tenting (front none) (back none))
                via.TentingIsChildNode = true;
                if (frontNode is not null) via.TentingFrontValue = frontNode.GetString();
                if (backNode is not null) via.TentingBackValue = backNode.GetString();
            }
            else
            {
                // Bare symbol format: (tenting front back)
                foreach (var tv in tentingNode.Values)
                {
                    if (tv is SExprSymbol ts)
                    {
                        if (ts.Value == "front") via.TentingFront = true;
                        else if (ts.Value == "back") via.TentingBack = true;
                    }
                }
            }
        }

        // Parse capping/filling (simple value tokens)
        via.Capping = node.GetChild("capping")?.GetString();
        via.Filling = node.GetChild("filling")?.GetString();

        // Parse covering (front/back children)
        var coveringViaNode = node.GetChild("covering");
        if (coveringViaNode is not null)
        {
            via.HasCovering = true;
            via.CoveringFront = coveringViaNode.GetChild("front")?.GetString();
            via.CoveringBack = coveringViaNode.GetChild("back")?.GetString();
        }

        // Parse plugging (front/back children)
        var pluggingViaNode = node.GetChild("plugging");
        if (pluggingViaNode is not null)
        {
            via.HasPlugging = true;
            via.PluggingFront = pluggingViaNode.GetChild("front")?.GetString();
            via.PluggingBack = pluggingViaNode.GetChild("back")?.GetString();
        }

        // Parse zone_layer_connections
        var zlcNode = node.GetChild("zone_layer_connections");
        if (zlcNode is not null)
        {
            var connections = new List<string>();
            foreach (var v in zlcNode.Values)
            {
                if (v is SExprString s) connections.Add(s.Value);
                else if (v is SExprSymbol sym) connections.Add(sym.Value);
            }
            via.ZoneLayerConnections = connections;
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

        var (arcUuid, arcUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var arcUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var arcUuidIsSymbol = arcUuidNode?.Values.Count > 0 && arcUuidNode.Values[0] is SExprSymbol;

        var arc = new KiCadPcbArc
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
            Uuid = arcUuid,
            UuidToken = arcUuidToken,
            UuidIsSymbol = arcUuidIsSymbol,
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            Status = node.GetChild("status")?.GetInt()
        };

        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            arc.IsLocked = lockedChild.GetBool() ?? true;
            arc.LockedIsChildNode = true;
        }

        return arc;
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

        // Detect whether the angle was explicitly present in the at node
        var atNode = node.GetChild("at");
        text.PositionIncludesAngle = atNode is not null && atNode.Values.Count >= 3;

        var layerNode = node.GetChild("layer");
        text.LayerName = layerNode?.GetString();

        var (textUuid, textUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var textUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var textUuidIsSymbol = textUuidNode?.Values.Count > 0 && textUuidNode.Values[0] is SExprSymbol;
        text.Uuid = textUuid;
        text.UuidToken = textUuidToken;
        text.UuidIsSymbol = textUuidIsSymbol;

        // Check for knockout on layer node: (layer "F.SilkS" knockout)
        if (layerNode is not null)
        {
            foreach (var v in layerNode.Values)
            {
                if (v is SExprSymbol s && s.Value == "knockout")
                {
                    text.IsKnockout = true;
                    break;
                }
            }
        }

        // Also check for knockout as bare symbol on text node (legacy format)
        if (!text.IsKnockout)
        {
            foreach (var v in node.Values)
            {
                if (v is SExprSymbol s && s.Value == "knockout")
                {
                    text.IsKnockout = true;
                    break;
                }
            }
        }

        var (fontH, fontW, justification, isHidden, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _, boldIsSymbol, italicIsSymbol) = SExpressionHelper.ParseTextEffectsEx(node);
        text.Height = fontH;
        text.FontWidth = fontW;
        text.FontBold = isBold;
        text.FontItalic = isItalic;
        text.BoldIsSymbol = boldIsSymbol;
        text.ItalicIsSymbol = italicIsSymbol;
        text.FontName = fontFace;
        text.FontThickness = fontThickness;
        text.FontColor = fontColor;
        text.Justification = justification;
        text.IsMirrored = isMirrored;

        // Check for hide both from effects and as a top-level symbol on the gr_text node
        text.IsHidden = isHidden || node.Values.Any(v => v is SExprSymbol s && s.Value == "hide");

        // Parse render_cache
        var renderCacheNode = node.GetChild("render_cache");
        if (renderCacheNode is not null)
        {
            var rc = new KiCadTextRenderCache
            {
                FontName = renderCacheNode.GetString(0),
            };
            var sizeVal = renderCacheNode.GetDouble(1);
            if (sizeVal.HasValue)
                rc.FontSize = new CoordPoint(Coord.FromMm(sizeVal.Value), Coord.Zero);

            foreach (var polyNode in renderCacheNode.GetChildren("polygon"))
            {
                var pts = SExpressionHelper.ParsePoints(polyNode);
                if (pts.Count > 0)
                    rc.Polygons.Add(pts);
            }
            text.RenderCache = rc;
        }

        return text;
    }

    // -- Board-level graphic parsers --

    private static void ParseGraphicCommon(SExpr node, KiCadPcbGraphic graphic)
    {
        graphic.LayerName = node.GetChild("layer")?.GetString();

        var (grUuid, grUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var grUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var grUuidIsSymbol = grUuidNode?.Values.Count > 0 && grUuidNode.Values[0] is SExprSymbol;
        graphic.Uuid = grUuid;
        graphic.UuidToken = grUuidToken;
        graphic.UuidIsSymbol = grUuidIsSymbol;

        // Check for locked as bare symbol
        graphic.IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked");
        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            graphic.IsLocked = lockedChild.GetBool() ?? true;
            graphic.LockedIsChildNode = true;
        }

        var strokeNode = node.GetChild("stroke");
        if (strokeNode is not null)
            graphic.HasStroke = true;
        var (strokeWidth, strokeStyle, strokeColor) = SExpressionHelper.ParseStroke(node);
        graphic.StrokeWidth = strokeWidth;
        graphic.StrokeStyle = strokeStyle;
        graphic.StrokeColor = strokeColor;

        // Legacy width fallback
        if (graphic.StrokeWidth == Coord.Zero)
            graphic.StrokeWidth = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        var (fillType, _, fillColor, usePcbFillFormat) = SExpressionHelper.ParseFillWithFormat(node);
        graphic.FillType = fillType;
        graphic.FillColor = fillColor;
        graphic.UsePcbFillFormat = usePcbFillFormat;
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

    internal static KiCadPcbZone ParseZone(SExpr node) => ParseZoneStructured(node);

    private static KiCadPcbZone ParseZoneStructured(SExpr node)
    {
        var (zoneUuid, zoneUuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        var zoneUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
        var zoneUuidIsSymbol = zoneUuidNode?.Values.Count > 0 && zoneUuidNode.Values[0] is SExprSymbol;

        var zone = new KiCadPcbZone
        {
            Net = node.GetChild("net")?.GetInt() ?? 0,
            NetName = node.GetChild("net_name")?.GetString(),
            Uuid = zoneUuid,
            UuidToken = zoneUuidToken,
            UuidIsSymbol = zoneUuidIsSymbol,
            Priority = node.GetChild("priority")?.GetInt() ?? 0,
            Name = node.GetChild("name")?.GetString(),
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked"),
            MinThickness = Coord.FromMm(node.GetChild("min_thickness")?.GetDouble() ?? 0)
        };

        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            zone.IsLocked = lockedChild.GetBool() ?? true;
            zone.LockedIsChildNode = true;
        }

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

        // Connect pads
        var connectPadsNode = node.GetChild("connect_pads");
        if (connectPadsNode is not null)
        {
            zone.HasConnectPads = true;
            zone.ConnectPadsMode = connectPadsNode.GetString(0);
            zone.ConnectPadsClearance = Coord.FromMm(connectPadsNode.GetChild("clearance")?.GetDouble() ?? 0);
        }

        // Keepout
        var keepoutNode = node.GetChild("keepout");
        if (keepoutNode is not null)
        {
            zone.IsKeepout = true;
            zone.KeepoutTracks = keepoutNode.GetChild("tracks")?.GetString();
            zone.KeepoutVias = keepoutNode.GetChild("vias")?.GetString();
            zone.KeepoutPads = keepoutNode.GetChild("pads")?.GetString();
            zone.KeepoutCopperpour = keepoutNode.GetChild("copperpour")?.GetString();
            zone.KeepoutFootprints = keepoutNode.GetChild("footprints")?.GetString();
        }

        // Placement
        var placementNode = node.GetChild("placement");
        if (placementNode is not null)
        {
            zone.HasPlacement = true;
            zone.PlacementEnabled = placementNode.GetChild("enabled")?.GetBool() ?? false;
            zone.PlacementSheetName = placementNode.GetChild("sheetname")?.GetString();
            zone.PlacementComponentClass = placementNode.GetChild("component_class")?.GetString();
        }

        // Fill settings
        var fillNode = node.GetChild("fill");
        if (fillNode is not null)
        {
            var fillBool = fillNode.GetBool(0);
            zone.HasFillValue = fillBool.HasValue;
            zone.IsFilled = fillBool ?? false;
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

            // Fill mode
            zone.FillMode = fillNode.GetChild("mode")?.GetString();
        }

        // Zone outline polygon
        var polygon = node.GetChild("polygon");
        if (polygon is not null)
        {
            // Parse full vertices (including arcs) for round-trip fidelity
            var vertices = SExpressionHelper.ParsePolygonVertices(polygon);
            if (vertices.Count > 0)
            {
                zone.OutlineVertices = vertices;
                // Also populate Outline with just the xy points for backward compat
                zone.Outline = vertices
                    .Where(v => !v.IsArc)
                    .Select(v => v.Point)
                    .ToList();
            }
        }

        // Filled areas thickness
        var fatNode = node.GetChild("filled_areas_thickness");
        if (fatNode is not null)
        {
            zone.HasFilledAreasThickness = true;
            zone.FilledAreasThickness = fatNode.GetBool() ?? false;
        }

        // Zone attr
        var attrZoneNode = node.GetChild("attr");
        if (attrZoneNode is not null)
        {
            zone.HasAttr = true;
            var teardropNode = attrZoneNode.GetChild("teardrop");
            if (teardropNode is not null)
                zone.AttrTeardropType = teardropNode.GetChild("type")?.GetString();
        }

        // Filled polygons
        foreach (var fpChild in node.GetChildren("filled_polygon"))
        {
            var fp = new KiCadPcbZoneFilledPolygon
            {
                LayerName = fpChild.GetChild("layer")?.GetString() ?? "",
                Points = SExpressionHelper.ParsePoints(fpChild),
                IslandIndex = fpChild.GetChild("island")?.GetInt()
            };
            zone.FilledPolygons.Add(fp);
        }

        // Fill segments (legacy)
        foreach (var fsChild in node.GetChildren("fill_segments"))
        {
            var fs = new KiCadPcbZoneFillSegment
            {
                LayerName = fsChild.GetChild("layer")?.GetString() ?? "",
                Points = SExpressionHelper.ParsePoints(fsChild)
            };
            zone.FillSegments.Add(fs);
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

        // Track Has* flags for existing properties
        if (node.GetChild("clearance") is not null) nc.HasClearance = true;
        if (node.GetChild("trace_width") is not null) nc.HasTraceWidth = true;
        if (node.GetChild("via_dia") is not null) nc.HasViaDia = true;
        if (node.GetChild("via_drill") is not null) nc.HasViaDrill = true;
        if (node.GetChild("uvia_dia") is not null) nc.HasUViaDia = true;
        if (node.GetChild("uvia_drill") is not null) nc.HasUViaDrill = true;

        // Parse diff_pair and bus_width
        if (node.GetChild("diff_pair_width") is not null)
        {
            nc.DiffPairWidth = Coord.FromMm(node.GetChild("diff_pair_width")?.GetDouble() ?? 0);
            nc.HasDiffPairWidth = true;
        }
        if (node.GetChild("diff_pair_gap") is not null)
        {
            nc.DiffPairGap = Coord.FromMm(node.GetChild("diff_pair_gap")?.GetDouble() ?? 0);
            nc.HasDiffPairGap = true;
        }
        if (node.GetChild("bus_width") is not null)
        {
            nc.BusWidth = Coord.FromMm(node.GetChild("bus_width")?.GetDouble() ?? 0);
            nc.HasBusWidth = true;
        }

        // Parse add_net children
        foreach (var netChild in node.GetChildren("add_net"))
        {
            var name = netChild.GetString();
            if (name is not null)
                nc.NetNames.Add(name);
        }

        return nc;
    }

    private static void ParseLayers(SExpr node, KiCadPcb pcb)
    {
        foreach (var layerChild in node.Children)
        {
            // Token is the ordinal number: (0 "F.Cu" signal)
            _ = int.TryParse(layerChild.Token, out var ordinal);
            var canonicalName = layerChild.GetString(0) ?? "";
            var layerType = layerChild.GetString(1) ?? "";
            var userName = layerChild.GetString(2);
            pcb.LayerDefinitionList.Add(new KiCadPcbLayerDefinition
            {
                Ordinal = ordinal,
                CanonicalName = canonicalName,
                LayerType = layerType,
                UserName = userName
            });
        }
    }

    private static void ParsePaper(SExpr node, KiCadPcb pcb)
    {
        // Can be (paper "A4") or (paper "A4" portrait) or (paper 297 210) or (paper "User" 210.007 229.997)
        var firstVal = node.GetString(0);
        if (firstVal is not null)
        {
            pcb.Paper = firstVal;
            // Check for custom dimensions after paper name (e.g. "User" 210.007 229.997)
            var w = node.GetDouble(1);
            var h = node.GetDouble(2);
            if (w.HasValue && h.HasValue)
            {
                pcb.PaperWidth = w.Value;
                pcb.PaperHeight = h.Value;
            }
            // Check for portrait
            foreach (var v in node.Values)
            {
                if (v is SExprSymbol sym && sym.Value == "portrait")
                    pcb.PaperPortrait = true;
            }
        }
        else
        {
            // Custom dimensions without a name
            pcb.PaperWidth = node.GetDouble(0);
            pcb.PaperHeight = node.GetDouble(1);
        }
    }

    internal static KiCadTitleBlock ParseTitleBlock(SExpr node)
    {
        var tb = new KiCadTitleBlock
        {
            Title = node.GetChild("title")?.GetString(),
            Date = node.GetChild("date")?.GetString(),
            Revision = node.GetChild("rev")?.GetString(),
            Company = node.GetChild("company")?.GetString()
        };

        foreach (var commentNode in node.GetChildren("comment"))
        {
            var num = commentNode.GetInt(0);
            var text = commentNode.GetString(1);
            if (num.HasValue && text is not null)
                tb.Comments[num.Value] = text;
        }

        return tb;
    }

    private static KiCadPcbSetup ParseSetup(SExpr node)
    {
        var setup = new KiCadPcbSetup();

        var btNode = node.GetChild("board_thickness");
        if (btNode is not null)
        {
            setup.BoardThickness = Coord.FromMm(btNode.GetDouble() ?? 1.6);
            setup.HasBoardThickness = true;
        }

        var ptmNode = node.GetChild("pad_to_mask_clearance");
        if (ptmNode is not null)
        {
            setup.PadToMaskClearance = Coord.FromMm(ptmNode.GetDouble() ?? 0);
            setup.HasPadToMaskClearance = true;
        }

        var smmwNode = node.GetChild("solder_mask_min_width");
        if (smmwNode is not null)
        {
            setup.SolderMaskMinWidth = Coord.FromMm(smmwNode.GetDouble() ?? 0);
            setup.HasSolderMaskMinWidth = true;
        }

        var ptpcNode = node.GetChild("pad_to_paste_clearance");
        if (ptpcNode is not null)
        {
            setup.PadToPasteClearance = Coord.FromMm(ptpcNode.GetDouble() ?? 0);
            setup.HasPadToPasteClearance = true;
        }

        var ptpcrNode = node.GetChild("pad_to_paste_clearance_ratio");
        if (ptpcrNode is not null)
        {
            setup.PadToPasteClearanceRatio = ptpcrNode.GetDouble();
            setup.HasPadToPasteClearanceRatio = true;
        }

        var asbNode = node.GetChild("allow_soldermask_bridges_in_footprints");
        if (asbNode is not null)
        {
            setup.AllowSolderMaskBridgesInFootprints = asbNode.GetBool();
            setup.HasAllowSolderMaskBridges = true;
        }

        var tentingNode = node.GetChild("tenting");
        if (tentingNode is not null)
        {
            setup.HasTenting = true;
            // Check if child node format (front yes) vs bare symbol (front)
            var frontChild = tentingNode.GetChild("front");
            var backChild = tentingNode.GetChild("back");
            if (frontChild is not null || backChild is not null)
            {
                setup.TentingIsChildNode = true;
                if (frontChild is not null) setup.TentingFront = frontChild.GetBool() ?? true;
                if (backChild is not null) setup.TentingBack = backChild.GetBool() ?? true;
            }
            else
            {
                // Bare symbol format: (tenting front back)
                foreach (var v in tentingNode.Values)
                {
                    if (v is SExprSymbol sym)
                    {
                        if (sym.Value == "front") setup.TentingFront = true;
                        else if (sym.Value == "back") setup.TentingBack = true;
                    }
                }
            }
        }

        // Covering
        var coveringNode = node.GetChild("covering");
        if (coveringNode is not null)
        {
            setup.HasCovering = true;
            setup.CoveringFront = coveringNode.GetChild("front")?.GetString();
            setup.CoveringBack = coveringNode.GetChild("back")?.GetString();
        }

        // Plugging
        var pluggingNode = node.GetChild("plugging");
        if (pluggingNode is not null)
        {
            setup.HasPlugging = true;
            setup.PluggingFront = pluggingNode.GetChild("front")?.GetString();
            setup.PluggingBack = pluggingNode.GetChild("back")?.GetString();
        }

        // Capping
        setup.Capping = node.GetChild("capping")?.GetString();

        // Filling
        setup.Filling = node.GetChild("filling")?.GetString();

        var aoNode = node.GetChild("aux_axis_origin");
        if (aoNode is not null)
        {
            setup.AuxAxisOrigin = new CoordPoint(
                Coord.FromMm(aoNode.GetDouble(0) ?? 0),
                Coord.FromMm(aoNode.GetDouble(1) ?? 0));
        }

        var goNode = node.GetChild("grid_origin");
        if (goNode is not null)
        {
            setup.GridOrigin = new CoordPoint(
                Coord.FromMm(goNode.GetDouble(0) ?? 0),
                Coord.FromMm(goNode.GetDouble(1) ?? 0));
        }

        // Parse stackup
        var stackupNode = node.GetChild("stackup");
        if (stackupNode is not null)
        {
            var stackup = new KiCadPcbStackup();
            stackup.CopperFinish = stackupNode.GetChild("copper_finish")?.GetString();
            stackup.DielectricConstraints = stackupNode.GetChild("dielectric_constraints")?.GetBool();
            stackup.EdgeConnector = stackupNode.GetChild("edge_connector")?.GetString();
            stackup.CastellatedPads = stackupNode.GetChild("castellated_pads")?.GetBool();
            stackup.EdgePlating = stackupNode.GetChild("edge_plating")?.GetBool();

            foreach (var layerNode in stackupNode.GetChildren("layer"))
            {
                var layerName = layerNode.GetString(0) ?? "";
                // Check for bare "addsublayer" symbol(s) after the layer name
                var hasAddsublayer = false;
                for (int vi = 1; vi < layerNode.Values.Count; vi++)
                {
                    if (layerNode.Values[vi] is SExprSymbol addSym && addSym.Value == "addsublayer")
                    {
                        hasAddsublayer = true;
                        break;
                    }
                }

                var sl = new KiCadPcbStackupLayer
                {
                    Name = layerName,
                    IsDielectric = layerName.StartsWith("dielectric"),
                    Type = layerNode.GetChild("type")?.GetString(),
                    Color = layerNode.GetChild("color")?.GetString(),
                    Material = layerNode.GetChild("material")?.GetString(),
                };
                var thickNode = layerNode.GetChild("thickness");
                if (thickNode is not null)
                    sl.Thickness = thickNode.GetDouble();
                var erNode = layerNode.GetChild("epsilon_r");
                if (erNode is not null)
                    sl.EpsilonR = erNode.GetDouble();
                var ltNode = layerNode.GetChild("loss_tangent");
                if (ltNode is not null)
                    sl.LossTangent = ltNode.GetDouble();

                // Parse sublayer properties when addsublayer is present.
                // The children list contains the main layer properties first,
                // then sublayer properties (which repeat tokens like thickness, material, etc.).
                if (hasAddsublayer)
                {
                    var seenTokens = new HashSet<string>();
                    KiCadPcbStackupSublayer? currentSublayer = null;
                    foreach (var child in layerNode.Children)
                    {
                        if (!seenTokens.Add(child.Token))
                        {
                            // Duplicate token — start a new sublayer if we haven't yet
                            if (currentSublayer is null || seenTokens.Count == 1)
                            {
                                currentSublayer = new KiCadPcbStackupSublayer();
                                sl.Sublayers.Add(currentSublayer);
                                seenTokens.Clear();
                                seenTokens.Add(child.Token);
                            }
                        }
                        if (currentSublayer is not null)
                        {
                            switch (child.Token)
                            {
                                case "color": currentSublayer.Color = child.GetString(); break;
                                case "thickness": currentSublayer.Thickness = child.GetDouble(); break;
                                case "material": currentSublayer.Material = child.GetString(); break;
                                case "epsilon_r": currentSublayer.EpsilonR = child.GetDouble(); break;
                                case "loss_tangent": currentSublayer.LossTangent = child.GetDouble(); break;
                            }
                        }
                    }
                }

                // Check for dielectric number in the name
                if (sl.IsDielectric && layerName.Contains(' '))
                {
                    var parts = layerName.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var dielNum))
                        sl.DielectricNumber = dielNum;
                }

                stackup.Layers.Add(sl);
            }

            setup.Stackup = stackup;
        }

        // Parse pcbplotparams
        var plotNode = node.GetChild("pcbplotparams");
        if (plotNode is not null)
        {
            var plotParams = new KiCadPcbPlotParams();
            foreach (var paramChild in plotNode.Children)
            {
                var key = paramChild.Token;
                if (paramChild.Values.Count == 0)
                {
                    plotParams.Parameters.Add((key, "", true));
                    continue;
                }
                var firstVal = paramChild.Values[0];
                var val = firstVal switch
                {
                    SExprString s => s.Value,
                    SExprSymbol s => s.Value,
                    SExprNumber n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => ""
                };
                // Numbers and symbols are bare (unquoted), strings are quoted
                var isSymbol = firstVal is not SExprString;
                plotParams.Parameters.Add((key, val, isSymbol));
            }
            setup.PlotParams = plotParams;
        }

        return setup;
    }

    internal static KiCadPcbDimension ParseDimension(SExpr node)
    {
        var dim = new KiCadPcbDimension();

        // Locked
        dim.IsLocked = SExpressionHelper.HasSymbol(node, "locked");
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            dim.IsLocked = lockedChild.GetBool() ?? true;
            dim.LockedIsChildNode = true;
        }

        dim.DimensionType = node.GetChild("type")?.GetString();
        dim.LayerName = node.GetChild("layer")?.GetString();

        var (uuid, uuidIsSymbol) = SExpressionHelper.ParseUuidEx(node);
        dim.Uuid = uuid;
        dim.UuidIsSymbol = uuidIsSymbol;

        dim.Points = SExpressionHelper.ParsePoints(node);
        dim.Height = node.GetChild("height")?.GetDouble();
        dim.Orientation = node.GetChild("orientation")?.GetDouble();
        dim.LeaderLength = node.GetChild("leader_length")?.GetDouble();

        // Format
        var fmtNode = node.GetChild("format");
        if (fmtNode is not null)
        {
            dim.HasFormat = true;
            dim.FormatPrefix = fmtNode.GetChild("prefix")?.GetString();
            dim.FormatSuffix = fmtNode.GetChild("suffix")?.GetString();
            dim.FormatUnits = fmtNode.GetChild("units")?.GetInt();
            dim.FormatUnitsFormat = fmtNode.GetChild("units_format")?.GetInt();
            dim.FormatPrecision = fmtNode.GetChild("precision")?.GetInt();
            dim.FormatOverrideValue = fmtNode.GetChild("override_value")?.GetString();
            dim.FormatSuppressZeroes = fmtNode.GetChild("suppress_zeroes")?.GetBool();
        }

        // Style
        var styleNode = node.GetChild("style");
        if (styleNode is not null)
        {
            dim.HasStyle = true;
            dim.StyleThickness = Coord.FromMm(styleNode.GetChild("thickness")?.GetDouble() ?? 0);
            dim.StyleArrowLength = styleNode.GetChild("arrow_length")?.GetDouble();
            dim.StyleTextPositionMode = styleNode.GetChild("text_position_mode")?.GetInt();
            dim.StyleArrowDirection = styleNode.GetChild("arrow_direction")?.GetString();
            dim.StyleExtensionHeight = styleNode.GetChild("extension_height")?.GetDouble();
            dim.StyleTextFrame = styleNode.GetChild("text_frame")?.GetInt();
            dim.StyleExtensionOffset = styleNode.GetChild("extension_offset")?.GetDouble();
            dim.StyleKeepTextAligned = styleNode.GetChild("keep_text_aligned")?.GetBool();
        }

        // Embedded gr_text
        var grTextNode = node.GetChild("gr_text");
        if (grTextNode is not null)
            dim.Text = ParseGrText(grTextNode);

        return dim;
    }

    private static KiCadPcbGeneratedElement ParseGeneratedElement(SExpr node)
    {
        var gen = new KiCadPcbGeneratedElement
        {
            Uuid = SExpressionHelper.ParseUuid(node),
            GeneratedType = node.GetChild("type")?.GetString(),
            Name = node.GetChild("name")?.GetString(),
            LayerName = node.GetChild("layer")?.GetString()
        };

        // Base line
        var baseLineNode = node.GetChild("base_line");
        if (baseLineNode is not null)
            gen.BaseLinePoints = SExpressionHelper.ParsePoints(baseLineNode);

        // Coupled base line
        var baseCoupledNode = node.GetChild("base_line_coupled");
        if (baseCoupledNode is not null)
            gen.BaseLineCoupledPoints = SExpressionHelper.ParsePoints(baseCoupledNode);

        // Origin and end
        var originNode = node.GetChild("origin");
        if (originNode is not null)
        {
            var xyNode = originNode.GetChild("xy");
            if (xyNode is not null)
                gen.Origin = SExpressionHelper.ParseXY(xyNode);
        }

        var endNode = node.GetChild("end");
        if (endNode is not null)
        {
            var xyNode = endNode.GetChild("xy");
            if (xyNode is not null)
                gen.End = SExpressionHelper.ParseXY(xyNode);
        }

        // Members
        var membersNode = node.GetChild("members");
        if (membersNode is not null)
        {
            foreach (var v in membersNode.Values)
            {
                if (v is SExprString s) gen.Members.Add(s.Value);
                else if (v is SExprSymbol sym) gen.Members.Add(sym.Value);
            }
        }

        // All other simple properties (stored as key-value tuples)
        var knownTokens = new HashSet<string>
        {
            "uuid", "type", "name", "layer", "base_line", "base_line_coupled",
            "origin", "end", "members"
        };

        foreach (var child in node.Children)
        {
            if (knownTokens.Contains(child.Token)) continue;
            if (child.Values.Count > 0)
            {
                var firstVal = child.Values[0];
                var val = firstVal switch
                {
                    SExprString s => s.Value,
                    SExprSymbol s => s.Value,
                    SExprNumber n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => ""
                };
                var isSymbol = firstVal is not SExprString;
                gen.Properties.Add((child.Token, val, isSymbol));
            }
        }

        return gen;
    }
}
