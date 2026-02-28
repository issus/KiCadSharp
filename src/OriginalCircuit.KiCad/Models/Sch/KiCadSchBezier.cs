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

    /// <summary>
    /// Gets or sets the UUID of this bezier curve.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets whether the UUID was encoded as a symbol (unquoted) rather than a string.
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>
    /// Gets or sets whether the fill node was present in the source file.
    /// </summary>
    public bool HasFill { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(ControlPoints);
}
