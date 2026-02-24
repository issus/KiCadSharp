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
    /// Gets the KiCad-specific graphic style for the pin.
    /// </summary>
    public PinGraphicStyle GraphicStyle { get; set; }

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
