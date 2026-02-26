// ============================================================================
// Example: Creating KiCad Files from Scratch
// ============================================================================
//
// This example demonstrates creating all four KiCad file types:
//
//   .kicad_sym  - Symbol library (schematic symbols for circuit diagrams)
//   .kicad_mod  - Footprint (physical land pattern for soldering)
//   .kicad_sch  - Schematic document (a circuit diagram sheet)
//   .kicad_pcb  - PCB document (a board layout with routed copper)
//
// KEY CONCEPTS
// ────────────
// All coordinates use the Coord struct, a fixed-point integer internally.
// KiCad uses millimeters natively — use Coord.FromMm() for all values.
// CoordPoint pairs an X and Y Coord for 2D positions.
//
// KiCad files are S-expression (Lisp-like) text files. The writers produce
// properly formatted output that KiCad can open directly.
//
// Readers and writers are static async methods:
//   await SymLibWriter.WriteAsync(lib, "path.kicad_sym");
//   var lib = await SymLibReader.ReadAsync("path.kicad_sym");
//
// Models can also save themselves:
//   await lib.SaveAsync("path.kicad_sym");
//
// ============================================================================

using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

var outputDir = Path.Combine(Path.GetTempPath(), "KiCadExamples");
Directory.CreateDirectory(outputDir);
Console.WriteLine($"Output directory: {outputDir}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Symbol Library (.kicad_sym)                                         ║
// ║                                                                         ║
// ║  A symbol library contains one or more schematic symbols. Each symbol   ║
// ║  (KiCadSchComponent) has pins, graphical primitives (rectangles, lines, ║
// ║  arcs, circles, polylines), and properties (Reference, Value, etc.).    ║
// ║                                                                         ║
// ║  KiCad symbols use a two-level structure: the root component holds      ║
// ║  pins and properties, while SubSymbols hold the graphical body. The     ║
// ║  naming convention is "Name_0_1" for the graphical unit and             ║
// ║  "Name_1_1" for the unit with pins.                                     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating Symbol Library ===");

var symLib = new KiCadSymLib
{
    Version = 20231120,
    Generator = "KiCadSharpExample",
    GeneratorVersion = "1.0"
};

// ── Resistor symbol: rectangle body + 2 passive pins ────────────────────────
//
// Pin positions are relative to the component origin (0,0).
// Orientation specifies which direction the pin line extends from the body.
// PinElectricalType affects ERC (Electrical Rules Check) in KiCad.

var resistor = new KiCadSchComponent
{
    Name = "R",
    Description = "Resistor",
    InBom = true,
    OnBoard = true,
    PinNamesOffset = Coord.FromMm(1.0)
};
resistor.AddParameter(new KiCadSchParameter { Name = "Reference", Value = "R", Location = new CoordPoint(Coord.FromMm(2.032), Coord.Zero), Orientation = 90, FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });
resistor.AddParameter(new KiCadSchParameter { Name = "Value", Value = "R", Location = new CoordPoint(Coord.FromMm(-2.032), Coord.Zero), Orientation = 90, FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });

// Graphics unit: the rectangle body
var resistorGraphics = new KiCadSchComponent { Name = "R_0_1" };
resistorGraphics.AddRectangle(new KiCadSchRectangle
{
    Corner1 = new CoordPoint(Coord.FromMm(-1.016), Coord.FromMm(-2.54)),
    Corner2 = new CoordPoint(Coord.FromMm(1.016), Coord.FromMm(2.54)),
    LineWidth = Coord.FromMm(0.254),
    FillType = SchFillType.Background,
    IsFilled = true
});
resistor.AddSubSymbol(resistorGraphics);

// Pin unit: pins live here
var resistorPins = new KiCadSchComponent { Name = "R_1_1" };
resistorPins.AddPin(new KiCadSchPin
{
    Name = "~",
    Designator = "1",
    Location = new CoordPoint(Coord.Zero, Coord.FromMm(3.81)),
    Length = Coord.FromMm(1.27),
    Orientation = PinOrientation.Down,
    ElectricalType = PinElectricalType.Passive,
    GraphicStyle = PinGraphicStyle.Line
});
resistorPins.AddPin(new KiCadSchPin
{
    Name = "~",
    Designator = "2",
    Location = new CoordPoint(Coord.Zero, Coord.FromMm(-3.81)),
    Length = Coord.FromMm(1.27),
    Orientation = PinOrientation.Up,
    ElectricalType = PinElectricalType.Passive,
    GraphicStyle = PinGraphicStyle.Line
});
resistor.AddSubSymbol(resistorPins);

symLib.Add(resistor);

// ── Op-amp symbol: polyline triangle body + 3 pins ──────────────────────────

var opamp = new KiCadSchComponent
{
    Name = "OPA",
    Description = "Operational Amplifier",
    InBom = true,
    OnBoard = true
};
opamp.AddParameter(new KiCadSchParameter { Name = "Reference", Value = "U", Location = new CoordPoint(Coord.FromMm(5.08), Coord.FromMm(3.81)), FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });
opamp.AddParameter(new KiCadSchParameter { Name = "Value", Value = "OPA", Location = new CoordPoint(Coord.FromMm(5.08), Coord.FromMm(-3.81)), FontSizeWidth = Coord.FromMm(1.27), FontSizeHeight = Coord.FromMm(1.27) });

// Graphics unit: triangle body using a polygon
var opampGraphics = new KiCadSchComponent { Name = "OPA_0_1" };
opampGraphics.AddPolygon(new KiCadSchPolygon
{
    Vertices =
    [
        new CoordPoint(Coord.FromMm(-2.54), Coord.FromMm(-5.08)),
        new CoordPoint(Coord.FromMm(-2.54), Coord.FromMm(5.08)),
        new CoordPoint(Coord.FromMm(5.08), Coord.Zero),
        new CoordPoint(Coord.FromMm(-2.54), Coord.FromMm(-5.08))
    ],
    LineWidth = Coord.FromMm(0.254),
    FillType = SchFillType.Background,
    IsFilled = true
});
opamp.AddSubSymbol(opampGraphics);

// Pin unit
var opampPins = new KiCadSchComponent { Name = "OPA_1_1" };
opampPins.AddPin(new KiCadSchPin
{
    Name = "+",
    Designator = "1",
    Location = new CoordPoint(Coord.FromMm(-5.08), Coord.FromMm(2.54)),
    Length = Coord.FromMm(2.54),
    Orientation = PinOrientation.Right,
    ElectricalType = PinElectricalType.Input,
    GraphicStyle = PinGraphicStyle.Line
});
opampPins.AddPin(new KiCadSchPin
{
    Name = "-",
    Designator = "2",
    Location = new CoordPoint(Coord.FromMm(-5.08), Coord.FromMm(-2.54)),
    Length = Coord.FromMm(2.54),
    Orientation = PinOrientation.Right,
    ElectricalType = PinElectricalType.Input,
    GraphicStyle = PinGraphicStyle.Line
});
opampPins.AddPin(new KiCadSchPin
{
    Name = "OUT",
    Designator = "3",
    Location = new CoordPoint(Coord.FromMm(7.62), Coord.Zero),
    Length = Coord.FromMm(2.54),
    Orientation = PinOrientation.Left,
    ElectricalType = PinElectricalType.Output,
    GraphicStyle = PinGraphicStyle.Line
});
opamp.AddSubSymbol(opampPins);

symLib.Add(opamp);

// Save using the static writer
var symLibPath = Path.Combine(outputDir, "MySymbols.kicad_sym");
await SymLibWriter.WriteAsync(symLib, symLibPath);

Console.WriteLine($"  Created: {symLibPath}");
Console.WriteLine($"  Components: {symLib.Count}");
foreach (var comp in symLib.Components)
    Console.WriteLine($"    - {comp.Name}: {comp.Description}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Footprint (.kicad_mod)                                              ║
// ║                                                                         ║
// ║  A footprint defines the physical copper pattern on a PCB. It contains  ║
// ║  pads (connection points), tracks (silkscreen/courtyard lines), text    ║
// ║  labels (reference, value), and optional 3D model references.           ║
// ║                                                                         ║
// ║  KiCad PCB layers use string names:                                     ║
// ║    "F.Cu" / "B.Cu"       — Front/back copper                           ║
// ║    "F.SilkS" / "B.SilkS" — Silkscreen                                 ║
// ║    "F.Mask" / "B.Mask"    — Solder mask openings                       ║
// ║    "F.Paste" / "B.Paste"  — Solder paste stencil                       ║
// ║    "F.CrtYd" / "B.CrtYd"  — Courtyard (assembly spacing)              ║
// ║    "F.Fab" / "B.Fab"     — Fabrication (assembly drawing)              ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating Footprint ===");

// SMD 0805 resistor footprint
var resistorFp = new KiCadPcbComponent
{
    Name = "R_0805_2012Metric",
    Description = "Resistor SMD 0805 (2012 Metric)",
    LayerName = "F.Cu",
    Tags = "resistor smd 0805",
    Attributes = FootprintAttribute.Smd
};
resistorFp.AddProperty(new KiCadSchParameter { Name = "Reference", Value = "REF**", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-1.65)), FontSizeWidth = Coord.FromMm(1.0), FontSizeHeight = Coord.FromMm(1.0) });
resistorFp.AddProperty(new KiCadSchParameter { Name = "Value", Value = "R_0805", Location = new CoordPoint(Coord.Zero, Coord.FromMm(1.65)), FontSizeWidth = Coord.FromMm(1.0), FontSizeHeight = Coord.FromMm(1.0) });
resistorFp.AddPad(new KiCadPcbPad
{
    Designator = "1",
    Location = new CoordPoint(Coord.FromMm(-0.9125), Coord.Zero),
    Size = new CoordPoint(Coord.FromMm(1.025), Coord.FromMm(1.4)),
    Shape = PadShape.RoundRect,
    PadType = PadType.Smd,
    CornerRadiusPercentage = 25,
    Layers = ["F.Cu", "F.Paste", "F.Mask"]
});
resistorFp.AddPad(new KiCadPcbPad
{
    Designator = "2",
    Location = new CoordPoint(Coord.FromMm(0.9125), Coord.Zero),
    Size = new CoordPoint(Coord.FromMm(1.025), Coord.FromMm(1.4)),
    Shape = PadShape.RoundRect,
    PadType = PadType.Smd,
    CornerRadiusPercentage = 25,
    Layers = ["F.Cu", "F.Paste", "F.Mask"]
});
resistorFp.AddTrack(new KiCadPcbTrack { Start = new CoordPoint(Coord.FromMm(-0.261252), Coord.FromMm(-0.71)), End = new CoordPoint(Coord.FromMm(0.261252), Coord.FromMm(-0.71)), Width = Coord.FromMm(0.12), LayerName = "F.SilkS" });
resistorFp.AddTrack(new KiCadPcbTrack { Start = new CoordPoint(Coord.FromMm(-0.261252), Coord.FromMm(0.71)), End = new CoordPoint(Coord.FromMm(0.261252), Coord.FromMm(0.71)), Width = Coord.FromMm(0.12), LayerName = "F.SilkS" });
resistorFp.AddText(new KiCadPcbText { Text = "REF**", TextType = "reference", Location = new CoordPoint(Coord.Zero, Coord.FromMm(-1.65)), Height = Coord.FromMm(1.0), LayerName = "F.SilkS" });
resistorFp.AddText(new KiCadPcbText { Text = "R_0805", TextType = "value", Location = new CoordPoint(Coord.Zero, Coord.FromMm(1.65)), Height = Coord.FromMm(1.0), LayerName = "F.Fab" });

var fpPath = Path.Combine(outputDir, "R_0805_2012Metric.kicad_mod");
await FootprintWriter.WriteAsync(resistorFp, fpPath);

Console.WriteLine($"  Created: {fpPath}");
Console.WriteLine($"  Pads: {resistorFp.Pads.Count}");
Console.WriteLine($"  Tracks: {resistorFp.Tracks.Count}");
Console.WriteLine($"  Bounds: {resistorFp.Bounds.Width.ToMm():F2} x {resistorFp.Bounds.Height.ToMm():F2} mm");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Schematic Document (.kicad_sch)                                     ║
// ║                                                                         ║
// ║  A schematic document is a circuit diagram sheet. It contains placed    ║
// ║  component instances, wires for electrical connections, net labels,     ║
// ║  junctions, power symbols, and hierarchical sheet references.           ║
// ║                                                                         ║
// ║  Component instances reference symbols from the embedded lib_symbols    ║
// ║  section. Wires create connections between pins. Net labels assign      ║
// ║  names to nets for inter-sheet connectivity.                            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating Schematic ===");

var sch = new KiCadSch
{
    Version = 20231120,
    Generator = "KiCadSharpExample",
    GeneratorVersion = "1.0",
    Uuid = Guid.NewGuid().ToString("D")
};
// Wires create electrical connections between pins
sch.AddWire(new KiCadSchWire
{
    Vertices = [new CoordPoint(Coord.FromMm(127), Coord.FromMm(80.01)), new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66))],
    Uuid = Guid.NewGuid().ToString("D")
});
sch.AddWire(new KiCadSchWire
{
    Vertices = [new CoordPoint(Coord.FromMm(127), Coord.FromMm(95.25)), new CoordPoint(Coord.FromMm(127), Coord.FromMm(101.6))],
    Uuid = Guid.NewGuid().ToString("D")
});
// Net labels name wire segments
sch.AddNetLabel(new KiCadSchNetLabel
{
    Text = "VCC",
    Location = new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66)),
    Uuid = Guid.NewGuid().ToString("D")
});
// Junctions mark intentional wire connections
sch.AddJunction(new KiCadSchJunction
{
    Location = new CoordPoint(Coord.FromMm(127), Coord.FromMm(73.66)),
    Size = Coord.FromMm(0.9),
    Uuid = Guid.NewGuid().ToString("D")
});

var schPath = Path.Combine(outputDir, "MySchematic.kicad_sch");
await sch.SaveAsync(schPath);

Console.WriteLine($"  Created: {schPath}");
Console.WriteLine($"  Wires: {sch.Wires.Count}");
Console.WriteLine($"  Net labels: {sch.NetLabels.Count}");
Console.WriteLine($"  Junctions: {sch.Junctions.Count}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. PCB Document (.kicad_pcb)                                           ║
// ║                                                                         ║
// ║  A PCB document represents a physical board layout. It contains:        ║
// ║    - Net definitions (named electrical connections)                     ║
// ║    - Footprint instances (placed components)                           ║
// ║    - Tracks (copper traces connecting pads)                            ║
// ║    - Vias (connections between copper layers)                          ║
// ║    - Zones/regions (copper pours)                                      ║
// ║    - Text and graphical items                                          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating PCB ===");

var pcb = new KiCadPcb
{
    Version = 20231014,
    Generator = "KiCadSharpExample",
    GeneratorVersion = "1.0",
    BoardThickness = Coord.FromMm(1.6)
};
// Net definitions
pcb.AddNet(0, "");
pcb.AddNet(1, "VCC");
pcb.AddNet(2, "GND");
pcb.AddNet(3, "SIG");
// Copper traces
pcb.AddTrack(new KiCadPcbTrack
{
    Start = new CoordPoint(Coord.FromMm(100), Coord.FromMm(100)),
    End = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)),
    Width = Coord.FromMm(0.25),
    LayerName = "F.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
pcb.AddTrack(new KiCadPcbTrack
{
    Start = new CoordPoint(Coord.FromMm(110), Coord.FromMm(100)),
    End = new CoordPoint(Coord.FromMm(110), Coord.FromMm(110)),
    Width = Coord.FromMm(0.25),
    LayerName = "F.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
// Vias connect copper between layers
pcb.AddVia(new KiCadPcbVia
{
    Location = new CoordPoint(Coord.FromMm(110), Coord.FromMm(110)),
    Diameter = Coord.FromMm(0.8),
    HoleSize = Coord.FromMm(0.4),
    StartLayerName = "F.Cu",
    EndLayerName = "B.Cu",
    Net = 1,
    Uuid = Guid.NewGuid().ToString("D")
});
// Board text
pcb.AddText(new KiCadPcbText
{
    Text = "KiCadSharp Example Board",
    Location = new CoordPoint(Coord.FromMm(105), Coord.FromMm(95)),
    Height = Coord.FromMm(1.5),
    LayerName = "F.SilkS",
    Uuid = Guid.NewGuid().ToString("D")
});

var pcbPath = Path.Combine(outputDir, "MyBoard.kicad_pcb");
await pcb.SaveAsync(pcbPath);

Console.WriteLine($"  Created: {pcbPath}");
Console.WriteLine($"  Nets: {pcb.Nets.Count}");
Console.WriteLine($"  Tracks: {pcb.Tracks.Count}");
Console.WriteLine($"  Vias: {pcb.Vias.Count}");
Console.WriteLine($"  Board thickness: {pcb.BoardThickness.ToMm():F1} mm");

Console.WriteLine($"\nAll files created in: {outputDir}");
