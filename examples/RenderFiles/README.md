# RenderFiles Example

Demonstrates rendering KiCad schematic symbols and PCB footprints to PNG and SVG images.

## What It Shows

- **Raster rendering**: produce PNG images of symbols and footprints using SkiaSharp
- **SVG rendering**: produce vector SVG output using .NET XML APIs
- **Auto-zoom**: automatically scale components to fit the output dimensions
- **Hi-res output**: render at custom resolutions (e.g., 2048x2048)
- **CoordTransform**: world-to-screen coordinate mapping with auto-zoom
- **Layer colors**: KiCad's default display colors for PCB layers

## Rendering API

```csharp
var renderer = new KiCadRasterRenderer();

using var fs = File.Create("output.png");
await renderer.RenderAsync(component, fs, new RenderOptions { Width = 512, Height = 512 });
```

Both `KiCadRasterRenderer` (PNG) and `KiCadSvgRenderer` (SVG) accept `KiCadSchComponent` and `KiCadPcbComponent`.

## Running

```
dotnet run --project examples/RenderFiles -- <input-file> [output-dir]
```

For example:
```
dotnet run --project examples/RenderFiles -- resistor.kicad_sym ./output
dotnet run --project examples/RenderFiles -- SOT-23.kicad_mod
```

Supported input formats: `.kicad_sym` (symbol library) and `.kicad_mod` (footprint).
