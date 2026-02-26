// ============================================================================
// Example: Loading and Inspecting KiCad Files
// ============================================================================
//
// This example demonstrates reading all four KiCad file types and inspecting
// their contents programmatically.
//
// LOADING API
// ───────────
// KiCad files are loaded with static async reader methods:
//
//   var lib = await SymLibReader.ReadAsync("symbols.kicad_sym");
//   var sch = await SchReader.ReadAsync("schematic.kicad_sch");
//   var fp  = await FootprintReader.ReadAsync("footprint.kicad_mod");
//   var pcb = await PcbReader.ReadAsync("board.kicad_pcb");
//
// All readers return ValueTask and accept either a file path or a Stream,
// plus an optional CancellationToken.
//
// TYPE HIERARCHY
// ──────────────
// The returned objects implement shared interfaces from Eda.Abstractions:
//   ISchLibrary  — symbol library (Components, Count, Contains, Add, Remove)
//   ISchDocument — schematic (Components, Wires, NetLabels, Junctions, etc.)
//   IPcbComponent — footprint (Pads, Tracks, Arcs, Texts, etc.)
//   IPcbDocument  — PCB layout (Components, Tracks, Vias, Pads, etc.)
//
// The concrete KiCad types expose additional properties not in the
// interfaces (InBom, OnBoard, LayerName, Uuid, etc.).
//
// This example is self-contained: it creates minimal test files first,
// then loads and inspects them.
//
// ============================================================================

using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

// Create test files so this example is self-contained
var tempDir = Path.Combine(Path.GetTempPath(), "KiCadLoadExample");
Directory.CreateDirectory(tempDir);
await CreateTestFilesAsync(tempDir);

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Load a Symbol Library (.kicad_sym)                                  ║
// ║                                                                         ║
// ║  SymLibReader.ReadAsync() parses the S-expression file and returns a    ║
// ║  KiCadSymLib containing all symbol definitions.                         ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("=== Loading Symbol Library (.kicad_sym) ===");

var symLib = await SymLibReader.ReadAsync(Path.Combine(tempDir, "Test.kicad_sym"));
Console.WriteLine($"  Version: {symLib.Version}");
Console.WriteLine($"  Generator: {symLib.Generator}");
Console.WriteLine($"  Components: {symLib.Count}");

foreach (var component in symLib.Components)
{
    Console.WriteLine($"\n  Symbol: {component.Name}");
    Console.WriteLine($"    Description: {component.Description}");
    Console.WriteLine($"    InBom: {component.InBom}, OnBoard: {component.OnBoard}");
    Console.WriteLine($"    Bounds: {component.Bounds.Width.ToMm():F2} x {component.Bounds.Height.ToMm():F2} mm");

    // Pins may be in the root component or in SubSymbols
    var allPins = component.Pins.Concat(component.SubSymbols.SelectMany(s => s.Pins));
    foreach (var pin in allPins)
    {
        var p = (KiCadSchPin)pin;
        Console.WriteLine($"    Pin {p.Designator} ({p.Name}): " +
                          $"at ({p.Location.X.ToMm():F2}, {p.Location.Y.ToMm():F2}) mm " +
                          $"type={p.ElectricalType} orient={p.Orientation}");
    }

    // Parameters hold Reference, Value, and other properties
    foreach (var param in component.Parameters)
        Console.WriteLine($"    Property: {param.Name} = \"{param.Value}\"");
}

// Lookup by name
Console.WriteLine($"\n  Contains 'R': {symLib.Contains("R")}");
Console.WriteLine($"  Contains 'UNKNOWN': {symLib.Contains("UNKNOWN")}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Load a Footprint (.kicad_mod)                                       ║
// ║                                                                         ║
// ║  FootprintReader.ReadAsync() returns a single KiCadPcbComponent.        ║
// ║  Unlike symbol libraries which hold multiple symbols, a .kicad_mod      ║
// ║  file contains exactly one footprint.                                   ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading Footprint (.kicad_mod) ===");

var footprint = await FootprintReader.ReadAsync(Path.Combine(tempDir, "Test.kicad_mod"));

Console.WriteLine($"  Name: {footprint.Name}");
Console.WriteLine($"  Description: {footprint.Description}");
Console.WriteLine($"  Layer: {footprint.LayerName}");
Console.WriteLine($"  Attributes: {footprint.Attributes}");
Console.WriteLine($"  Pads: {footprint.Pads.Count}");
Console.WriteLine($"  Tracks: {footprint.Tracks.Count}");
Console.WriteLine($"  Texts: {footprint.Texts.Count}");
Console.WriteLine($"  Bounds: {footprint.Bounds.Width.ToMm():F2} x {footprint.Bounds.Height.ToMm():F2} mm");

foreach (var pad in footprint.Pads)
{
    var p = (KiCadPcbPad)pad;
    Console.WriteLine($"  Pad {p.Designator}: shape={p.Shape} size={p.Size.X.ToMm():F2}x{p.Size.Y.ToMm():F2} mm " +
                      $"type={p.PadType} layers=[{string.Join(", ", p.Layers)}]");
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Load a Schematic Document (.kicad_sch)                              ║
// ║                                                                         ║
// ║  SchReader.ReadAsync() returns a KiCadSch containing all placed         ║
// ║  components, wires, net labels, junctions, and other connectivity       ║
// ║  elements on the schematic sheet.                                       ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading Schematic (.kicad_sch) ===");

var sch = await SchReader.ReadAsync(Path.Combine(tempDir, "Test.kicad_sch"));

Console.WriteLine($"  Version: {sch.Version}");
Console.WriteLine($"  Wires: {sch.Wires.Count}");
Console.WriteLine($"  Net labels: {sch.NetLabels.Count}");
Console.WriteLine($"  Junctions: {sch.Junctions.Count}");
Console.WriteLine($"  No-connects: {sch.NoConnects.Count}");

foreach (var wire in sch.Wires)
{
    var w = (KiCadSchWire)wire;
    var start = w.Vertices[0];
    var end = w.Vertices[^1];
    Console.WriteLine($"  Wire: ({start.X.ToMm():F2}, {start.Y.ToMm():F2}) -> ({end.X.ToMm():F2}, {end.Y.ToMm():F2})");
}

foreach (var nl in sch.NetLabels)
    Console.WriteLine($"  Net label: \"{nl.Text}\" at ({nl.Location.X.ToMm():F2}, {nl.Location.Y.ToMm():F2})");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Load a PCB Document (.kicad_pcb)                                    ║
// ║                                                                         ║
// ║  PcbReader.ReadAsync() returns a KiCadPcb with the full board layout:   ║
// ║  footprints, copper traces, vias, zones, text, and net definitions.     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading PCB (.kicad_pcb) ===");

var pcb = await PcbReader.ReadAsync(Path.Combine(tempDir, "Test.kicad_pcb"));

Console.WriteLine($"  Version: {pcb.Version}");
Console.WriteLine($"  Board thickness: {pcb.BoardThickness.ToMm():F1} mm");
Console.WriteLine($"  Components: {pcb.Components.Count}");
Console.WriteLine($"  Tracks: {pcb.Tracks.Count}");
Console.WriteLine($"  Vias: {pcb.Vias.Count}");
Console.WriteLine($"  Nets: {pcb.Nets.Count}");

foreach (var net in pcb.Nets)
    Console.WriteLine($"  Net {net.Number}: \"{net.Name}\"");

foreach (var track in pcb.Tracks)
{
    var t = (KiCadPcbTrack)track;
    Console.WriteLine($"  Track: ({t.Start.X.ToMm():F2}, {t.Start.Y.ToMm():F2}) -> " +
                      $"({t.End.X.ToMm():F2}, {t.End.Y.ToMm():F2}) w={t.Width.ToMm():F2}mm layer={t.LayerName}");
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  5. Load from a Stream (in-memory)                                      ║
// ║                                                                         ║
// ║  All readers accept a Stream, so you can load from byte arrays,         ║
// ║  HTTP responses, embedded resources, zip archives, etc.                 ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading from Stream ===");

var fileBytes = await File.ReadAllBytesAsync(Path.Combine(tempDir, "Test.kicad_sym"));
using var memStream = new MemoryStream(fileBytes);
var memLib = await SymLibReader.ReadAsync(memStream);
Console.WriteLine($"  Loaded {memLib.Count} symbol(s) from memory stream");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  6. Diagnostics                                                         ║
// ║                                                                         ║
// ║  Non-fatal warnings encountered during parsing are collected in the     ║
// ║  Diagnostics property rather than throwing exceptions. This allows      ║
// ║  partially-valid files to be loaded.                                    ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Diagnostics ===");

if (symLib.Diagnostics.Count > 0)
{
    foreach (var diag in symLib.Diagnostics)
        Console.WriteLine($"  [{diag.Severity}] {diag.Message}");
}
else
{
    Console.WriteLine("  No diagnostics (clean load)");
}

// Clean up
Directory.Delete(tempDir, recursive: true);
Console.WriteLine("\nDone!");

// ── Helper: Create minimal test files ─────────────────────────────────────
// These create small but valid KiCad files so the example is self-contained.

static async Task CreateTestFilesAsync(string dir)
{
    // Symbol library with a resistor
    var symLib = new KiCadSymLib
    {
        Version = 20231120,
        Generator = "test",
        GeneratorVersion = "1.0"
    };
    var resistor = new KiCadSchComponent
    {
        Name = "R",
        Description = "Resistor",
        InBom = true,
        OnBoard = true
    };
    resistor.AddParameter(new KiCadSchParameter { Name = "Reference", Value = "R", FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });
    resistor.AddParameter(new KiCadSchParameter { Name = "Value", Value = "R", FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });

    var rGraphics = new KiCadSchComponent { Name = "R_0_1" };
    rGraphics.AddRectangle(new KiCadSchRectangle { Corner1 = new CoordPoint(Coord.FromMm(-1.016), Coord.FromMm(-2.54)), Corner2 = new CoordPoint(Coord.FromMm(1.016), Coord.FromMm(2.54)), LineWidth = Coord.FromMm(0.254), FillType = SchFillType.Background, IsFilled = true });
    resistor.AddSubSymbol(rGraphics);

    var rPins = new KiCadSchComponent { Name = "R_1_1" };
    rPins.AddPin(new KiCadSchPin { Name = "~", Designator = "1", Location = new CoordPoint(Coord.Zero, Coord.FromMm(3.81)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Down, ElectricalType = PinElectricalType.Passive });
    rPins.AddPin(new KiCadSchPin { Name = "~", Designator = "2", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-3.81)), Length = Coord.FromMm(1.27), Orientation = PinOrientation.Up, ElectricalType = PinElectricalType.Passive });
    resistor.AddSubSymbol(rPins);
    symLib.Add(resistor);
    await SymLibWriter.WriteAsync(symLib, Path.Combine(dir, "Test.kicad_sym"));

    // Footprint
    var fp = new KiCadPcbComponent
    {
        Name = "R_0805",
        Description = "0805 Resistor",
        LayerName = "F.Cu",
        Attributes = FootprintAttribute.Smd
    };
    fp.AddPad(new KiCadPcbPad { Designator = "1", Location = new CoordPoint(Coord.FromMm(-0.9), Coord.Zero), Size = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(1.4)), Shape = PadShape.RoundRect, PadType = PadType.Smd, CornerRadiusPercentage = 25, Layers = ["F.Cu", "F.Paste", "F.Mask"] });
    fp.AddPad(new KiCadPcbPad { Designator = "2", Location = new CoordPoint(Coord.FromMm(0.9), Coord.Zero), Size = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(1.4)), Shape = PadShape.RoundRect, PadType = PadType.Smd, CornerRadiusPercentage = 25, Layers = ["F.Cu", "F.Paste", "F.Mask"] });
    fp.AddTrack(new KiCadPcbTrack { Start = new CoordPoint(Coord.FromMm(-0.26), Coord.FromMm(-0.71)), End = new CoordPoint(Coord.FromMm(0.26), Coord.FromMm(-0.71)), Width = Coord.FromMm(0.12), LayerName = "F.SilkS" });
    fp.AddText(new KiCadPcbText { Text = "REF**", TextType = "reference", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-1.5)), Height = Coord.FromMm(1.0), LayerName = "F.SilkS" });
    await FootprintWriter.WriteAsync(fp, Path.Combine(dir, "Test.kicad_mod"));

    // Schematic
    var sch = new KiCadSch
    {
        Version = 20231120,
        Generator = "test",
        Uuid = Guid.NewGuid().ToString("D")
    };
    sch.AddWire(new KiCadSchWire { Vertices = [new CoordPoint(Coord.FromMm(127), Coord.FromMm(80)), new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66))], Uuid = Guid.NewGuid().ToString("D") });
    sch.AddNetLabel(new KiCadSchNetLabel { Text = "VCC", Location = new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66)), Uuid = Guid.NewGuid().ToString("D") });
    sch.AddJunction(new KiCadSchJunction { Location = new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66)), Size = Coord.FromMm(0.9), Uuid = Guid.NewGuid().ToString("D") });
    await sch.SaveAsync(Path.Combine(dir, "Test.kicad_sch"));

    // PCB
    var pcb = new KiCadPcb
    {
        Version = 20231014,
        Generator = "test",
        BoardThickness = Coord.FromMm(1.6)
    };
    pcb.AddNet(0, "");
    pcb.AddNet(1, "VCC");
    pcb.AddNet(2, "GND");
    pcb.AddTrack(new KiCadPcbTrack { Start = new CoordPoint(Coord.FromMm(100), Coord.FromMm(100)), End = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)), Width = Coord.FromMm(0.25), LayerName = "F.Cu", Net = 1, Uuid = Guid.NewGuid().ToString("D") });
    pcb.AddVia(new KiCadPcbVia { Location = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)), Diameter = Coord.FromMm(0.8), HoleSize = Coord.FromMm(0.4), StartLayerName = "F.Cu", EndLayerName = "B.Cu", Net = 1, Uuid = Guid.NewGuid().ToString("D") });
    await pcb.SaveAsync(Path.Combine(dir, "Test.kicad_pcb"));
}
