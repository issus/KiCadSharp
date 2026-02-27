using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic line, mapped from a <c>(polyline (pts (xy X1 Y1) (xy X2 Y2)) (stroke ...))</c> with exactly 2 points.
/// </summary>
public sealed class KiCadSchLine : ISchLine
{
    /// <inheritdoc />
    public CoordPoint Start { get; set; }

    /// <inheritdoc />
    public CoordPoint End { get; set; }

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public Coord Width { get; set; }

    /// <inheritdoc />
    public LineStyle LineStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the stroke color child was present in the source file.
    /// </summary>
    public bool HasStrokeColor { get; set; }

    /// <summary>
    /// Gets or sets whether the fill node was present in the source file.
    /// </summary>
    public bool HasFill { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the line.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets whether the UUID was an unquoted symbol in the source file (KiCad 9+).
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Start, End);
}
