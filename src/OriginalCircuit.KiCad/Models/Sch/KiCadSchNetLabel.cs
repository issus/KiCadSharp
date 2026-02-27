using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic net label, mapped from <c>(label "TEXT" (at X Y ANGLE) (effects ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchNetLabel : ISchNetLabel
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public int Orientation { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public NetLabelType LabelType { get; set; }

    /// <summary>
    /// Gets the UUID of the net label.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontSize = Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Text.Length * fontSize.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontSize);
        }
    }
}
