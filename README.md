# OriginalCircuit.KiCad

[![License](https://img.shields.io/github/license/issus/KiCadSharp)](LICENSE)

A high-performance .NET library for reading and writing KiCad EDA files without requiring KiCad to be installed. It supports symbol libraries, footprints, schematic documents, and PCB layouts, and provides cross-platform rendering to raster images and SVG.

## Supported File Types

| File Type | Extension | Read | Write | Render |
|-----------|-----------|------|-------|--------|
| Symbol Library | `.kicad_sym` | Yes | Yes | Yes |
| Schematic Document | `.kicad_sch` | Yes | Yes | — |
| Footprint | `.kicad_mod` | Yes | Yes | Yes |
| PCB Layout | `.kicad_pcb` | Yes | Yes | — |

## Installation

Install the core library:

```
dotnet add package OriginalCircuit.KiCad
```

Optional rendering packages:

```
dotnet add package OriginalCircuit.KiCad.Rendering.Raster   # PNG/JPG via SkiaSharp
dotnet add package OriginalCircuit.KiCad.Rendering.Svg      # Vector SVG output
```

## Quick Start

**Reading a symbol library:**

```csharp
using OriginalCircuit.KiCad.Serialization;

var lib = await SymLibReader.ReadAsync("MySymbols.kicad_sym");

foreach (var symbol in lib.Components)
{
    Console.WriteLine($"{symbol.Name}: {symbol.Description}");
    Console.WriteLine($"  Pins: {symbol.SubSymbols.SelectMany(s => s.Pins).Count()}");
}
```

**Reading a footprint:**

```csharp
var footprint = await FootprintReader.ReadAsync("R_0805.kicad_mod");

Console.WriteLine($"{footprint.Name} — {footprint.Pads.Count} pads");
foreach (var pad in footprint.Pads)
    Console.WriteLine($"  Pad {pad.Designator}: {pad.Location.X.ToMm():F2} x {pad.Location.Y.ToMm():F2} mm");
```

**Creating a symbol library from scratch:**

```csharp
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Enums;

var lib = new KiCadSymLib { Version = 20231120, Generator = "MyApp" };

var resistor = new KiCadSchComponent
{
    Name = "R",
    Description = "Resistor",
    InBom = true,
    OnBoard = true,
    SubSymbols =
    [
        new KiCadSchComponent
        {
            Name = "R_1_1",
            Pins = (IReadOnlyList<ISchPin>)new List<KiCadSchPin>
            {
                new() { Designator = "1", Location = new CoordPoint(Coord.Zero, Coord.FromMm(3.81)),
                         Length = Coord.FromMm(1.27), Orientation = PinOrientation.Down,
                         ElectricalType = PinElectricalType.Passive },
                new() { Designator = "2", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-3.81)),
                         Length = Coord.FromMm(1.27), Orientation = PinOrientation.Up,
                         ElectricalType = PinElectricalType.Passive }
            }
        }
    ]
};

lib.Add(resistor);
await lib.SaveAsync("MySymbols.kicad_sym");
```

**Modifying an existing file:**

```csharp
var lib = await SymLibReader.ReadAsync("existing.kicad_sym");
lib.Remove("OLD_SYMBOL");
lib["R"]!.Description = "Updated resistor";
await lib.SaveAsync("modified.kicad_sym");
```

## Rendering

Optional rendering packages produce visual previews of schematic symbols and PCB footprints:

- **OriginalCircuit.KiCad.Rendering.Raster** — renders to PNG using SkiaSharp (cross-platform)
- **OriginalCircuit.KiCad.Rendering.Svg** — renders to SVG using .NET XML APIs (no native dependencies)

```csharp
using OriginalCircuit.KiCad.Rendering;
using OriginalCircuit.Eda.Rendering;

var lib = await SymLibReader.ReadAsync("symbols.kicad_sym");
var renderer = new KiCadRasterRenderer();

foreach (var symbol in lib.Components)
{
    using var fs = File.Create($"{symbol.Name}.png");
    await renderer.RenderAsync(symbol, fs, new RenderOptions { Width = 512, Height = 512 });
}
```

See the [examples/RenderFiles](examples/RenderFiles) project for complete rendering examples.

## Examples

The [examples/](examples/) directory contains runnable examples:

- `CreateFiles` — create all four KiCad file types from scratch
- `LoadFiles` — read files and inspect their contents
- `ModifyFiles` — read a file, modify components, and write it back
- `RenderFiles` — render components to PNG and SVG

Run any example with:

```
dotnet run --project examples/CreateFiles
```

## License

MIT — see [LICENSE](LICENSE) for details.
