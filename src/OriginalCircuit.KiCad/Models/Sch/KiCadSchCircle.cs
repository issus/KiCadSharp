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
    public CoordPoint Center { get; set; }

    /// <inheritdoc />
    public Coord Radius { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public EdaColor FillColor { get; set; }

    /// <inheritdoc />
    public Coord LineWidth { get; set; }

    /// <inheritdoc />
    public bool IsFilled { get; set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(
        new CoordPoint(Center.X - Radius, Center.Y - Radius),
        new CoordPoint(Center.X + Radius, Center.Y + Radius));
}
