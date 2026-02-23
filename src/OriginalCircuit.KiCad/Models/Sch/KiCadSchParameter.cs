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
    public CoordPoint Location { get; internal set; }

    /// <inheritdoc />
    public string Name { get; internal set; } = "";

    /// <inheritdoc />
    public string Value { get; internal set; } = "";

    /// <inheritdoc />
    public EdaColor Color { get; internal set; }

    /// <inheritdoc />
    public int Orientation { get; internal set; }

    /// <inheritdoc />
    public TextJustification Justification { get; internal set; }

    /// <inheritdoc />
    public bool IsVisible { get; internal set; } = true;

    /// <inheritdoc />
    public bool IsMirrored { get; internal set; }

    /// <summary>
    /// Gets the font size width.
    /// </summary>
    public Coord FontSizeWidth { get; internal set; }

    /// <summary>
    /// Gets the font size height.
    /// </summary>
    public Coord FontSizeHeight { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
