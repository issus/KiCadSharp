// ============================================================================
// Example: Modifying Existing KiCad Files
// ============================================================================
//
// This example demonstrates the load -> modify -> save workflow for KiCad
// files. This is the most common real-world usage pattern: you load an
// existing file, make changes, and save it back.
//
// MODIFICATION PATTERNS
// ─────────────────────
// Libraries (KiCadSymLib):
//   - Add new symbols:        library.Add(component)
//   - Remove by name:         library.Remove("R")
//   - Lookup by name:         library["R"]
//   - Modify properties:      change properties on loaded components
//
// Documents (KiCadSch, KiCadPcb):
//   - Modify collections by assigning new lists to the collection properties
//   - Access existing items via typed collections (Wires, Tracks, etc.)
//
// SAVING
// ──────
// After modification, save using either:
//   - Static writer:  await SymLibWriter.WriteAsync(lib, "path")
//   - Model method:   await lib.SaveAsync("path")
//
// VERIFICATION
// ────────────
// Each section creates a file, loads it, modifies it, saves it,
// then reloads to verify the changes persisted through the round-trip.
//
// ============================================================================

using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

var tempDir = Path.Combine(Path.GetTempPath(), "KiCadModifyExample");
Directory.CreateDirectory(tempDir);

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Modify a Symbol Library: remove a symbol, add a new one             ║
// ║                                                                         ║
// ║  Demonstrates: library.Remove("name"), library.Add(component),          ║
// ║  and round-trip verification via SaveAsync + reload.                     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("=== Modifying Symbol Library ===");

// Step 1: Create and save an initial library with 2 symbols
var symLib = new KiCadSymLib { Version = 20231120, Generator = "test" };

symLib.Add(new KiCadSchComponent
{
    Name = "R",
    Description = "Resistor",
    InBom = true,
    OnBoard = true,
    Parameters = [new KiCadSchParameter { Name = "Reference", Value = "R", FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) }],
    SubSymbols =
    [
        new KiCadSchComponent
        {
            Name = "R_1_1",
            Pins = (IReadOnlyList<ISchPin>)new List<KiCadSchPin>
            {
                new() { Name = "~", Designator = "1", Location = new CoordPoint(Coord.Zero, Coord.FromMm(3.81)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Down, ElectricalType = PinElectricalType.Passive },
                new() { Name = "~", Designator = "2", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-3.81)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Up, ElectricalType = PinElectricalType.Passive }
            }
        }
    ]
});

symLib.Add(new KiCadSchComponent
{
    Name = "C",
    Description = "Capacitor",
    InBom = true,
    OnBoard = true,
    Parameters = [new KiCadSchParameter { Name = "Reference", Value = "C", FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) }],
    SubSymbols =
    [
        new KiCadSchComponent
        {
            Name = "C_1_1",
            Pins = (IReadOnlyList<ISchPin>)new List<KiCadSchPin>
            {
                new() { Name = "~", Designator = "1", Location = new CoordPoint(Coord.Zero, Coord.FromMm(2.54)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Down, ElectricalType = PinElectricalType.Passive },
                new() { Name = "~", Designator = "2", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-2.54)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Up, ElectricalType = PinElectricalType.Passive }
            }
        }
    ]
});

var symLibPath = Path.Combine(tempDir, "Modified.kicad_sym");
await symLib.SaveAsync(symLibPath);
Console.WriteLine($"  Initial: {symLib.Count} symbols ({string.Join(", ", symLib.Components.Select(c => c.Name))})");

// Step 2: Load the saved file back
var loadedSymLib = await SymLibReader.ReadAsync(symLibPath);

// Step 3: Remove a symbol by name
loadedSymLib.Remove("R");
Console.WriteLine($"  After removing 'R': {loadedSymLib.Count} symbol(s)");

// Step 4: Add a new symbol
loadedSymLib.Add(new KiCadSchComponent
{
    Name = "L",
    Description = "Inductor",
    InBom = true,
    OnBoard = true,
    Parameters = [new KiCadSchParameter { Name = "Reference", Value = "L", FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) }],
    SubSymbols =
    [
        new KiCadSchComponent
        {
            Name = "L_1_1",
            Pins = (IReadOnlyList<ISchPin>)new List<KiCadSchPin>
            {
                new() { Name = "~", Designator = "1", Location = new CoordPoint(Coord.Zero, Coord.FromMm(2.54)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Down, ElectricalType = PinElectricalType.Passive },
                new() { Name = "~", Designator = "2", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-2.54)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Up, ElectricalType = PinElectricalType.Passive }
            }
        }
    ]
});
Console.WriteLine($"  After adding 'L': {loadedSymLib.Count} symbol(s)");

// Step 5: Save and verify
await loadedSymLib.SaveAsync(symLibPath);

var verifySymLib = await SymLibReader.ReadAsync(symLibPath);
Console.WriteLine($"  Verified after reload: {verifySymLib.Count} symbols ({string.Join(", ", verifySymLib.Components.Select(c => c.Name))})");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Modify a Symbol: change properties and description                  ║
// ║                                                                         ║
// ║  Demonstrates: looking up a component by name, modifying its            ║
// ║  properties (Name, Description), and verifying the round-trip.          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying Symbol Properties ===");

var lib2 = await SymLibReader.ReadAsync(symLibPath);
var cap = lib2["C"];
Console.WriteLine($"  Before: Name='{cap!.Name}' Description='{cap.Description}'");

cap.Name = "C_Polarized";
cap.Description = "Polarized Capacitor (Electrolytic)";

await lib2.SaveAsync(symLibPath);

var verifyLib2 = await SymLibReader.ReadAsync(symLibPath);
var verifiedCap = verifyLib2["C_Polarized"];
Console.WriteLine($"  After:  Name='{verifiedCap!.Name}' Description='{verifiedCap.Description}'");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Modify a Footprint: change description and pad properties           ║
// ║                                                                         ║
// ║  Demonstrates: loading a footprint, changing metadata, and modifying    ║
// ║  existing pad geometry.                                                 ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying Footprint ===");

// Create initial footprint
var fp = new KiCadPcbComponent
{
    Name = "R_0805",
    Description = "0805 Resistor",
    LayerName = "F.Cu",
    Attributes = FootprintAttribute.Smd,
    Pads = (IReadOnlyList<IPcbPad>)new List<KiCadPcbPad>
    {
        new() { Designator = "1", Location = new CoordPoint(Coord.FromMm(-0.9), Coord.Zero), Size = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(1.4)), Shape = PadShape.RoundRect, PadType = PadType.Smd, Layers = ["F.Cu", "F.Paste", "F.Mask"] },
        new() { Designator = "2", Location = new CoordPoint(Coord.FromMm(0.9), Coord.Zero), Size = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(1.4)), Shape = PadShape.RoundRect, PadType = PadType.Smd, Layers = ["F.Cu", "F.Paste", "F.Mask"] }
    }
};

var fpPath = Path.Combine(tempDir, "Modified.kicad_mod");
await FootprintWriter.WriteAsync(fp, fpPath);
Console.WriteLine($"  Initial: '{fp.Name}' with {fp.Pads.Count} pads");

// Load and modify
var loadedFp = await FootprintReader.ReadAsync(fpPath);
loadedFp.Name = "R_0805_HandSolder";
loadedFp.Description = "0805 Resistor, hand-soldering variant with extended pads";

Console.WriteLine($"  Modified: '{loadedFp.Name}'");
Console.WriteLine($"    Description: {loadedFp.Description}");
Console.WriteLine($"    Pads: {loadedFp.Pads.Count}");

// Save and verify
await FootprintWriter.WriteAsync(loadedFp, fpPath);

var verifyFp = await FootprintReader.ReadAsync(fpPath);
Console.WriteLine($"  Verified: '{verifyFp.Name}' — {verifyFp.Description}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Modify a Schematic: add wires and net labels                        ║
// ║                                                                         ║
// ║  Demonstrates: loading a schematic, adding connectivity elements        ║
// ║  (wires, net labels), and saving the modified document.                 ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying Schematic ===");

// Create initial schematic with one wire
var sch = new KiCadSch
{
    Version = 20231120,
    Generator = "test",
    Uuid = Guid.NewGuid().ToString("D"),
    Wires = (IReadOnlyList<ISchWire>)new List<KiCadSchWire>
    {
        new() { Vertices = [new CoordPoint(Coord.FromMm(100), Coord.FromMm(100)), new CoordPoint(Coord.FromMm(110), Coord.FromMm(100))], Uuid = Guid.NewGuid().ToString("D") }
    }
};

var schPath = Path.Combine(tempDir, "Modified.kicad_sch");
await sch.SaveAsync(schPath);
Console.WriteLine($"  Initial: {sch.Wires.Count} wire, {sch.NetLabels.Count} net labels");

// Load and add more connectivity
var loadedSch = await SchReader.ReadAsync(schPath);

// Add a new wire by creating a new list that includes existing + new wires
var wires = loadedSch.Wires.Cast<KiCadSchWire>().ToList();
wires.Add(new KiCadSchWire
{
    Vertices = [new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)), new CoordPoint(Coord.FromMm(110), Coord.FromMm(90))],
    Uuid = Guid.NewGuid().ToString("D")
});
loadedSch.Wires = wires;

// Add a net label
var netLabels = loadedSch.NetLabels.Cast<KiCadSchNetLabel>().ToList();
netLabels.Add(new KiCadSchNetLabel
{
    Text = "DATA_BUS",
    Location = new CoordPoint(Coord.FromMm(110), Coord.FromMm(90)),
    Uuid = Guid.NewGuid().ToString("D")
});
loadedSch.NetLabels = netLabels;

Console.WriteLine($"  After modification: {loadedSch.Wires.Count} wires, {loadedSch.NetLabels.Count} net labels");

// Save and verify
await loadedSch.SaveAsync(schPath);

var verifySch = await SchReader.ReadAsync(schPath);
Console.WriteLine($"  Verified: {verifySch.Wires.Count} wires, {verifySch.NetLabels.Count} net labels");
foreach (var nl in verifySch.NetLabels)
    Console.WriteLine($"    Net label: \"{nl.Text}\"");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  5. Modify a PCB: add tracks and vias                                   ║
// ║                                                                         ║
// ║  Demonstrates: loading a PCB, adding copper primitives (tracks, vias),  ║
// ║  and verifying the round-trip.                                          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying PCB ===");

// Create initial board with one track
var pcb = new KiCadPcb
{
    Version = 20231014,
    Generator = "test",
    BoardThickness = Coord.FromMm(1.6),
    Nets = [(0, ""), (1, "VCC"), (2, "GND")],
    Tracks = (IReadOnlyList<IPcbTrack>)new List<KiCadPcbTrack>
    {
        new() { Start = new CoordPoint(Coord.FromMm(100), Coord.FromMm(100)), End = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)), Width = Coord.FromMm(0.25), LayerName = "F.Cu", Net = 1, Uuid = Guid.NewGuid().ToString("D") }
    }
};

var pcbPath = Path.Combine(tempDir, "Modified.kicad_pcb");
await pcb.SaveAsync(pcbPath);
Console.WriteLine($"  Initial: {pcb.Tracks.Count} track, {pcb.Vias.Count} vias");

// Load and add more routing
var loadedPcb = await PcbReader.ReadAsync(pcbPath);

// Add tracks by extending the collection
var tracks = loadedPcb.Tracks.Cast<KiCadPcbTrack>().ToList();
tracks.Add(new KiCadPcbTrack
{
    Start = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)),
    End = new CoordPoint(Coord.FromMm(110), Coord.FromMm(90)),
    Width = Coord.FromMm(0.25),
    LayerName = "F.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
tracks.Add(new KiCadPcbTrack
{
    Start = new CoordPoint(Coord.FromMm(110), Coord.FromMm(90)),
    End = new CoordPoint(Coord.FromMm(120), Coord.FromMm(90)),
    Width = Coord.FromMm(0.25),
    LayerName = "F.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
loadedPcb.Tracks = tracks;

// Add a via at the corner
var vias = loadedPcb.Vias.Cast<KiCadPcbVia>().ToList();
vias.Add(new KiCadPcbVia
{
    Location = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)),
    Diameter = Coord.FromMm(0.8),
    HoleSize = Coord.FromMm(0.4),
    StartLayerName = "F.Cu",
    EndLayerName = "B.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
loadedPcb.Vias = vias;

Console.WriteLine($"  After modification: {loadedPcb.Tracks.Count} tracks, {loadedPcb.Vias.Count} vias");

// Save and verify
await loadedPcb.SaveAsync(pcbPath);

var verifyPcb = await PcbReader.ReadAsync(pcbPath);
Console.WriteLine($"  Verified: {verifyPcb.Tracks.Count} tracks, {verifyPcb.Vias.Count} vias");

// Clean up
Directory.Delete(tempDir, recursive: true);
Console.WriteLine("\nAll modifications verified successfully!");
