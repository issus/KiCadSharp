using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic bezier curve, mapped from <c>(bezier (pts ...) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchBezier : ISchBezier
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> ControlPoints { get; set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the stroke color child was present in the source file.
    /// </summary>
    public bool HasStrokeColor { get; set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets the fill color.
    /// </summary>
    public EdaColor FillColor { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(ControlPoints);
}
