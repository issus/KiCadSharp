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
    public CoordRect Bounds => new(Corner1, Corner2);
}
