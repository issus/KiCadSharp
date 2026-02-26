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

    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public int PartCount { get; set; } = 1;

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
    /// Gets the name of the symbol this one extends (derived/inherited symbols).
    /// </summary>
    public string? Extends { get; set; }

    /// <summary>
    /// Gets the pin names offset from the pin end.
    /// </summary>
    public Coord PinNamesOffset { get; set; }

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

    public void AddPin(ISchPin pin)
    {
        if (pin is not KiCadSchPin kpin)
            throw new ArgumentException($"Expected {nameof(KiCadSchPin)}", nameof(pin));
        _pins.Add(kpin);
    }

    public bool RemovePin(ISchPin pin) => pin is KiCadSchPin kpin && _pins.Remove(kpin);

    public void AddLine(ISchLine line)
    {
        if (line is not KiCadSchLine kline)
            throw new ArgumentException($"Expected {nameof(KiCadSchLine)}", nameof(line));
        _lines.Add(kline);
    }

    public bool RemoveLine(ISchLine line) => line is KiCadSchLine kline && _lines.Remove(kline);

    public void AddRectangle(ISchRectangle rectangle)
    {
        if (rectangle is not KiCadSchRectangle krect)
            throw new ArgumentException($"Expected {nameof(KiCadSchRectangle)}", nameof(rectangle));
        _rectangles.Add(krect);
    }

    public bool RemoveRectangle(ISchRectangle rectangle) => rectangle is KiCadSchRectangle krect && _rectangles.Remove(krect);

    public void AddLabel(ISchLabel label)
    {
        if (label is not KiCadSchLabel klabel)
            throw new ArgumentException($"Expected {nameof(KiCadSchLabel)}", nameof(label));
        _labels.Add(klabel);
    }

    public bool RemoveLabel(ISchLabel label) => label is KiCadSchLabel klabel && _labels.Remove(klabel);

    public void AddWire(ISchWire wire)
    {
        if (wire is not KiCadSchWire kwire)
            throw new ArgumentException($"Expected {nameof(KiCadSchWire)}", nameof(wire));
        _wires.Add(kwire);
    }

    public bool RemoveWire(ISchWire wire) => wire is KiCadSchWire kwire && _wires.Remove(kwire);

    public void AddPolyline(ISchPolyline polyline)
    {
        if (polyline is not KiCadSchPolyline kpoly)
            throw new ArgumentException($"Expected {nameof(KiCadSchPolyline)}", nameof(polyline));
        _polylines.Add(kpoly);
    }

    public bool RemovePolyline(ISchPolyline polyline) => polyline is KiCadSchPolyline kpoly && _polylines.Remove(kpoly);

    public void AddPolygon(ISchPolygon polygon)
    {
        if (polygon is not KiCadSchPolygon kpoly)
            throw new ArgumentException($"Expected {nameof(KiCadSchPolygon)}", nameof(polygon));
        _polygons.Add(kpoly);
    }

    public bool RemovePolygon(ISchPolygon polygon) => polygon is KiCadSchPolygon kpoly && _polygons.Remove(kpoly);

    public void AddArc(ISchArc arc)
    {
        if (arc is not KiCadSchArc karc)
            throw new ArgumentException($"Expected {nameof(KiCadSchArc)}", nameof(arc));
        _arcs.Add(karc);
    }

    public bool RemoveArc(ISchArc arc) => arc is KiCadSchArc karc && _arcs.Remove(karc);

    public void AddCircle(ISchCircle circle)
    {
        if (circle is not KiCadSchCircle kcircle)
            throw new ArgumentException($"Expected {nameof(KiCadSchCircle)}", nameof(circle));
        _circles.Add(kcircle);
    }

    public bool RemoveCircle(ISchCircle circle) => circle is KiCadSchCircle kcircle && _circles.Remove(kcircle);

    public void AddBezier(ISchBezier bezier)
    {
        if (bezier is not KiCadSchBezier kbez)
            throw new ArgumentException($"Expected {nameof(KiCadSchBezier)}", nameof(bezier));
        _beziers.Add(kbez);
    }

    public bool RemoveBezier(ISchBezier bezier) => bezier is KiCadSchBezier kbez && _beziers.Remove(kbez);

    public void AddNetLabel(ISchNetLabel netLabel)
    {
        if (netLabel is not KiCadSchNetLabel knl)
            throw new ArgumentException($"Expected {nameof(KiCadSchNetLabel)}", nameof(netLabel));
        _netLabels.Add(knl);
    }

    public bool RemoveNetLabel(ISchNetLabel netLabel) => netLabel is KiCadSchNetLabel knl && _netLabels.Remove(knl);

    public void AddJunction(ISchJunction junction)
    {
        if (junction is not KiCadSchJunction kj)
            throw new ArgumentException($"Expected {nameof(KiCadSchJunction)}", nameof(junction));
        _junctions.Add(kj);
    }

    public bool RemoveJunction(ISchJunction junction) => junction is KiCadSchJunction kj && _junctions.Remove(kj);

    public void AddImage(ISchImage image)
    {
        if (image is not KiCadSchImage kimg)
            throw new ArgumentException($"Expected {nameof(KiCadSchImage)}", nameof(image));
        _images.Add(kimg);
    }

    public bool RemoveImage(ISchImage image) => image is KiCadSchImage kimg && _images.Remove(kimg);

    public void AddParameter(ISchParameter parameter)
    {
        if (parameter is not KiCadSchParameter kparam)
            throw new ArgumentException($"Expected {nameof(KiCadSchParameter)}", nameof(parameter));
        _parameters.Add(kparam);
    }

    public bool RemoveParameter(ISchParameter parameter) => parameter is KiCadSchParameter kparam && _parameters.Remove(kparam);

    /// <summary>
    /// Adds a sub-symbol to this component.
    /// </summary>
    public void AddSubSymbol(KiCadSchComponent subSymbol) => _subSymbols.Add(subSymbol);

    /// <summary>
    /// Removes a sub-symbol from this component.
    /// </summary>
    public bool RemoveSubSymbol(KiCadSchComponent subSymbol) => _subSymbols.Remove(subSymbol);
}
