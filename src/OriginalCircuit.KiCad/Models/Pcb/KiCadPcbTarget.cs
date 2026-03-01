using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB target marker, mapped from <c>(target SHAPE (at X Y) (size S) (width W) (layer L) (uuid UUID))</c>.
/// </summary>
public sealed class KiCadPcbTarget
{
    /// <summary>Gets or sets the shape (plus or x).</summary>
    public string Shape { get; set; } = "plus";

    /// <summary>Gets or sets the location.</summary>
    public CoordPoint Location { get; set; }

    /// <summary>Gets or sets the size.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets the line width.</summary>
    public Coord Width { get; set; }

    /// <summary>Gets or sets the layer name.</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets whether the UUID is a bare symbol.</summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>Gets or sets the UUID token name.</summary>
    public string UuidToken { get; set; } = "uuid";
}
