using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic component (symbol definition). Contains sub-symbols (units), pins, and graphical items.
/// </summary>
public sealed class KiCadSchComponent : ISchComponent
{
    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public int PartCount { get; internal set; } = 1;

    /// <inheritdoc />
    public IReadOnlyList<ISchPin> Pins { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchLine> Lines { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchRectangle> Rectangles { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchPolyline> Polylines { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchPolygon> Polygons { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchArc> Arcs { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchCircle> Circles { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchBezier> Beziers { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchImage> Images { get; internal set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ISchParameter> Parameters { get; internal set; } = [];

    /// <summary>
    /// Gets whether the symbol should be included in the BOM.
    /// </summary>
    public bool InBom { get; internal set; } = true;

    /// <summary>
    /// Gets whether the symbol should be included on the board.
    /// </summary>
    public bool OnBoard { get; internal set; } = true;

    /// <summary>
    /// Gets the name of the symbol this one extends (derived/inherited symbols).
    /// </summary>
    public string? Extends { get; internal set; }

    /// <summary>
    /// Gets the pin names offset from the pin end.
    /// </summary>
    public Coord PinNamesOffset { get; internal set; }

    /// <summary>
    /// Gets whether pin numbers are hidden.
    /// </summary>
    public bool HidePinNumbers { get; internal set; }

    /// <summary>
    /// Gets whether pin names are hidden.
    /// </summary>
    public bool HidePinNames { get; internal set; }

    /// <summary>
    /// Gets the sub-symbol children (units).
    /// </summary>
    public IReadOnlyList<KiCadSchComponent> SubSymbols { get; internal set; } = [];

    /// <summary>
    /// Gets the diagnostics collected during parsing.
    /// </summary>
    public IReadOnlyList<KiCadDiagnostic> Diagnostics { get; internal set; } = [];

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
}
