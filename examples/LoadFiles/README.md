# LoadFiles Example

Demonstrates reading and inspecting all four KiCad file types.

## What It Shows

- **Symbol library** (`.kicad_sym`): enumerate symbols, inspect pins, read properties, look up by name
- **Footprint** (`.kicad_mod`): read pad geometry, layer assignments, silkscreen, and bounding box
- **Schematic** (`.kicad_sch`): list wires, net labels, junctions, and no-connect markers
- **PCB layout** (`.kicad_pcb`): inspect net definitions, copper tracks, vias, and board thickness
- **Stream loading**: load from a `MemoryStream` instead of a file path
- **Diagnostics**: check for non-fatal warnings from the parser

## Loading API

```csharp
var lib = await SymLibReader.ReadAsync("symbols.kicad_sym");
var fp  = await FootprintReader.ReadAsync("footprint.kicad_mod");
var sch = await SchReader.ReadAsync("schematic.kicad_sch");
var pcb = await PcbReader.ReadAsync("board.kicad_pcb");
```

All readers also accept a `Stream` and an optional `CancellationToken`.

## Running

```
dotnet run --project examples/LoadFiles
```

The example is self-contained — it creates minimal test files, loads them, and cleans up automatically.
