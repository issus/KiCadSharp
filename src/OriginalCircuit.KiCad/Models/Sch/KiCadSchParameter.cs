using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic parameter (property), mapped from <c>(property "KEY" "VALUE" (at X Y ANGLE) (effects ...))</c>.
/// </summary>
public sealed class KiCadSchParameter : ISchParameter
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string Value { get; set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; set; }

    /// <inheritdoc />
    public int Orientation { get; set; }

    /// <inheritdoc />
    public TextJustification Justification { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Gets the font size width.
    /// </summary>
    public Coord FontSizeWidth { get; set; }

    /// <summary>
    /// Gets the font size height.
    /// </summary>
    public Coord FontSizeHeight { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var fontH = FontSizeHeight != Coord.Zero ? FontSizeHeight : Coord.FromMm(1.27);
            var fontW = FontSizeWidth != Coord.Zero ? FontSizeWidth : Coord.FromMm(1.27);
            var textWidth = Coord.FromMm(Value.Length * fontW.ToMm() * 0.6);
            return CoordRect.FromCenterAndSize(Location, textWidth, fontH);
        }
    }
}
