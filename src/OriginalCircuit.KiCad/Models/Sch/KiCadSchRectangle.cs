using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic rectangle, mapped from <c>(rectangle (start X Y) (end X Y) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchRectangle : ISchRectangle
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public EdaColor FillColor { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <inheritdoc />
    public bool IsFilled { get; set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Corner1, Corner2);
}
