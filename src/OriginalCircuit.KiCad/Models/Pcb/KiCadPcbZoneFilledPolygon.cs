using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// A filled polygon within a zone, mapped from <c>(filled_polygon (layer "name") (pts ...))</c>.
/// </summary>
public sealed class KiCadPcbZoneFilledPolygon
{
    /// <summary>Gets or sets the layer name.</summary>
    public string LayerName { get; set; } = "";

    /// <summary>Gets or sets the polygon points.</summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];

    /// <summary>Gets or sets the island index (if present).</summary>
    public int? IslandIndex { get; set; }
}
