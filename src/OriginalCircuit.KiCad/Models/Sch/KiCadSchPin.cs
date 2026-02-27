using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad schematic pin, mapped from <c>(pin ELECTRICAL_TYPE GRAPHIC_STYLE (at X Y ANGLE) (length LENGTH) (name "NAME" ...) (number "NUMBER" ...))</c>.
/// </summary>
public sealed class KiCadSchPin : ISchPin
{
    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public string? Designator { get; set; }

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Length { get; set; }

    /// <inheritdoc />
    public PinOrientation Orientation { get; set; }

    /// <inheritdoc />
    public PinElectricalType ElectricalType { get; set; }

    /// <inheritdoc />
    public bool ShowName { get; set; } = true;

    /// <inheritdoc />
    public bool ShowDesignator { get; set; } = true;

    /// <inheritdoc />
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets whether the hide attribute was stored as a bare symbol value (KiCad 6 format: <c>(pin ... hide)</c>)
    /// rather than as a child node (KiCad 8 format: <c>(pin ... (hide yes))</c>).
    /// </summary>
    public bool HideIsSymbolValue { get; set; }

    /// <summary>
    /// Gets the KiCad-specific graphic style for the pin.
    /// </summary>
    public PinGraphicStyle GraphicStyle { get; set; }

    /// <summary>
    /// Gets the pin name font size height.
    /// </summary>
    public Coord NameFontSizeHeight { get; set; }

    /// <summary>
    /// Gets the pin name font size width.
    /// </summary>
    public Coord NameFontSizeWidth { get; set; }

    /// <summary>
    /// Gets the pin number font size height.
    /// </summary>
    public Coord NumberFontSizeHeight { get; set; }

    /// <summary>
    /// Gets the pin number font size width.
    /// </summary>
    public Coord NumberFontSizeWidth { get; set; }

    /// <summary>
    /// Gets the list of pin alternates.
    /// </summary>
    public List<KiCadSchPinAlternate> Alternates { get; set; } = [];

    /// <summary>
    /// Gets or sets the UUID of the pin (used in placed symbols).
    /// </summary>
    public string? Uuid { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var end = Orientation switch
            {
                PinOrientation.Right => new CoordPoint(Location.X + Length, Location.Y),
                PinOrientation.Left => new CoordPoint(Location.X - Length, Location.Y),
                PinOrientation.Up => new CoordPoint(Location.X, Location.Y + Length),
                PinOrientation.Down => new CoordPoint(Location.X, Location.Y - Length),
                _ => Location
            };
            return new CoordRect(Location, end);
        }
    }
}

/// <summary>
/// Represents an alternate definition for a KiCad pin.
/// </summary>
public sealed class KiCadSchPinAlternate
{
    /// <summary>
    /// Gets the alternate name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets the alternate electrical type.
    /// </summary>
    public PinElectricalType ElectricalType { get; set; }

    /// <summary>
    /// Gets the alternate graphic style.
    /// </summary>
    public PinGraphicStyle GraphicStyle { get; set; }
}
