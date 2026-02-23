using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB text, mapped from <c>(gr_text "TEXT" (at X Y [ANGLE]) (layer L) (effects ...) (uuid UUID))</c>
/// and footprint-level <c>(fp_text TYPE "TEXT" ...)</c>.
/// </summary>
public sealed class KiCadPcbText : IPcbText
{
    /// <inheritdoc />
    public string Text { get; set; } = "";

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Height { get; internal set; }

    /// <inheritdoc />
    public Coord StrokeWidth { get; internal set; }

    /// <inheritdoc />
    public double Rotation { get; internal set; }

    /// <inheritdoc />
    public int Layer { get; internal set; }

    /// <summary>
    /// Gets the layer name as a string.
    /// </summary>
    public string? LayerName { get; internal set; }

    /// <inheritdoc />
    public bool IsMirrored { get; internal set; }

    /// <inheritdoc />
    public string? FontName { get; internal set; }

    /// <inheritdoc />
    public bool FontBold { get; internal set; }

    /// <inheritdoc />
    public bool FontItalic { get; internal set; }

    /// <summary>
    /// Gets the text type (for fp_text: reference, value, user).
    /// </summary>
    public string? TextType { get; internal set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; internal set; }

    /// <summary>
    /// Gets whether this text is hidden.
    /// </summary>
    public bool IsHidden { get; internal set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, Location);
}
