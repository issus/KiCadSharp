using OriginalCircuit.Eda.Rendering;

namespace OriginalCircuit.KiCad.Rendering;

/// <summary>
/// Maps KiCad layer names to ARGB colors and draw priority.
/// Colors are based on KiCad's default color scheme.
/// </summary>
public static class KiCadLayerColors
{
    /// <summary>
    /// Gets the default ARGB color for a KiCad layer name.
    /// </summary>
    /// <param name="layerName">The KiCad layer name (e.g. "F.Cu", "B.SilkS").</param>
    /// <returns>The ARGB color for the layer, or gray if unknown.</returns>
    public static uint GetColor(string? layerName)
    {
        if (layerName == null) return ColorHelper.Gray;

        return layerName switch
        {
            // Copper layers
            "F.Cu" => 0xFFFF0000,         // Red
            "B.Cu" => 0xFF0000FF,         // Blue
            "In1.Cu" => 0xFFC2C200,       // Dark Yellow
            "In2.Cu" => 0xFFC200C2,       // Dark Magenta
            "In3.Cu" => 0xFF00C2C2,       // Dark Cyan
            "In4.Cu" => 0xFF0000C2,       // Dark Blue

            // Silkscreen
            "F.SilkS" => 0xFF00FFFF,     // Cyan
            "B.SilkS" => 0xFFFF00FF,     // Magenta

            // Solder mask
            "F.Mask" => 0x80FF00FF,       // Purple (semi-transparent)
            "B.Mask" => 0x8000FFFF,       // Cyan (semi-transparent)

            // Solder paste
            "F.Paste" => 0xFFFF8080,      // Light red
            "B.Paste" => 0xFF8080FF,      // Light blue

            // Fabrication
            "F.Fab" => 0xFFC8C800,        // Yellow
            "B.Fab" => 0xFF0000C8,        // Blue

            // Courtyard
            "F.CrtYd" => 0xFFC8C8C8,     // Light gray
            "B.CrtYd" => 0xFF808080,      // Gray

            // Adhesive
            "F.Adhes" => 0xFFC800C8,      // Magenta
            "B.Adhes" => 0xFF0000C8,      // Blue

            // Board edges
            "Edge.Cuts" => 0xFFC8C800,    // Yellow

            // Mechanical/User layers
            "Dwgs.User" => 0xFFC8C8C8,   // Light gray
            "Cmts.User" => 0xFF0000FF,    // Blue
            "Eco1.User" => 0xFF008000,    // Green
            "Eco2.User" => 0xFFFFFF00,    // Yellow
            "Margin" => 0xFFFF00FF,       // Magenta

            _ => ColorHelper.Gray
        };
    }

    /// <summary>
    /// Gets the draw priority for a KiCad layer (lower = drawn first / behind).
    /// </summary>
    /// <param name="layerName">The KiCad layer name.</param>
    /// <returns>The draw priority (0-100).</returns>
    public static int GetPriority(string? layerName)
    {
        if (layerName == null) return 50;

        return layerName switch
        {
            "B.Fab" => 10,
            "B.CrtYd" => 11,
            "B.Adhes" => 12,
            "B.Paste" => 15,
            "B.Mask" => 18,
            "B.SilkS" => 20,
            "B.Cu" => 25,
            "In4.Cu" => 30,
            "In3.Cu" => 35,
            "In2.Cu" => 40,
            "In1.Cu" => 45,
            "F.Cu" => 50,
            "F.SilkS" => 55,
            "F.Mask" => 58,
            "F.Paste" => 60,
            "F.Adhes" => 62,
            "F.CrtYd" => 65,
            "F.Fab" => 70,
            "Edge.Cuts" => 80,
            "Dwgs.User" => 85,
            "Cmts.User" => 86,
            "Margin" => 90,
            _ => 50
        };
    }

    /// <summary>
    /// Default schematic color for lines and shapes (dark green, matching KiCad default).
    /// </summary>
    public const uint SchematicDefault = 0xFF008484;

    /// <summary>
    /// Default schematic pin color (red).
    /// </summary>
    public const uint SchematicPin = 0xFFCC0000;

    /// <summary>
    /// Default schematic pin name color (cyan/teal).
    /// </summary>
    public const uint SchematicPinName = 0xFF008484;

    /// <summary>
    /// Default schematic pin number color (red).
    /// </summary>
    public const uint SchematicPinNumber = 0xFFCC0000;

    /// <summary>
    /// Default schematic wire color (green).
    /// </summary>
    public const uint SchematicWire = 0xFF008000;

    /// <summary>
    /// Default schematic bus color (blue).
    /// </summary>
    public const uint SchematicBus = 0xFF0000C8;

    /// <summary>
    /// Default schematic junction color (green).
    /// </summary>
    public const uint SchematicJunction = 0xFF008000;

    /// <summary>
    /// Default schematic no-connect marker color (blue).
    /// </summary>
    public const uint SchematicNoConnect = 0xFF0000FF;

    /// <summary>
    /// Default schematic net label color (cyan/teal).
    /// </summary>
    public const uint SchematicNetLabel = 0xFF008484;

    /// <summary>
    /// Default schematic text/parameter color (cyan/teal).
    /// </summary>
    public const uint SchematicText = 0xFF008484;

    /// <summary>
    /// Default schematic sheet border color (magenta).
    /// </summary>
    public const uint SchematicSheet = 0xFFFF00FF;

    /// <summary>
    /// Default fill color for filled schematic shapes.
    /// </summary>
    public const uint SchematicFill = 0xFFFFFFC8;

    /// <summary>
    /// Default schematic background fill color.
    /// </summary>
    public const uint SchematicBackgroundFill = 0xFFFFFFC8;
}
