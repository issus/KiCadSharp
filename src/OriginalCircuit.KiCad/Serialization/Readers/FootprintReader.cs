using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models;
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
            Name = node.GetString() ?? "",
            RootToken = token
        };

        // Standalone .kicad_mod file metadata
        var versionNode = node.GetChild("version");
        if (versionNode is not null)
            component.Version = versionNode.GetInt();

        var generatorNode = node.GetChild("generator");
        component.Generator = generatorNode?.GetString();
        component.GeneratorIsSymbol = generatorNode?.Values.Count > 0 && generatorNode.Values[0] is SExprSymbol;
        component.GeneratorVersion = node.GetChild("generator_version")?.GetString();

        var (loc, angle) = SExpressionHelper.ParsePosition(node);
        component.Location = loc;
        component.Rotation = angle;

        component.LayerName = node.GetChild("layer")?.GetString();
        component.Description = node.GetChild("descr")?.GetString();
        component.Tags = node.GetChild("tags")?.GetString();
        component.Path = node.GetChild("path")?.GetString();

        var (uuid, uuidToken) = SExpressionHelper.ParseUuidWithToken(node);
        component.Uuid = uuid;
        bool uuidIsSymbol = false;
        // If the root has no uuid/tstamp, detect from first child that has one
        if (uuid is null)
        {
            foreach (var child in node.Children)
            {
                var childUuid = child.GetChild("tstamp") ?? child.GetChild("uuid");
                if (childUuid is not null)
                {
                    uuidToken = childUuid.Token;
                    uuidIsSymbol = childUuid.Values.Count > 0 && childUuid.Values[0] is SExprSymbol;
                    break;
                }
            }
        }
        else
        {
            var rootUuidNode = node.GetChild("uuid") ?? node.GetChild("tstamp");
            uuidIsSymbol = rootUuidNode?.Values.Count > 0 && rootUuidNode.Values[0] is SExprSymbol;
        }
        component.UuidToken = uuidToken;
        component.UuidIsSymbol = uuidIsSymbol;

        // KiCad 8+ tokens
        component.EmbeddedFonts = node.GetChild("embedded_fonts") is { } efNode ? efNode.GetBool() : null;
        var dupPadNode = node.GetChild("duplicate_pad_numbers_are_jumpers");
        if (dupPadNode is not null)
            component.DuplicatePadNumbersAreJumpers = dupPadNode.GetBool() ?? true;

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
                        case "allow_missing_courtyard": attrs |= FootprintAttribute.AllowMissingCourtyard; break;
                        case "dnp": attrs |= FootprintAttribute.Dnp; break;
                        case "allow_soldermask_bridges": attrs |= FootprintAttribute.AllowSoldermaskBridges; break;
                    }
                }
            }
            component.Attributes = attrs;
        }

        // Parse locked and placed flags (bare symbol or child node format)
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
        // Also check for (locked yes) child node format (KiCad 9+)
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            component.IsLocked = lockedChild.GetBool() ?? true;
            component.UsesChildNodeFlags = true;
        }
        var placedChild = node.GetChild("placed");
        if (placedChild is not null)
        {
            component.IsPlaced = placedChild.GetBool() ?? true;
            component.UsesChildNodeFlags = true;
        }

        // Parse clearance, solder mask margin, etc.
        var clearanceNode = node.GetChild("clearance");
        if (clearanceNode is not null)
        {
            component.Clearance = Coord.FromMm(clearanceNode.GetDouble() ?? 0);
            component.HasClearance = true;
        }
        var solderMaskNode = node.GetChild("solder_mask_margin");
        if (solderMaskNode is not null)
        {
            component.SolderMaskMargin = Coord.FromMm(solderMaskNode.GetDouble() ?? 0);
            component.HasSolderMaskMargin = true;
        }
        var solderPasteNode = node.GetChild("solder_paste_margin");
        if (solderPasteNode is not null)
        {
            component.SolderPasteMargin = Coord.FromMm(solderPasteNode.GetDouble() ?? 0);
            component.HasSolderPasteMargin = true;
        }
        component.SolderPasteRatio = node.GetChild("solder_paste_ratio")?.GetDouble() ?? 0;
        var pasteMarginRatioNode = node.GetChild("solder_paste_margin_ratio");
        if (pasteMarginRatioNode is not null)
            component.SolderPasteMarginRatio = pasteMarginRatioNode.GetDouble();
        var thermalWidthNode = node.GetChild("thermal_width");
        if (thermalWidthNode is not null)
        {
            component.ThermalWidth = Coord.FromMm(thermalWidthNode.GetDouble() ?? 0);
            component.HasThermalWidth = true;
        }
        var thermalGapNode = node.GetChild("thermal_gap");
        if (thermalGapNode is not null)
        {
            component.ThermalGap = Coord.FromMm(thermalGapNode.GetDouble() ?? 0);
            component.HasThermalGap = true;
        }

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
                        var fpText = ParseFpText(child);
                        texts.Add(fpText);
                        component.GraphicalItemOrderList.Add(fpText);
                        break;
                    case "fp_text_private":
                    case "fp_text_box":
                        break;
                    case "fp_line":
                        var line = ParseFpLine(child);
                        tracks.Add(line);
                        component.GraphicalItemOrderList.Add(line);
                        break;
                    case "fp_arc":
                        var arc = ParseFpArc(child);
                        arcs.Add(arc);
                        component.GraphicalItemOrderList.Add(arc);
                        break;
                    case "fp_circle":
                        var circle = ParseFpCircle(child);
                        circles.Add(circle);
                        component.GraphicalItemOrderList.Add(circle);
                        break;
                    case "fp_rect":
                        var rect = ParseFpRect(child);
                        rectangles.Add(rect);
                        component.GraphicalItemOrderList.Add(rect);
                        break;
                    case "fp_poly":
                        var poly = ParseFpPoly(child);
                        polygons.Add(poly);
                        component.GraphicalItemOrderList.Add(poly);
                        break;
                    case "fp_curve":
                        var curve = ParseFpCurve(child);
                        curves.Add(curve);
                        component.GraphicalItemOrderList.Add(curve);
                        break;
                    case "fp_image":
                    case "image":
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
                            component.Model3DScaleX = scaleNode.GetDouble(0) ?? 1;
                            component.Model3DScaleY = scaleNode.GetDouble(1) ?? 1;
                            component.Model3DScaleZ = scaleNode.GetDouble(2) ?? 1;
                        }
                        var rotateNode = child.GetChild("rotate")?.GetChild("xyz");
                        if (rotateNode is not null)
                        {
                            component.Model3DRotationX = rotateNode.GetDouble(0) ?? 0;
                            component.Model3DRotationY = rotateNode.GetDouble(1) ?? 0;
                            component.Model3DRotationZ = rotateNode.GetDouble(2) ?? 0;
                        }
                        break;
                    case "property":
                        properties.Add(SymLibReader.ParseProperty(child));
                        break;
                    case "net_tie_pad_groups":
                        foreach (var v in child.Values)
                        {
                            if (v is SExprString s) component.NetTiePadGroupList.Add(s.Value);
                        }
                        break;
                    case "private_layers":
                        foreach (var v in child.Values)
                        {
                            if (v is SExprString s) component.PrivateLayerList.Add(s.Value);
                            else if (v is SExprSymbol sym) component.PrivateLayerList.Add(sym.Value);
                        }
                        break;
                    case "group":
                        component.GroupList.Add(ParseGroup(child));
                        break;
                    case "component_classes":
                        foreach (var classChild in child.GetChildren("class"))
                        {
                            var className = classChild.GetString();
                            if (className is not null)
                                component.ComponentClassList.Add(className);
                        }
                        break;
                    case "embedded_files":
                        component.EmbeddedFiles = ParseEmbeddedFiles(child);
                        break;
                    case "zone":
                        component.ZoneList.Add(PcbReader.ParseZone(child));
                        break;
                    case "teardrop":
                        // Not yet fully implemented
                        break;
                    case "dimension":
                        component.DimensionList.Add(PcbReader.ParseDimension(child));
                        break;
                    case "sheetname":
                        component.SheetName = child.GetString();
                        break;
                    case "sheetfile":
                        component.SheetFile = child.GetString();
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
                    case "locked":
                    case "placed":
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
        var rrNode = node.GetChild("roundrect_rratio");
        if (rrNode is not null)
        {
            var rrRatio = rrNode.GetDouble() ?? 0;
            pad.HasRoundRectRatio = true;
            pad.RoundRectRatio = rrRatio;
            pad.CornerRadiusPercentage = (int)(rrRatio * 100);
        }

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
            pad.HasZoneConnect = true;
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

        // Thermal bridge width and angle
        pad.ThermalBridgeWidth = Coord.FromMm(node.GetChild("thermal_bridge_width")?.GetDouble() ?? 0);
        var tbAngleNode = node.GetChild("thermal_bridge_angle");
        if (tbAngleNode is not null)
        {
            pad.HasThermalBridgeAngle = true;
            pad.ThermalBridgeAngle = tbAngleNode.GetDouble() ?? 0;
        }

        // Pad locked flag (bare symbol format)
        foreach (var v in node.Values)
        {
            if (v is SExprSymbol s && s.Value == "locked")
            {
                pad.IsLocked = true;
                break;
            }
        }
        // Also check for (locked yes) child node format (KiCad 9+)
        var padLockedChild = node.GetChild("locked");
        if (padLockedChild is not null)
        {
            pad.IsLocked = padLockedChild.GetBool() ?? true;
            pad.LockedIsChildNode = true;
        }

        // Remove unused layers and keep end layers (KiCad 8+ uses boolean value: yes/no)
        var removeUnusedNode = node.GetChild("remove_unused_layers");
        if (removeUnusedNode is not null)
        {
            pad.HasRemoveUnusedLayers = true;
            pad.RemoveUnusedLayers = removeUnusedNode.GetBool() ?? true;
        }
        var keepEndNode = node.GetChild("keep_end_layers");
        if (keepEndNode is not null)
        {
            pad.HasKeepEndLayers = true;
            pad.KeepEndLayers = keepEndNode.GetBool() ?? true;
        }

        pad.Uuid = SExpressionHelper.ParseUuid(node);

        // Rect delta (trapezoidal pads)
        var rectDeltaNode = node.GetChild("rect_delta");
        if (rectDeltaNode is not null)
        {
            pad.RectDelta = new CoordPoint(
                Coord.FromMm(rectDeltaNode.GetDouble(0) ?? 0),
                Coord.FromMm(rectDeltaNode.GetDouble(1) ?? 0));
        }

        // Custom pad options
        var optionsNode = node.GetChild("options");
        if (optionsNode is not null)
        {
            pad.CustomClearanceType = optionsNode.GetChild("clearance")?.GetString();
            pad.CustomAnchorShape = optionsNode.GetChild("anchor")?.GetString();
        }

        // Custom pad primitives
        var primitivesNode = node.GetChild("primitives");
        if (primitivesNode is not null)
        {
            foreach (var primChild in primitivesNode.Children)
            {
                switch (primChild.Token)
                {
                    case "gr_line":
                        pad.Primitives.Add(ParseFpLine(primChild));
                        break;
                    case "gr_circle":
                        pad.Primitives.Add(ParseFpCircle(primChild));
                        break;
                    case "gr_rect":
                        pad.Primitives.Add(ParseFpRect(primChild));
                        break;
                    case "gr_arc":
                        pad.Primitives.Add(ParseFpArc(primChild));
                        break;
                    case "gr_poly":
                        pad.Primitives.Add(ParseFpPoly(primChild));
                        break;
                    case "gr_curve":
                        pad.Primitives.Add(ParseFpCurve(primChild));
                        break;
                }
            }
        }

        // Teardrops
        var padTeardropNode = node.GetChild("teardrops") ?? node.GetChild("teardrop");
        if (padTeardropNode is not null)
        {
            pad.HasTeardrops = true;
            pad.TeardropEnabled = padTeardropNode.GetChild("enabled")?.GetBool();
            pad.TeardropBestLengthRatio = padTeardropNode.GetChild("best_length_ratio")?.GetDouble();
            var maxLenNode = padTeardropNode.GetChild("max_length");
            if (maxLenNode is not null)
                pad.TeardropMaxLength = Coord.FromMm(maxLenNode.GetDouble() ?? 0);
            pad.TeardropBestWidthRatio = padTeardropNode.GetChild("best_width_ratio")?.GetDouble();
            var maxWidthNode = padTeardropNode.GetChild("max_width");
            if (maxWidthNode is not null)
                pad.TeardropMaxWidth = Coord.FromMm(maxWidthNode.GetDouble() ?? 0);
            pad.TeardropCurvedEdges = padTeardropNode.GetChild("curved_edges")?.GetBool();
            pad.TeardropFilterRatio = padTeardropNode.GetChild("filter_ratio")?.GetDouble();
            pad.TeardropAllowTwoSegments = padTeardropNode.GetChild("allow_two_segments")?.GetBool();
            pad.TeardropPreferZoneConnections = padTeardropNode.GetChild("prefer_zone_connections")?.GetBool();
        }

        // Tenting
        var padTentingNode = node.GetChild("tenting");
        if (padTentingNode is not null)
        {
            pad.HasTenting = true;
            // Check for child node format: (tenting (front none) (back none))
            pad.TentingFront = padTentingNode.GetChild("front")?.GetString();
            pad.TentingBack = padTentingNode.GetChild("back")?.GetString();
            // Also check for bare symbols: (tenting front back)
            if (pad.TentingFront is null && pad.TentingBack is null)
            {
                foreach (var v in padTentingNode.Values)
                {
                    if (v is SExprSymbol sym)
                    {
                        if (sym.Value == "front") pad.TentingFront = "front";
                        else if (sym.Value == "back") pad.TentingBack = "back";
                    }
                }
            }
        }

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

        // Detect whether the at node included the angle value or unlocked keyword
        var atNode = node.GetChild("at");
        if (atNode is not null)
        {
            // Count numeric values (x, y, and optionally angle)
            int numericCount = 0;
            foreach (var v in atNode.Values)
            {
                if (v is SExprSymbol sym && sym.Value == "unlocked")
                {
                    text.IsUnlocked = true;
                    text.UnlockedInAtNode = true;
                }
                else
                {
                    numericCount++;
                }
            }
            text.PositionIncludesAngle = numericCount >= 3;
        }

        // Detect child ordering: does uuid/tstamp come after effects?
        var effectsIdx = -1;
        var uuidIdx = -1;
        for (int i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i].Token == "effects") effectsIdx = i;
            else if (node.Children[i].Token is "uuid" or "tstamp") uuidIdx = i;
        }
        text.UuidAfterEffects = effectsIdx >= 0 && uuidIdx > effectsIdx;

        var layerNode = node.GetChild("layer");
        text.LayerName = layerNode?.GetString();

        // Check for knockout on layer node: (layer "F.SilkS" knockout)
        if (layerNode is not null)
        {
            foreach (var v in layerNode.Values)
            {
                if (v is SExprSymbol sym && sym.Value == "knockout")
                {
                    text.IsKnockout = true;
                    break;
                }
            }
        }

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

        // Also check for (hide yes) and (unlocked yes) child node format (KiCad 9+)
        var hideChild = node.GetChild("hide");
        if (hideChild is not null)
        {
            text.IsHidden = hideChild.GetBool() ?? true;
            text.HideIsChildNode = true;
        }

        var unlockedChild = node.GetChild("unlocked");
        if (unlockedChild is not null)
        {
            text.IsUnlocked = unlockedChild.GetBool() ?? true;
            text.UnlockedIsChildNode = true;
        }

        var (fontH, fontW, justification, _, isMirrored, isBold, isItalic, fontFace, fontThickness, fontColor, _, boldIsSymbol, italicIsSymbol) = SExpressionHelper.ParseTextEffectsEx(node);
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

        text.Uuid = SExpressionHelper.ParseUuid(node);

        // Parse render_cache
        var renderCacheNode = node.GetChild("render_cache");
        if (renderCacheNode is not null)
        {
            var rc = new KiCadTextRenderCache
            {
                FontName = renderCacheNode.GetString(0),
            };
            // Second value is the font size/rotation
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

        var (fillType, _, fillColor, usePcbFillFmt) = SExpressionHelper.ParseFillWithFormat(node);

        var track = new KiCadPcbTrack
        {
            Start = start,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            UsePcbFillFormat = usePcbFillFmt,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            track.IsLocked = lockedChild.GetBool() ?? true;
            track.LockedIsChildNode = true;
        }

        return track;
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

        var (fillType, _, fillColor, usePcbFillFmt) = SExpressionHelper.ParseFillWithFormat(node);

        var circle = new KiCadPcbCircle
        {
            Center = center,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            UsePcbFillFormat = usePcbFillFmt,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            circle.IsLocked = lockedChild.GetBool() ?? true;
            circle.LockedIsChildNode = true;
        }

        return circle;
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

        var (fillType, _, fillColor, usePcbFillFmt) = SExpressionHelper.ParseFillWithFormat(node);

        var rect = new KiCadPcbRectangle
        {
            Start = start,
            End = end,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            UsePcbFillFormat = usePcbFillFmt,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            rect.IsLocked = lockedChild.GetBool() ?? true;
            rect.LockedIsChildNode = true;
        }

        return rect;
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

        var arc = new KiCadPcbArc
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

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            arc.IsLocked = lockedChild.GetBool() ?? true;
            arc.LockedIsChildNode = true;
        }

        return arc;
    }

    private static KiCadPcbPolygon ParseFpPoly(SExpr node)
    {
        var points = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);
        var (fillType, _, fillColor, usePcbFillFmt) = SExpressionHelper.ParseFillWithFormat(node);

        var poly = new KiCadPcbPolygon
        {
            Points = points,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            FillType = fillType,
            FillColor = fillColor,
            UsePcbFillFormat = usePcbFillFmt,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            poly.IsLocked = lockedChild.GetBool() ?? true;
            poly.LockedIsChildNode = true;
        }

        return poly;
    }

    private static KiCadPcbCurve ParseFpCurve(SExpr node)
    {
        var points = SExpressionHelper.ParsePoints(node);
        var (width, style, color) = SExpressionHelper.ParseStroke(node);
        if (width == Coord.Zero)
            width = Coord.FromMm(node.GetChild("width")?.GetDouble() ?? 0);

        var curve = new KiCadPcbCurve
        {
            Points = points,
            Width = width,
            StrokeStyle = style,
            StrokeColor = color,
            LayerName = node.GetChild("layer")?.GetString(),
            IsLocked = SExpressionHelper.HasSymbol(node, "locked"),
            Uuid = SExpressionHelper.ParseUuid(node)
        };

        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            curve.IsLocked = lockedChild.GetBool() ?? true;
            curve.LockedIsChildNode = true;
        }

        return curve;
    }

    private static KiCadPcb3DModel Parse3DModel(SExpr node)
    {
        var model = new KiCadPcb3DModel
        {
            Path = node.GetString() ?? ""
        };

        // Parse hide flag
        var hideNode = node.GetChild("hide");
        if (hideNode is not null)
        {
            // (hide yes) or bare (hide)
            var hideVal = hideNode.GetString();
            model.IsHidden = hideVal is null || hideVal == "yes";
        }

        // Parse opacity
        var opacityNode = node.GetChild("opacity");
        if (opacityNode is not null)
            model.Opacity = opacityNode.GetDouble();

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
            model.ScaleX = scaleNode.GetDouble(0) ?? 1;
            model.ScaleY = scaleNode.GetDouble(1) ?? 1;
            model.ScaleZ = scaleNode.GetDouble(2) ?? 1;
        }

        var rotateNode = node.GetChild("rotate")?.GetChild("xyz");
        if (rotateNode is not null)
        {
            model.RotationX = rotateNode.GetDouble(0) ?? 0;
            model.RotationY = rotateNode.GetDouble(1) ?? 0;
            model.RotationZ = rotateNode.GetDouble(2) ?? 0;
        }

        return model;
    }

    internal static KiCadPcbGroup ParseGroup(SExpr node)
    {
        var group = new KiCadPcbGroup
        {
            Name = node.GetString() ?? "",
            Id = SExpressionHelper.ParseUuid(node)
        };

        // Parse locked
        group.IsLocked = SExpressionHelper.HasSymbol(node, "locked");
        var lockedChild = node.GetChild("locked");
        if (lockedChild is not null)
        {
            group.IsLocked = lockedChild.GetBool() ?? true;
            group.LockedIsChildNode = true;
        }

        // Parse members
        var membersNode = node.GetChild("members");
        if (membersNode is not null)
        {
            foreach (var v in membersNode.Values)
            {
                if (v is SExprString s) group.Members.Add(s.Value);
                else if (v is SExprSymbol sym) group.Members.Add(sym.Value);
            }
        }

        return group;
    }

    internal static KiCadEmbeddedFiles ParseEmbeddedFiles(SExpr node)
    {
        var embedded = new KiCadEmbeddedFiles();
        foreach (var fileNode in node.GetChildren("file"))
        {
            var file = new KiCadEmbeddedFile
            {
                Name = fileNode.GetChild("name")?.GetString() ?? "",
                Type = fileNode.GetChild("type")?.GetString() ?? "",
                Checksum = fileNode.GetChild("checksum")?.GetString(),
            };

            // Data may contain multiple string values (one per line in the file)
            var dataNode = fileNode.GetChild("data");
            if (dataNode is not null)
            {
                var hasSymbol = false;
                foreach (var v in dataNode.Values)
                {
                    if (v is SExprString s) file.DataSegments.Add(s.Value);
                    else if (v is SExprSymbol sym) { file.DataSegments.Add(sym.Value); hasSymbol = true; }
                }
                file.DataSegmentsAreSymbols = hasSymbol;
            }

            embedded.Files.Add(file);
        }
        return embedded;
    }
}
