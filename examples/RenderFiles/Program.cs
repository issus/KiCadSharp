// ============================================================================
// Example: Rendering KiCad Components to PNG and SVG
// ============================================================================
//
// This example demonstrates the rendering system, which can produce visual
// previews of KiCad schematic symbols and PCB footprints.
//
// RENDERING ARCHITECTURE
// ──────────────────────
// The rendering system uses shared base classes from Eda.Rendering:
//
//   Eda.Rendering.Core    - CoordTransform, ColorHelper, RenderOptions
//   Eda.Rendering.Raster  - SkiaSharp raster backend (PNG)
//   Eda.Rendering.Svg     - SVG vector backend (System.Xml.Linq)
//
// KiCad-specific rendering is in OriginalCircuit.KiCad.Rendering:
//
//   KiCadRasterRenderer   - Renders KiCad components to PNG
//   KiCadSvgRenderer      - Renders KiCad components to SVG
//   KiCadLayerColors      - Maps KiCad layer names to display colors
//
// WHAT CAN BE RENDERED
// ────────────────────
// Both renderers accept KiCad schematic components (.kicad_sym) and
// PCB footprints (.kicad_mod). They draw all contained primitives:
// pins, lines, rectangles, arcs, circles, polylines, pads, tracks, etc.
//
// With AutoZoom=true (default), the component is automatically scaled
// to fit the output dimensions while maintaining aspect ratio.
//
// ============================================================================

using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.KiCad.Rendering;
using OriginalCircuit.KiCad.Serialization;

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  Parse command-line arguments                                           ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

if (args.Length < 1)
{
    Console.WriteLine("Usage: RenderFiles <input-file> [output-dir]");
    Console.WriteLine();
    Console.WriteLine("  input-file   Path to a .kicad_sym or .kicad_mod file");
    Console.WriteLine("  output-dir   Directory for output files (default: temp directory)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  RenderFiles resistor.kicad_sym");
    Console.WriteLine("  RenderFiles SOT-23.kicad_mod ./output");
    return;
}

var inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"File not found: {inputPath}");
    return;
}

var outputDir = args.Length > 1
    ? args[1]
    : Path.Combine(Path.GetTempPath(), "KiCadRenderExample");
Directory.CreateDirectory(outputDir);
Console.WriteLine($"Output directory: {outputDir}");

var baseName = Path.GetFileNameWithoutExtension(inputPath);
var ext = Path.GetExtension(inputPath).ToLowerInvariant();

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Render a Symbol Library (.kicad_sym)                                ║
// ║                                                                         ║
// ║  Symbol libraries contain one or more schematic symbols. Each symbol    ║
// ║  is rendered individually to both PNG and SVG.                          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

if (ext == ".kicad_sym")
{
    Console.WriteLine($"\nReading symbol library: {inputPath}");
    var lib = await SymLibReader.ReadAsync(inputPath);
    Console.WriteLine($"  Found {lib.Components.Count} component(s)");

    var rasterRenderer = new KiCadRasterRenderer();
    var svgRenderer = new KiCadSvgRenderer();

    foreach (var component in lib.Components)
    {
        var safeName = string.Join("_", component.Name.Split(Path.GetInvalidFileNameChars()));

        // Render to PNG
        var pngPath = Path.Combine(outputDir, $"{safeName}.png");
        using (var fs = File.Create(pngPath))
            await rasterRenderer.RenderAsync(component, fs, new RenderOptions { Width = 512, Height = 512 });
        Console.WriteLine($"  PNG: {pngPath} ({new FileInfo(pngPath).Length} bytes)");

        // Render to SVG
        var svgPath = Path.Combine(outputDir, $"{safeName}.svg");
        using (var fs = File.Create(svgPath))
            await svgRenderer.RenderAsync(component, fs, new RenderOptions { Width = 512, Height = 512 });
        Console.WriteLine($"  SVG: {svgPath} ({new FileInfo(svgPath).Length} bytes)");
    }
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Render a Footprint (.kicad_mod)                                     ║
// ║                                                                         ║
// ║  Footprint files contain a single PCB footprint with pads, tracks,      ║
// ║  silkscreen, courtyard, and fabrication layer graphics.                  ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

else if (ext == ".kicad_mod")
{
    Console.WriteLine($"\nReading footprint: {inputPath}");
    var component = await FootprintReader.ReadAsync(inputPath);

    var rasterRenderer = new KiCadRasterRenderer();
    var svgRenderer = new KiCadSvgRenderer();

    // Render to PNG
    var pngPath = Path.Combine(outputDir, $"{baseName}.png");
    using (var fs = File.Create(pngPath))
        await rasterRenderer.RenderAsync(component, fs, new RenderOptions { Width = 512, Height = 512 });
    Console.WriteLine($"  PNG: {pngPath} ({new FileInfo(pngPath).Length} bytes)");

    // Render to SVG
    var svgPath = Path.Combine(outputDir, $"{baseName}.svg");
    using (var fs = File.Create(svgPath))
        await svgRenderer.RenderAsync(component, fs, new RenderOptions { Width = 512, Height = 512 });
    Console.WriteLine($"  SVG: {svgPath} ({new FileInfo(svgPath).Length} bytes)");

    // Render hi-res version with custom options
    Console.WriteLine("\n=== Hi-Res Render (2048x2048) ===");
    var hiResPath = Path.Combine(outputDir, $"{baseName}_hires.png");
    using (var fs = File.Create(hiResPath))
        await rasterRenderer.RenderAsync(component, fs, new RenderOptions { Width = 2048, Height = 2048 });
    Console.WriteLine($"  PNG: {hiResPath} ({new FileInfo(hiResPath).Length} bytes)");
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. CoordTransform: world-to-screen coordinate mapping                  ║
// ║                                                                         ║
// ║  CoordTransform handles mapping between KiCad's internal coordinate     ║
// ║  space (Coord values) and screen pixel positions. AutoZoom() calculates ║
// ║  Scale and Center to fit a bounding box into the output dimensions.     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

else
{
    Console.Error.WriteLine($"Unsupported file extension: {ext}");
    Console.Error.WriteLine("Supported: .kicad_sym, .kicad_mod");
    return;
}

Console.WriteLine("\n=== CoordTransform Example ===");

var transform = new CoordTransform
{
    ScreenWidth = 1024,
    ScreenHeight = 768
};

var exampleBounds = new CoordRect(
    Coord.FromMm(0), Coord.FromMm(0),
    Coord.FromMm(10), Coord.FromMm(8));
transform.AutoZoom(exampleBounds);

Console.WriteLine($"  Bounds: (0, 0) to (10, 8) mm");
Console.WriteLine($"  Scale: {transform.Scale:F6}");
Console.WriteLine($"  Center: ({transform.CenterX:F0}, {transform.CenterY:F0})");

var (sx, sy) = transform.WorldToScreen(Coord.FromMm(5), Coord.FromMm(4));
Console.WriteLine($"  World (5mm, 4mm) -> Screen ({sx:F1}, {sy:F1})");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Layer Colors                                                        ║
// ║                                                                         ║
// ║  KiCadLayerColors provides KiCad's default display colors for PCB       ║
// ║  layers. GetColor() returns ARGB uint, GetPriority() returns draw       ║
// ║  order (higher = drawn later / on top).                                 ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== KiCad Layer Colors ===");

var layers = new[]
{
    "F.Cu", "B.Cu", "F.SilkS", "B.SilkS",
    "F.Mask", "B.Mask", "Edge.Cuts", "F.Fab"
};

foreach (var layer in layers)
{
    var color = KiCadLayerColors.GetColor(layer);
    var priority = KiCadLayerColors.GetPriority(layer);
    Console.WriteLine($"  {layer,-12} color=0x{color:X8}  priority={priority}");
}

Console.WriteLine($"\nAll rendered files are in: {outputDir}");
