# CreateFiles Example

Demonstrates creating all four KiCad file types from scratch using the KiCadSharp API.

## What It Creates

| File | Description |
|------|-------------|
| `MySymbols.kicad_sym` | Symbol library with a resistor and op-amp |
| `R_0805_2012Metric.kicad_mod` | SMD 0805 resistor footprint with rounded-rectangle pads |
| `MySchematic.kicad_sch` | Schematic sheet with wires, net labels, and junctions |
| `MyBoard.kicad_pcb` | PCB layout with tracks, a via, and net definitions |

## Key Concepts

- **Coordinates**: All values use `Coord.FromMm()` since KiCad uses millimeters natively
- **Symbol structure**: KiCad symbols use a two-level structure — the root component holds properties, while `SubSymbols` hold graphical bodies and pins
- **PCB layers**: KiCad uses string layer names (`"F.Cu"`, `"F.SilkS"`, `"F.Mask"`, etc.) instead of numeric IDs
- **Properties**: Symbol parameters (`Reference`, `Value`) and footprint properties are `KiCadSchParameter` objects

## Running

```
dotnet run --project examples/CreateFiles
```

Output files are written to a temporary directory printed at startup. You can open the generated files directly in KiCad.
