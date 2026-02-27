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
    /// Gets or sets the UUID of the rectangle.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets whether the UUID was an unquoted symbol in the source file (KiCad 9+).
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Corner1, Corner2);
}
