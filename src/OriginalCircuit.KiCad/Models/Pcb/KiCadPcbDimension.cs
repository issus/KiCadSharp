using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB dimension, mapped from <c>(dimension (type ...) (layer ...) ...)</c>.
/// Supports aligned, orthogonal, radial, leader, and center dimension types.
/// </summary>
public sealed class KiCadPcbDimension
{
    /// <summary>Gets or sets the dimension type (aligned, orthogonal, radial, leader, center).</summary>
    public string? DimensionType { get; set; }

    /// <summary>Gets or sets whether this dimension is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Gets or sets whether the locked flag uses child node format.</summary>
    public bool LockedIsChildNode { get; set; }

    /// <summary>Gets or sets the layer name.</summary>
    public string? LayerName { get; set; }

    /// <summary>Gets or sets the UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Gets or sets whether the UUID value is a bare symbol.</summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>Gets or sets the dimension points.</summary>
    public IReadOnlyList<CoordPoint> Points { get; set; } = [];

    /// <summary>Gets or sets the height (for aligned/orthogonal).</summary>
    public double? Height { get; set; }

    /// <summary>Gets or sets the orientation (for orthogonal: 0=horizontal, 1=vertical).</summary>
    public double? Orientation { get; set; }

    /// <summary>Gets or sets the leader length (for radial/leader).</summary>
    public double? LeaderLength { get; set; }

    // -- Format --

    /// <summary>Gets or sets whether format section was present.</summary>
    public bool HasFormat { get; set; }
    /// <summary>Gets or sets the format prefix.</summary>
    public string? FormatPrefix { get; set; }
    /// <summary>Gets or sets the format suffix.</summary>
    public string? FormatSuffix { get; set; }
    /// <summary>Gets or sets the format units.</summary>
    public int? FormatUnits { get; set; }
    /// <summary>Gets or sets the format units_format.</summary>
    public int? FormatUnitsFormat { get; set; }
    /// <summary>Gets or sets the format precision.</summary>
    public int? FormatPrecision { get; set; }
    /// <summary>Gets or sets the format override_value.</summary>
    public string? FormatOverrideValue { get; set; }
    /// <summary>Gets or sets whether suppress_zeroes is enabled. Note: KiCad uses "suppress_zeroes" (with 'e').</summary>
    public bool? FormatSuppressZeroes { get; set; }

    // -- Style --

    /// <summary>Gets or sets whether style section was present.</summary>
    public bool HasStyle { get; set; }
    /// <summary>Gets or sets the style line thickness.</summary>
    public Coord StyleThickness { get; set; }
    /// <summary>Gets or sets the style arrow length.</summary>
    public double? StyleArrowLength { get; set; }
    /// <summary>Gets or sets the style text position mode.</summary>
    public int? StyleTextPositionMode { get; set; }
    /// <summary>Gets or sets the style arrow direction.</summary>
    public string? StyleArrowDirection { get; set; }
    /// <summary>Gets or sets the style extension height.</summary>
    public double? StyleExtensionHeight { get; set; }
    /// <summary>Gets or sets the style text frame.</summary>
    public int? StyleTextFrame { get; set; }
    /// <summary>Gets or sets the style extension offset.</summary>
    public double? StyleExtensionOffset { get; set; }
    /// <summary>Gets or sets whether keep_text_aligned is set.</summary>
    public bool? StyleKeepTextAligned { get; set; }

    // -- Embedded gr_text --

    /// <summary>Gets or sets the embedded text element for this dimension.</summary>
    public KiCadPcbText? Text { get; set; }
}
