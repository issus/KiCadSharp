# ModifyFiles Example

Demonstrates the load, modify, save, and verify workflow for all four KiCad file types.

## What It Shows

- **Symbol library**: add and remove symbols by name, rename components, change descriptions
- **Footprint**: modify name and description, inspect pad geometry after round-trip
- **Schematic**: append wires and net labels to a loaded document
- **PCB**: add copper tracks and vias to a loaded board layout
- **Round-trip verification**: every modification is saved, reloaded, and verified

## Modification Patterns

**Libraries** use `Add`/`Remove`/indexer:
```csharp
var lib = await SymLibReader.ReadAsync("symbols.kicad_sym");
lib.Remove("OLD_SYMBOL");
lib["R"]!.Description = "Updated";
lib.Add(newComponent);
await lib.SaveAsync("symbols.kicad_sym");
```

**Documents** use list replacement:
```csharp
var sch = await SchReader.ReadAsync("schematic.kicad_sch");
var wires = sch.Wires.Cast<KiCadSchWire>().ToList();
wires.Add(new KiCadSchWire { ... });
sch.Wires = wires;
await sch.SaveAsync("schematic.kicad_sch");
```

## Running

```
dotnet run --project examples/ModifyFiles
```

The example is self-contained — it creates test files, modifies them, verifies the round-trip, and cleans up automatically.
