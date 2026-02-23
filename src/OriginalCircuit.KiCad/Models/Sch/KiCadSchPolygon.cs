using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic polygon (a filled polyline), mapped from a polyline with fill.
/// </summary>
public sealed class KiCadSchPolygon : ISchPolygon
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices { get; internal set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public EdaColor FillColor { get; internal set; }

    /// <inheritdoc />
    public Coord LineWidth { get; internal set; }

    /// <inheritdoc />
    public bool IsFilled { get; internal set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Vertices);
}
