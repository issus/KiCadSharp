using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic circle, mapped from <c>(circle (center X Y) (radius R) (stroke ...) (fill ...))</c>.
/// </summary>
public sealed class KiCadSchCircle : ISchCircle
{
    /// <inheritdoc />
    public CoordPoint Center { get; internal set; }

    /// <inheritdoc />
    public Coord Radius { get; internal set; }

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
    public CoordRect Bounds => new(
        new CoordPoint(Center.X - Radius, Center.Y - Radius),
        new CoordPoint(Center.X + Radius, Center.Y + Radius));
}
