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

    /// <inheritdoc />
    public CoordRect Bounds => new(Start, End);
}
