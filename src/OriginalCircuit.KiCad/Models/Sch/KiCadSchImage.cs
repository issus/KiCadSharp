using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic image, mapped from <c>(image (at X Y) (scale S) (uuid ...) (data ...))</c>.
/// </summary>
public sealed class KiCadSchImage : ISchImage
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <inheritdoc />
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// Gets the scale factor of the image.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Gets the UUID of the image.
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Corner1, Corner2);
}
