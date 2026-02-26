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
        var root = await SExpressionReader.ReadAsync(path, ct).ConfigureAwait(false);
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
        var root = await SExpressionReader.ReadAsync(stream, ct).ConfigureAwait(false);
        return Parse(root);
    }

    private static KiCadPcb Parse(SExpr root)
    {
        if (root.Token != "kicad_pcb")
            throw new KiCadFileException($"Expected 'kicad_pcb' root token, got '{root.Token}'.");

        var pcb = new KiCadPcb
        {
            Version = root.GetChild("version")?.GetInt() ?? 0,
            Generator = root.GetChild("generator")?.GetString(),
            GeneratorVersion = root.GetChild("generator_version")?.GetString()
        };

        var diagnostics = new List<KiCadDiagnostic>();
        var components = new List<KiCadPcbComponent>();
        var tracks = new List<KiCadPcbTrack>();
        var vias = new List<KiCadPcbVia>();
        var arcs = new List<KiCadPcbArc>();
        var texts = new List<KiCadPcbText>();
        var regions = new List<KiCadPcbRegion>();
        var nets = new List<(int, string)>();

        // Parse setup / board thickness
        var setup = root.GetChild("setup");
        double? boardThicknessMm = setup?.GetChild("board_thickness")?.GetDouble();
        if (boardThicknessMm is null)
        {
            boardThicknessMm = root.GetChild("general")?.GetChild("thickness")?.GetDouble();
        }
        pcb.BoardThickness = Coord.FromMm(boardThicknessMm ?? 1.6);

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
                        var zoneRegions = ParseZone(child);
                        regions.AddRange(zoneRegions);
                        break;
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new KiCadDiagnostic(DiagnosticSeverity.Warning,
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
        pcb.DiagnosticList.AddRange(diagnostics);

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
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked")
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
            IsLocked = node.Values.Any(v => v is SExprSymbol s && s.Value == "locked")
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

        var (fontH, _, _, isHidden, _, isBold, isItalic) = SExpressionHelper.ParseTextEffects(node);
        text.Height = fontH;
        text.FontBold = isBold;
        text.FontItalic = isItalic;
        text.IsHidden = isHidden;

        return text;
    }

    private static List<KiCadPcbRegion> ParseZone(SExpr node)
    {
        var regions = new List<KiCadPcbRegion>();
        var net = node.GetChild("net")?.GetInt() ?? 0;
        var netName = node.GetChild("net_name")?.GetString();
        var layerName = node.GetChild("layer")?.GetString();
        var uuid = SExpressionHelper.ParseUuid(node);
        var priority = node.GetChild("priority")?.GetInt() ?? 0;

        // Parse filled_polygon children
        foreach (var filledPoly in node.GetChildren("filled_polygon"))
        {
            var polyLayer = filledPoly.GetChild("layer")?.GetString() ?? layerName;
            var pts = SExpressionHelper.ParsePoints(filledPoly);
            if (pts.Count > 0)
            {
                regions.Add(new KiCadPcbRegion
                {
                    Outline = pts,
                    LayerName = polyLayer,
                    Net = net,
                    NetName = netName,
                    Uuid = uuid,
                    Priority = priority
                });
            }
        }

        // Also parse polygon (zone outline) if no filled polygons
        if (regions.Count == 0)
        {
            var polygon = node.GetChild("polygon");
            if (polygon is not null)
            {
                var pts = SExpressionHelper.ParsePoints(polygon);
                if (pts.Count > 0)
                {
                    regions.Add(new KiCadPcbRegion
                    {
                        Outline = pts,
                        LayerName = layerName,
                        Net = net,
                        NetName = netName,
                        Uuid = uuid,
                        Priority = priority
                    });
                }
            }
        }

        return regions;
    }
}
