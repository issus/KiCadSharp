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

    /// <summary>
    /// Gets the stroke line style.
    /// </summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the stroke color child was present in the source file.
    /// </summary>
    public bool HasStrokeColor { get; set; }

    /// <inheritdoc />
    public bool IsFilled { get; set; }

    /// <summary>
    /// Gets the KiCad fill type.
    /// </summary>
    public SchFillType FillType { get; set; }

    /// <summary>
    /// Gets or sets the UUID of this circle.
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

    /// <summary>
    /// Gets or sets whether this item is marked as private (KiCad 9+).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(
        new CoordPoint(Center.X - Radius, Center.Y - Radius),
        new CoordPoint(Center.X + Radius, Center.Y + Radius));
}
