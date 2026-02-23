using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic sheet pin, mapped from <c>(pin "NAME" input|output|... (at X Y ANGLE) (effects ...) (uuid ...))</c>.
/// </summary>
public sealed class KiCadSchSheetPin : ISchSheetPin
{
    /// <inheritdoc />
    public string Name { get; internal set; } = "";

    /// <inheritdoc />
    public int IoType { get; internal set; }

    /// <inheritdoc />
    public int Side { get; internal set; }

    /// <inheritdoc />
    public CoordPoint Location { get; internal set; }

    /// <summary>
    /// Gets the UUID of the sheet pin.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
