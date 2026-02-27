using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic component (symbol definition). Contains sub-symbols (units), pins, and graphical items.
/// </summary>
public sealed class KiCadSchComponent : ISchComponent
{
    private readonly List<KiCadSchPin> _pins = [];
    private readonly List<KiCadSchLine> _lines = [];
    private readonly List<KiCadSchRectangle> _rectangles = [];
    private readonly List<KiCadSchLabel> _labels = [];
    private readonly List<KiCadSchWire> _wires = [];
    private readonly List<KiCadSchPolyline> _polylines = [];
    private readonly List<KiCadSchPolygon> _polygons = [];
    private readonly List<KiCadSchArc> _arcs = [];
    private readonly List<KiCadSchCircle> _circles = [];
    private readonly List<KiCadSchBezier> _beziers = [];
    private readonly List<KiCadSchNetLabel> _netLabels = [];
    private readonly List<KiCadSchJunction> _junctions = [];
    private readonly List<KiCadSchImage> _images = [];
    private readonly List<KiCadSchParameter> _parameters = [];
    private readonly List<KiCadSchComponent> _subSymbols = [];
    private readonly List<KiCadDiagnostic> _diagnostics = [];
    private readonly List<object> _orderedPrimitives = [];

    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public int PartCount { get; set; } = 1;

    /// <summary>
    /// Gets the unique identifier for this component instance.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets the location of this placed component in the schematic.
    /// </summary>
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Gets the rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Gets whether the component is mirrored along the X axis.
    /// </summary>
    public bool IsMirroredX { get; set; }

    /// <summary>
    /// Gets whether the component is mirrored along the Y axis.
    /// </summary>
    public bool IsMirroredY { get; set; }

    /// <summary>
    /// Gets the unit number for multi-unit symbols.
    /// </summary>
    public int Unit { get; set; } = 1;

    /// <inheritdoc />
    public IReadOnlyList<ISchPin> Pins => _pins;
    internal List<KiCadSchPin> PinList => _pins;

    /// <inheritdoc />
    public IReadOnlyList<ISchLine> Lines => _lines;
    internal List<KiCadSchLine> LineList => _lines;

    /// <inheritdoc />
    public IReadOnlyList<ISchRectangle> Rectangles => _rectangles;
    internal List<KiCadSchRectangle> RectangleList => _rectangles;

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels => _labels;
    internal List<KiCadSchLabel> LabelList => _labels;

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires => _wires;
    internal List<KiCadSchWire> WireList => _wires;

    /// <inheritdoc />
    public IReadOnlyList<ISchPolyline> Polylines => _polylines;
    internal List<KiCadSchPolyline> PolylineList => _polylines;

    /// <inheritdoc />
    public IReadOnlyList<ISchPolygon> Polygons => _polygons;
    internal List<KiCadSchPolygon> PolygonList => _polygons;

    /// <inheritdoc />
    public IReadOnlyList<ISchArc> Arcs => _arcs;
    internal List<KiCadSchArc> ArcList => _arcs;

    /// <inheritdoc />
    public IReadOnlyList<ISchCircle> Circles => _circles;
    internal List<KiCadSchCircle> CircleList => _circles;

    /// <inheritdoc />
    public IReadOnlyList<ISchBezier> Beziers => _beziers;
    internal List<KiCadSchBezier> BezierList => _beziers;

    /// <summary>
    /// Gets the ordered list of all graphical primitives in their original file order.
    /// Used to preserve primitive ordering during round-trip.
    /// </summary>
    public IReadOnlyList<object> OrderedPrimitives => _orderedPrimitives;
    internal List<object> OrderedPrimitivesList => _orderedPrimitives;

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels => _netLabels;
    internal List<KiCadSchNetLabel> NetLabelList => _netLabels;

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions => _junctions;
    internal List<KiCadSchJunction> JunctionList => _junctions;

    /// <inheritdoc />
    public IReadOnlyList<ISchImage> Images => _images;
    internal List<KiCadSchImage> ImageList => _images;

    /// <inheritdoc />
    public IReadOnlyList<ISchParameter> Parameters => _parameters;
    internal List<KiCadSchParameter> ParameterList => _parameters;

    /// <summary>
    /// Gets whether the symbol should be included in the BOM.
    /// </summary>
    public bool InBom { get; set; } = true;

    /// <summary>
    /// Gets whether the symbol should be included on the board.
    /// </summary>
    public bool OnBoard { get; set; } = true;

    /// <summary>
    /// Gets or sets the convert/body_style value.
    /// </summary>
    public int BodyStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the source file used <c>body_style</c> (KiCad 9+) instead of <c>convert</c> (KiCad 8).
    /// </summary>
    public bool UseBodyStyleToken { get; set; }

    /// <summary>
    /// Gets or sets whether the <c>duplicate_pin_numbers_are_jumpers</c> flag is present (KiCad 9+).
    /// </summary>
    public bool DuplicatePinNumbersAreJumpersPresent { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>duplicate_pin_numbers_are_jumpers</c> flag.
    /// </summary>
    public bool DuplicatePinNumbersAreJumpers { get; set; }

    /// <summary>
    /// Gets or sets whether the placed symbol's fields are auto-placed.
    /// </summary>
    public bool FieldsAutoplaced { get; set; }

    /// <summary>
    /// Gets or sets the lib_name for placed symbols.
    /// </summary>
    public string? LibName { get; set; }

    /// <summary>
    /// Gets or sets the raw instances S-expression subtree for round-trip fidelity.
    /// </summary>
    public SExpression.SExpression? InstancesRaw { get; set; }

    /// <summary>
    /// Gets or sets whether the symbol should be excluded from simulation (KiCad 8+).
    /// </summary>
    public bool ExcludeFromSim { get; set; }

    /// <summary>
    /// Gets or sets whether this placed symbol is marked as "Do Not Populate" (KiCad 8+).
    /// </summary>
    public bool Dnp { get; set; }

    /// <summary>
    /// Gets or sets whether the dnp node was present in the source file.
    /// </summary>
    public bool DnpPresent { get; set; }

    /// <summary>
    /// Gets or sets whether embedded fonts are used in this symbol (KiCad 8+).
    /// Null means the token was not present in the source file.
    /// </summary>
    public bool? EmbeddedFonts { get; set; }

    /// <summary>
    /// Gets or sets whether this symbol is a power symbol (KiCad 8+).
    /// </summary>
    public bool IsPower { get; set; }

    /// <summary>
    /// Gets or sets the power type value (e.g., "global" in KiCad 9+).
    /// Null when <c>(power)</c> has no value argument.
    /// </summary>
    public string? PowerType { get; set; }

    /// <summary>
    /// Gets the name of the symbol this one extends (derived/inherited symbols).
    /// </summary>
    public string? Extends { get; set; }

    /// <summary>
    /// Gets or sets whether the pin_names node was present in the source file.
    /// Null means not tracked (will always emit for KiCad 8+ files).
    /// </summary>
    public bool PinNamesPresent { get; set; }

    /// <summary>
    /// Gets or sets whether the pin_numbers node was present in the source file.
    /// </summary>
    public bool PinNumbersPresent { get; set; }

    /// <summary>
    /// Gets or sets whether the exclude_from_sim node was present in the source file.
    /// </summary>
    public bool ExcludeFromSimPresent { get; set; }

    /// <summary>
    /// Gets or sets whether the mirror node was present in the source file for this placed symbol.
    /// </summary>
    public bool MirrorPresent { get; set; }

    /// <summary>
    /// Gets the pin names offset from the pin end.
    /// </summary>
    public Coord PinNamesOffset { get; set; }

    /// <summary>
    /// Gets or sets whether the (offset N) child was explicitly present in the pin_names node.
    /// </summary>
    public bool PinNamesHasOffset { get; set; }

    /// <summary>
    /// Gets whether pin numbers are hidden.
    /// </summary>
    public bool HidePinNumbers { get; set; }

    /// <summary>
    /// Gets whether pin names are hidden.
    /// </summary>
    public bool HidePinNames { get; set; }

    /// <summary>
    /// Gets the sub-symbol children (units).
    /// </summary>
    public IReadOnlyList<KiCadSchComponent> SubSymbols => _subSymbols;
    internal List<KiCadSchComponent> SubSymbolList => _subSymbols;

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics => _diagnostics;
    internal List<KiCadDiagnostic> DiagnosticList => _diagnostics;

    /// <summary>Returns true if any diagnostic has Error severity.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc />
    /// <remarks>This property is computed on each access. Cache the result if accessing repeatedly.</remarks>
    public CoordRect Bounds
    {
        get
        {
            var rect = CoordRect.Empty;
            foreach (var pin in Pins) rect = rect.Union(pin.Bounds);
            foreach (var line in Lines) rect = rect.Union(line.Bounds);
            foreach (var r in Rectangles) rect = rect.Union(r.Bounds);
            foreach (var arc in Arcs) rect = rect.Union(arc.Bounds);
            foreach (var circle in Circles) rect = rect.Union(circle.Bounds);
            foreach (var poly in Polylines) rect = rect.Union(poly.Bounds);
            foreach (var poly in Polygons) rect = rect.Union(poly.Bounds);
            foreach (var bez in Beziers) rect = rect.Union(bez.Bounds);
            return rect;
        }
    }

    /// <inheritdoc />
    public void AddPin(ISchPin pin)
    {
        ArgumentNullException.ThrowIfNull(pin);
        if (pin is not KiCadSchPin kpin)
            throw new ArgumentException($"Expected {nameof(KiCadSchPin)}", nameof(pin));
        _pins.Add(kpin);
    }

    /// <inheritdoc />
    public bool RemovePin(ISchPin pin) => pin is KiCadSchPin kpin && _pins.Remove(kpin);

    /// <inheritdoc />
    public void AddLine(ISchLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line is not KiCadSchLine kline)
            throw new ArgumentException($"Expected {nameof(KiCadSchLine)}", nameof(line));
        _lines.Add(kline);
    }

    /// <inheritdoc />
    public bool RemoveLine(ISchLine line) => line is KiCadSchLine kline && _lines.Remove(kline);

    /// <inheritdoc />
    public void AddRectangle(ISchRectangle rectangle)
    {
        ArgumentNullException.ThrowIfNull(rectangle);
        if (rectangle is not KiCadSchRectangle krect)
            throw new ArgumentException($"Expected {nameof(KiCadSchRectangle)}", nameof(rectangle));
        _rectangles.Add(krect);
    }

    /// <inheritdoc />
    public bool RemoveRectangle(ISchRectangle rectangle) => rectangle is KiCadSchRectangle krect && _rectangles.Remove(krect);

    /// <inheritdoc />
    public void AddLabel(ISchLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        if (label is not KiCadSchLabel klabel)
            throw new ArgumentException($"Expected {nameof(KiCadSchLabel)}", nameof(label));
        _labels.Add(klabel);
    }

    /// <inheritdoc />
    public bool RemoveLabel(ISchLabel label) => label is KiCadSchLabel klabel && _labels.Remove(klabel);

    /// <inheritdoc />
    public void AddWire(ISchWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        if (wire is not KiCadSchWire kwire)
            throw new ArgumentException($"Expected {nameof(KiCadSchWire)}", nameof(wire));
        _wires.Add(kwire);
    }

    /// <inheritdoc />
    public bool RemoveWire(ISchWire wire) => wire is KiCadSchWire kwire && _wires.Remove(kwire);

    /// <inheritdoc />
    public void AddPolyline(ISchPolyline polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        if (polyline is not KiCadSchPolyline kpoly)
            throw new ArgumentException($"Expected {nameof(KiCadSchPolyline)}", nameof(polyline));
        _polylines.Add(kpoly);
    }

    /// <inheritdoc />
    public bool RemovePolyline(ISchPolyline polyline) => polyline is KiCadSchPolyline kpoly && _polylines.Remove(kpoly);

    /// <inheritdoc />
    public void AddPolygon(ISchPolygon polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        if (polygon is not KiCadSchPolygon kpoly)
            throw new ArgumentException($"Expected {nameof(KiCadSchPolygon)}", nameof(polygon));
        _polygons.Add(kpoly);
    }

    /// <inheritdoc />
    public bool RemovePolygon(ISchPolygon polygon) => polygon is KiCadSchPolygon kpoly && _polygons.Remove(kpoly);

    /// <inheritdoc />
    public void AddArc(ISchArc arc)
    {
        ArgumentNullException.ThrowIfNull(arc);
        if (arc is not KiCadSchArc karc)
            throw new ArgumentException($"Expected {nameof(KiCadSchArc)}", nameof(arc));
        _arcs.Add(karc);
    }

    /// <inheritdoc />
    public bool RemoveArc(ISchArc arc) => arc is KiCadSchArc karc && _arcs.Remove(karc);

    /// <inheritdoc />
    public void AddCircle(ISchCircle circle)
    {
        ArgumentNullException.ThrowIfNull(circle);
        if (circle is not KiCadSchCircle kcircle)
            throw new ArgumentException($"Expected {nameof(KiCadSchCircle)}", nameof(circle));
        _circles.Add(kcircle);
    }

    /// <inheritdoc />
    public bool RemoveCircle(ISchCircle circle) => circle is KiCadSchCircle kcircle && _circles.Remove(kcircle);

    /// <inheritdoc />
    public void AddBezier(ISchBezier bezier)
    {
        ArgumentNullException.ThrowIfNull(bezier);
        if (bezier is not KiCadSchBezier kbez)
            throw new ArgumentException($"Expected {nameof(KiCadSchBezier)}", nameof(bezier));
        _beziers.Add(kbez);
    }

    /// <inheritdoc />
    public bool RemoveBezier(ISchBezier bezier) => bezier is KiCadSchBezier kbez && _beziers.Remove(kbez);

    /// <inheritdoc />
    public void AddNetLabel(ISchNetLabel netLabel)
    {
        ArgumentNullException.ThrowIfNull(netLabel);
        if (netLabel is not KiCadSchNetLabel knl)
            throw new ArgumentException($"Expected {nameof(KiCadSchNetLabel)}", nameof(netLabel));
        _netLabels.Add(knl);
    }

    /// <inheritdoc />
    public bool RemoveNetLabel(ISchNetLabel netLabel) => netLabel is KiCadSchNetLabel knl && _netLabels.Remove(knl);

    /// <inheritdoc />
    public void AddJunction(ISchJunction junction)
    {
        ArgumentNullException.ThrowIfNull(junction);
        if (junction is not KiCadSchJunction kj)
            throw new ArgumentException($"Expected {nameof(KiCadSchJunction)}", nameof(junction));
        _junctions.Add(kj);
    }

    /// <inheritdoc />
    public bool RemoveJunction(ISchJunction junction) => junction is KiCadSchJunction kj && _junctions.Remove(kj);

    /// <inheritdoc />
    public void AddImage(ISchImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image is not KiCadSchImage kimg)
            throw new ArgumentException($"Expected {nameof(KiCadSchImage)}", nameof(image));
        _images.Add(kimg);
    }

    /// <inheritdoc />
    public bool RemoveImage(ISchImage image) => image is KiCadSchImage kimg && _images.Remove(kimg);

    /// <inheritdoc />
    public void AddParameter(ISchParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (parameter is not KiCadSchParameter kparam)
            throw new ArgumentException($"Expected {nameof(KiCadSchParameter)}", nameof(parameter));
        _parameters.Add(kparam);
    }

    /// <inheritdoc />
    public bool RemoveParameter(ISchParameter parameter) => parameter is KiCadSchParameter kparam && _parameters.Remove(kparam);

    /// <summary>
    /// Adds a sub-symbol to this component.
    /// </summary>
    public void AddSubSymbol(KiCadSchComponent subSymbol)
    {
        ArgumentNullException.ThrowIfNull(subSymbol);
        _subSymbols.Add(subSymbol);
    }

    /// <summary>
    /// Removes a sub-symbol from this component.
    /// </summary>
    public bool RemoveSubSymbol(KiCadSchComponent subSymbol) => _subSymbols.Remove(subSymbol);
}
