using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB image, mapped from <c>(image (at X Y [ANGLE]) (layer L) (scale S) (uuid UUID) (data ...))</c>.
/// Used for both board-level and footprint-level images.
/// </summary>
public sealed class KiCadPcbImage
{
    /// <summary>Gets or sets the location.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the rotation angle.</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the scale factor.</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Gets or sets the layer name.</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets whether the UUID is a bare symbol.</summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>Gets or sets the UUID token name.</summary>
    public string UuidToken { get; set; } = "uuid";

    /// <summary>Gets or sets the base64-encoded image data string (for round-trip fidelity).</summary>
    public string? DataString { get; set; }

    /// <summary>Gets or sets the decoded image data bytes.</summary>
    public byte[]? ImageData { get; set; }

    /// <summary>Gets or sets whether the position angle was present in source.</summary>
    public bool PositionIncludesAngle { get; set; }

    /// <summary>Gets or sets whether data values are bare symbols (unquoted).</summary>
    public bool DataAreSymbols { get; set; }
}
