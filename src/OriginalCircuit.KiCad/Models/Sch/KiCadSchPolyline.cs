using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic polyline, mapped from <c>(polyline (pts ...) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchPolyline : ISchPolyline
{
    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices { get; set; } = [];

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <inheritdoc />
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
    /// Gets or sets whether the fill node was present in the source file.
    /// </summary>
    public bool HasFill { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the polyline.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets whether the UUID was an unquoted symbol in the source file (KiCad 9+).
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => PointsBounds.Compute(Vertices);
}
