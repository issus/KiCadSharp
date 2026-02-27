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

        // Standalone .kicad_mod file metadata
        var versionNode = node.GetChild("version");
        if (versionNode is not null)
            component.Version = versionNode.GetInt();

        component.Generator = node.GetChild("generator")?.GetString();
        component.GeneratorVersion = node.GetChild("generator_version")?.GetString();

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        component.Location = loc;
        component.Rotation = angle;

        component.LayerName = node.GetChild("layer")?.GetString();
        component.Description = node.GetChild("descr")?.GetString();
        component.Tags = node.GetChild("tags")?.GetString();
        component.Path = node.GetChild("path")?.GetString();
        component.Uuid = SExpressionHelper.ParseUuid(node);

        // KiCad 8+ tokens
        component.EmbeddedFonts = node.GetChild("embedded_fonts")?.GetBool() ?? false;
        component.DuplicatePadNumbersAreJumpers = node.GetChild("duplicate_pad_numbers_are_jumpers") is not null;

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

        // Parse locked and placed flags
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s)
            {
                if (s.Value == "locked")
                    component.IsLocked = true;
                else if (s.Value == "placed")
                    component.IsPlaced = true;
            }
        }

        // Parse clearance, solder mask margin, etc.
        component.Clearance = Coord.FromMm(node.GetChild("clearance")?.GetDouble() ?? 0);
        component.SolderMaskMargin = Coord.FromMm(node.GetChild("solder_mask_margin")?.GetDouble() ?? 0);
        component.SolderPasteMargin = Coord.FromMm(node.GetChild("solder_paste_margin")?.GetDouble() ?? 0);
        component.SolderPasteRatio = node.GetChild("solder_paste_ratio")?.GetDouble() ?? 0;
        var pasteMarginRatioNode = node.GetChild("solder_paste_margin_ratio");
        if (pasteMarginRatioNode is not null)
            component.SolderPasteMarginRatio = pasteMarginRatioNode.GetDouble();
        component.ThermalWidth = Coord.FromMm(node.GetChild("thermal_width")?.GetDouble() ?? 0);
        component.ThermalGap = Coord.FromMm(node.GetChild("thermal_gap")?.GetDouble() ?? 0);

        // Parse zone_connect
        var zcNode = node.GetChild("zone_connect");
        if (zcNode is not null)
        {
            var zcVal = zcNode.GetInt() ?? 0;
            component.ZoneConnect = Enum.IsDefined(typeof(ZoneConnectionType), zcVal)
                ? (ZoneConnectionType)zcVal
                : ZoneConnectionType.Inherited;
        }

        // Parse autoplace cost
        component.AutoplaceCost90 = node.GetChild("autoplace_cost90")?.GetInt() ?? 0;
        component.AutoplaceCost180 = node.GetChild("autoplace_cost180")?.GetInt() ?? 0;

        // Parse tedit
        component.Tedit = node.GetChild("tedit")?.GetString();

        var diagnostics = new List<KiCadDiagnostic>();
        var pads = new List<KiCadPcbPad>();
        var texts = new List<KiCadPcbText>();
        var tracks = new List<KiCadPcbTrack>();
        var arcs = new List<KiCadPcbArc>();
        var rectangles = new List<KiCadPcbRectangle>();
        var circles = new List<KiCadPcbCircle>();
        var polygons = new List<KiCadPcbPolygon>();
        var curves = new List<KiCadPcbCurve>();
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
                    case "fp_text_private":
                        component.TextPrivateRaw.Add(child);
                        break;
                    case "fp_text_box":
                        component.TextBoxesRaw.Add(child);
                        break;
                    case "fp_line":
                        tracks.Add(ParseFpLine(child));
                        break;
                    case "fp_arc":
                        arcs.Add(ParseFpArc(child));
                        break;
                    case "fp_circle":
                        circles.Add(ParseFpCircle(child));
                        break;
                    case "fp_rect":
                        rectangles.Add(ParseFpRect(child));
                        break;
                    case "fp_poly":
                        polygons.Add(ParseFpPoly(child));
                        break;
                    case "fp_curve":
                        curves.Add(ParseFpCurve(child));
                        break;
                    case "model":
                        component.Model3DList.Add(Parse3DModel(child));
                        // Also update legacy single-model properties for backward compatibility
                        component.Model3D = child.GetString();
                        var offsetNode = child.GetChild("offset")?.GetChild("xyz");
                        if (offsetNode is not null)
                        {
                            component.Model3DOffset = new CoordPoint(
                                Coord.FromMm(offsetNode.GetDouble(0) ?? 0),
                                Coord.FromMm(offsetNode.GetDouble(1) ?? 0));
                            component.Model3DOffsetZ = offsetNode.GetDouble(2) ?? 0;
                        }
                        var scaleNode = child.GetChild("scale")?.GetChild("xyz");
                        if (scaleNode is not null)
                        {
                            component.Model3DScale = new CoordPoint(
                                Coord.FromMm(scaleNode.GetDouble(0) ?? 1),
                                Coord.FromMm(scaleNode.GetDouble(1) ?? 1));
                            component.Model3DScaleZ = scaleNode.GetDouble(2) ?? 1;
                        }
                        var rotateNode = child.GetChild("rotate")?.GetChild("xyz");
                        if (rotateNode is not null)
                        {
                            component.Model3DRotation = new CoordPoint(
                                Coord.FromMm(rotateNode.GetDouble(0) ?? 0),
                                Coord.FromMm(rotateNode.GetDouble(1) ?? 0));
                            component.Model3DRotationZ = rotateNode.GetDouble(2) ?? 0;
                        }
                        break;
                    case "property":
                        properties.Add(SymLibReader.ParseProperty(child));
                        break;
                    case "teardrop":
                        component.TeardropRaw = child;
                        break;
                    case "net_tie_pad_groups":
                        component.NetTiePadGroupsRaw = child;
                        break;
                    case "private_layers":
                        component.PrivateLayersRaw = child;
                        break;
                    case "zone":
                        component.ZonesRaw.Add(child);
                        break;
                    case "group":
                        component.GroupsRaw.Add(child);
                        break;
                    case "layer":
                    case "descr":
                    case "tags":
                    case "path":
                    case "uuid":
                    case "tstamp":
                    case "tedit":
                    case "at":
                    case "attr":
                    case "clearance":
                    case "solder_mask_margin":
                    case "solder_paste_margin":
                    case "solder_paste_ratio":
                    case "solder_paste_margin_ratio":
                    case "thermal_width":
                    case "thermal_gap":
                    case "zone_connect":
                    case "autoplace_cost90":
                    case "autoplace_cost180":
                    case "version":
                    case "generator":
                    case "generator_version":
                    case "embedded_fonts":
                    case "duplicate_pad_numbers_are_jumpers":
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
        component.RectangleList.AddRange(rectangles);
        component.CircleList.AddRange(circles);
        component.PolygonList.AddRange(polygons);
        component.CurveList.AddRange(curves);
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
                // Second diameter for oval
                pad.DrillSizeY = Coord.FromMm(drillNode.GetDouble(2) ?? pad.HoleSize.ToMm());
            }
            else
            {
                pad.HoleSize = Coord.FromMm(drillNode.GetDouble(0) ?? 0);
            }

            // Drill offset
            var drillOffsetNode = drillNode.GetChild("offset");
            if (drillOffsetNode is not null)
            {
                pad.DrillOffset = new CoordPoint(
                    Coord.FromMm(drillOffsetNode.GetDouble(0) ?? 0),
                    Coord.FromMm(drillOffsetNode.GetDouble(1) ?? 0));
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
            var zcVal = zcNode.GetInt() ?? 0;
            pad.ZoneConnect = Enum.IsDefined(typeof(ZoneConnectionType), zcVal)
                ? (ZoneConnectionType)zcVal
                : ZoneConnectionType.Inherited;
        }

        pad.PinFunction = node.GetChild("pinfunction")?.GetString();
        pad.PinType = node.GetChild("pintype")?.GetString();
        pad.DieLength = Coord.FromMm(node.GetChild("die_length")?.GetDouble() ?? 0);

        // Chamfer
        pad.ChamferRatio = node.GetChild("chamfer_ratio")?.GetDouble() ?? 0;
        var chamferNode = node.GetChild("chamfer");
        if (chamferNode is not null)
        {
            var corners = new List<string>();
            foreach (var v in chamferNode.Values)
            {
                if (v is SExprSymbol sym)
                    corners.Add(sym.Value);
            }
            pad.ChamferCorners = corners.ToArray();
        }

        // Pad property
        pad.PadProperty = node.GetChild("property")?.GetString();

        // Thermal bridge angle
        pad.ThermalBridgeAngle = node.GetChild("thermal_bridge_angle")?.GetDouble() ?? 0;

        // Custom pad primitives
        var primitivesNode = node.GetChild("primitives");
        if (primitivesNode is not null)
            pad.PrimitivesRaw = primitivesNode;

        // Pad locked flag
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "locked")
            {
                pad.IsLocked = true;
                break;
            }
        }

        // Remove unused layers and keep end layers
        pad.RemoveUnusedLayers = node.GetChild("remove_unused_layers") is not null;
        pad.KeepEndLayers = node.GetChild("keep_end_layers") is not null;

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

        // Check for hide and unlocked symbols
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s)
            {
                switch (s.Value)
                {
                    case "hide": text.IsHidden = true; break;
                    case "unlocked": text.IsUnlocked = true; break;
                }
            }
        }

        var (fontH, fontW, justification, _, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor) = SExpressionHelper.ParseTextEffects(node);
        text.Height = fontH;
        text.FontWidth = fontW;
        text.FontBold = isBold;
        text.FontItalic = isItalic;
        text.FontName = fontFace;
        text.FontThickness = fontThickness;
        text.FontColor = fontColor;
        text.Justification = justification;
        text.IsMirrored = isMirrored;

        // Render cache (raw preservation)
        var renderCache = node.GetChild("render_cache");
        if (renderCache is not null)
            text.RenderCache = renderCache;

        text.Uuid = SExpressionHelper.ParseUuid(node);

        return text;
    }

    private static KiCadPcbTrack ParseFpLine(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, style, color) = SExpressionHelper.ParseStroke(node);

        // If no stroke, try legacy width
        if (width == Coord.Zero)
        {
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);
        }

        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadPcbTrack
        {
            Start = start,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcbCircle ParseFpCircle(SExpr node)
    {
        var centerNode = node.GetChild("center");
        var endNode = node.GetChild("end");
        var center = centerNode is not null ? SExpressionHelper.ParseXY(centerNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadPcbCircle
        {
            Center = center,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcbRectangle ParseFpRect(SExpr node)
    {
        var startNode = node.GetChild("start");
        var endNode = node.GetChild("end");
        var start = startNode is not null ? SExpressionHelper.ParseXY(startNode) : CoordPoint.Zero;
        var end = endNode is not null ? SExpressionHelper.ParseXY(endNode) : CoordPoint.Zero;
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadPcbRectangle
        {
            Start = start,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
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
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
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
            StrokeStyle = style,
            StrokeColor = color,
            LayerName = node.GetChild("layer")?.GetString(),
            ArcStart = start,
            ArcMid = mid,
            ArcEnd = end,
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcbPolygon ParseFpPoly(SExpr node)
    {
        var points = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);
        var (fillType, _, fillColor) = SExpressionHelper.ParseFill(node);

        return new KiCadPcbPolygon
        {
            Points = points,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcbCurve ParseFpCurve(SExpr node)
    {
        var points = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        return new KiCadPcbCurve
        {
            Points = points,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };
    }

    private static KiCadPcb3DModel Parse3DModel(SExpr node)
    {
        var model = new KiCadPcb3DModel
        {
            Path = node.GetString() ?? ""
        };

        var offsetNode = node.GetChild("offset")?.GetChild("xyz");
        if (offsetNode is not null)
        {
            model.Offset = new CoordPoint(
                Coord.FromMm(offsetNode.GetDouble(0) ?? 0),
                Coord.FromMm(offsetNode.GetDouble(1) ?? 0));
            model.OffsetZ = offsetNode.GetDouble(2) ?? 0;
        }

        var scaleNode = node.GetChild("scale")?.GetChild("xyz");
        if (scaleNode is not null)
        {
            model.Scale = new CoordPoint(
                Coord.FromMm(scaleNode.GetDouble(0) ?? 1),
                Coord.FromMm(scaleNode.GetDouble(1) ?? 1));
            model.ScaleZ = scaleNode.GetDouble(2) ?? 1;
        }

        var rotateNode = node.GetChild("rotate")?.GetChild("xyz");
        if (rotateNode is not null)
        {
            model.Rotation = new CoordPoint(
                Coord.FromMm(rotateNode.GetDouble(0) ?? 0),
                Coord.FromMm(rotateNode.GetDouble(1) ?? 0));
            model.RotationZ = rotateNode.GetDouble(2) ?? 0;
        }

        return model;
    }
}
