using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

/// <summary>
/// Round-trip fidelity tests for schematic (.kicad_sch) reading and writing.
/// </summary>
public class SchRoundTripTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    private static async Task<(KiCadSch Original, KiCadSch RoundTripped)> RoundTrip(string file)
    {
        var sch1 = await SchReader.ReadAsync(TestDataPath(file));

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        return (sch1, sch2);
    }

    // --- Phase A: Wire/Bus/BusEntry stroke pass-through ---

    [Fact]
    public async Task Wire_StrokeStyle_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Wires.Count.Should().Be(sch1.Wires.Count);

        var w1 = (KiCadSchWire)sch1.Wires[0];
        var w2 = (KiCadSchWire)sch2.Wires[0];
        w2.LineStyle.Should().Be(LineStyle.Dash);
    }

    [Fact]
    public async Task Wire_StrokeColor_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var w1 = (KiCadSchWire)sch1.Wires[0];
        var w2 = (KiCadSchWire)sch2.Wires[0];
        w2.Color.R.Should().Be(w1.Color.R);
        w2.Color.G.Should().Be(w1.Color.G);
        w2.Color.B.Should().Be(w1.Color.B);
    }

    [Fact]
    public async Task Bus_StrokeStyleAndColor_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Buses.Count.Should().Be(1);
        var b1 = (KiCadSchBus)sch1.Buses[0];
        var b2 = (KiCadSchBus)sch2.Buses[0];
        b2.LineStyle.Should().Be(LineStyle.Dot);
        b2.Color.B.Should().Be(255);
    }

    [Fact]
    public async Task BusEntry_StrokeStyle_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.BusEntries.Count.Should().Be(1);
        var be1 = (KiCadSchBusEntry)sch1.BusEntries[0];
        var be2 = (KiCadSchBusEntry)sch2.BusEntries[0];
        be2.LineStyle.Should().Be(LineStyle.DashDot);
    }

    // --- Phase B: Label/Text font fixes ---

    [Fact]
    public async Task NetLabel_FontProperties_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        // Find the label "NET1"
        var label1 = sch1.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "NET1");
        var label2 = sch2.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "NET1");

        label2.FontSizeHeight.ToMm().Should().BeApproximately(1.27, 0.01);
        label2.FontSizeWidth.ToMm().Should().BeApproximately(1.27, 0.01);
        label2.Justification.Should().Be(label1.Justification);
        label2.FieldsAutoplaced.Should().BeTrue();
    }

    [Fact]
    public async Task GlobalLabel_Shape_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var gl1 = sch1.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "VCC");
        var gl2 = sch2.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "VCC");

        gl2.Shape.Should().Be("input");
        gl2.IsBold.Should().BeTrue();
        gl2.IsItalic.Should().BeTrue();
        gl2.FontSizeHeight.ToMm().Should().BeApproximately(1.5, 0.01);
        gl2.FontSizeWidth.ToMm().Should().BeApproximately(1.5, 0.01);
    }

    [Fact]
    public async Task HierarchicalLabel_Shape_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var hl1 = sch1.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "DATA_BUS");
        var hl2 = sch2.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "DATA_BUS");

        hl2.Shape.Should().Be("bidirectional");
        hl2.LabelType.Should().Be(NetLabelType.Hierarchical);
    }

    [Fact]
    public async Task TextLabel_FontAndUuid_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Labels.Count.Should().Be(sch1.Labels.Count);
        var t1 = (KiCadSchLabel)sch1.Labels[0];
        var t2 = (KiCadSchLabel)sch2.Labels[0];

        t2.Text.Should().Be("Hello World");
        t2.FontSizeHeight.ToMm().Should().BeApproximately(2.0, 0.01);
        t2.FontSizeWidth.ToMm().Should().BeApproximately(2.0, 0.01);
        t2.IsBold.Should().BeTrue();
        t2.Uuid.Should().Be("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    // --- Phase C: Sheet fixes ---

    [Fact]
    public async Task Sheet_StrokeAndFill_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Sheets.Count.Should().Be(1);
        var s1 = sch1.Sheets[0];
        var s2 = sch2.Sheets[0];

        s2.LineStyle.Should().Be(LineStyle.Dash);
        s2.Color.R.Should().Be(128);
        s2.FillColor.R.Should().Be(200);
    }

    [Fact]
    public async Task Sheet_PropertyPositions_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var s1 = sch1.Sheets[0];
        var s2 = sch2.Sheets[0];

        s2.SheetProperties.Count.Should().BeGreaterThanOrEqualTo(2);
        var nameProp = s2.SheetProperties.First(p => p.Name == "Sheetname");
        nameProp.Value.Should().Be("SubSheet");
        nameProp.FontSizeHeight.ToMm().Should().BeApproximately(1.5, 0.01);
    }

    [Fact]
    public async Task Sheet_FieldsAutoplaced_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Sheets[0].FieldsAutoplaced.Should().BeTrue();
    }

    // --- Phase D: Power port writing ---

    [Fact]
    public async Task PowerPort_RoundTrip_Preserved()
    {
        // Create a schematic with a power port
        var sch = new KiCadSch();
        sch.AddPowerObject(new KiCadSchPowerObject
        {
            Text = "VDD",
            Location = new CoordPoint(Coord.FromMm(50), Coord.FromMm(25)),
            Rotation = 0,
            Uuid = "77777777-7777-7777-7777-777777777777"
        });

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("power_port");
        text.Should().Contain("VDD");
    }

    // --- Phase E: Placed symbol fixes ---

    [Fact]
    public async Task PlacedSymbol_MirrorXY_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var comp = sch2.Components.OfType<KiCadSchComponent>().First();
        comp.IsMirroredX.Should().BeTrue();
        comp.IsMirroredY.Should().BeTrue();
    }

    [Fact]
    public async Task PlacedSymbol_InBomOnBoard_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var comp = sch2.Components.OfType<KiCadSchComponent>().First();
        comp.InBom.Should().BeTrue();
        comp.OnBoard.Should().BeTrue();
    }

    [Fact]
    public async Task PlacedSymbol_Pins_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var comp1 = sch1.Components.OfType<KiCadSchComponent>().First();
        var comp2 = sch2.Components.OfType<KiCadSchComponent>().First();

        comp2.Pins.Count.Should().Be(comp1.Pins.Count);
        comp2.Pins.Count.Should().Be(2);
    }

    [Fact]
    public async Task PlacedSymbol_FieldsAutoplaced_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var comp = sch2.Components.OfType<KiCadSchComponent>().First();
        comp.FieldsAutoplaced.Should().BeTrue();
    }

    [Fact]
    public async Task PlacedSymbol_Instances_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var comp = sch2.Components.OfType<KiCadSchComponent>().First();
        comp.InstancesRaw.Should().NotBeNull();
    }

    // --- Phase F: Document-level tokens ---

    [Fact]
    public async Task DocumentLevel_Paper_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Paper.Should().Be("A4");
    }

    [Fact]
    public async Task DocumentLevel_TitleBlock_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.TitleBlock.Should().NotBeNull();
        sch2.TitleBlock!.Token.Should().Be("title_block");
    }

    [Fact]
    public async Task DocumentLevel_SheetInstances_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.SheetInstances.Should().NotBeNull();
        sch2.SheetInstances!.Token.Should().Be("sheet_instances");
    }

    [Fact]
    public async Task DocumentLevel_SymbolInstances_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.SymbolInstances.Should().NotBeNull();
        sch2.SymbolInstances!.Token.Should().Be("symbol_instances");
    }

    // --- Phase G: Graphical shapes ---

    [Fact]
    public async Task GraphicalShapes_Polyline_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Polylines.Count.Should().Be(sch1.Polylines.Count);
        if (sch2.Polylines.Count > 0)
        {
            var p1 = sch1.Polylines[0];
            var p2 = sch2.Polylines[0];
            p2.Vertices.Count.Should().Be(p1.Vertices.Count);
        }
    }

    [Fact]
    public async Task GraphicalShapes_Circle_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Circles.Count.Should().Be(sch1.Circles.Count);
        if (sch2.Circles.Count > 0)
        {
            var c1 = sch1.Circles[0];
            var c2 = sch2.Circles[0];
            c2.Center.X.ToMm().Should().BeApproximately(c1.Center.X.ToMm(), 0.01);
            c2.Center.Y.ToMm().Should().BeApproximately(c1.Center.Y.ToMm(), 0.01);
            c2.Radius.ToMm().Should().BeApproximately(c1.Radius.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task GraphicalShapes_Rectangle_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Rectangles.Count.Should().Be(sch1.Rectangles.Count);
        if (sch2.Rectangles.Count > 0)
        {
            var r1 = sch1.Rectangles[0];
            var r2 = sch2.Rectangles[0];
            r2.Corner1.X.ToMm().Should().BeApproximately(r1.Corner1.X.ToMm(), 0.01);
            r2.Corner2.X.ToMm().Should().BeApproximately(r1.Corner2.X.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task GraphicalShapes_Arc_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Arcs.Count.Should().Be(sch1.Arcs.Count);
        if (sch2.Arcs.Count > 0)
        {
            var a1 = sch1.Arcs[0];
            var a2 = sch2.Arcs[0];
            a2.ArcStart.X.ToMm().Should().BeApproximately(a1.ArcStart.X.ToMm(), 0.01);
            a2.ArcEnd.X.ToMm().Should().BeApproximately(a1.ArcEnd.X.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task GraphicalShapes_Bezier_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Beziers.Count.Should().Be(sch1.Beziers.Count);
        if (sch2.Beziers.Count > 0)
        {
            var b1 = sch1.Beziers[0];
            var b2 = sch2.Beziers[0];
            b2.ControlPoints.Count.Should().Be(b1.ControlPoints.Count);
        }
    }

    // --- Phase H: Label properties ---

    [Fact]
    public async Task GlobalLabel_Properties_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        var gl1 = sch1.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "VCC");
        var gl2 = sch2.NetLabels.OfType<KiCadSchNetLabel>().First(l => l.Text == "VCC");

        gl2.Properties.Count.Should().Be(gl1.Properties.Count);
        if (gl2.Properties.Count > 0)
        {
            gl2.Properties[0].Name.Should().Be("Intersheetrefs");
        }
    }

    // --- Overall round-trip test ---

    [Fact]
    public async Task FullRoundTrip_AllCounts_Preserved()
    {
        var (sch1, sch2) = await RoundTrip("roundtrip.kicad_sch");

        sch2.Version.Should().Be(sch1.Version);
        sch2.Uuid.Should().Be(sch1.Uuid);
        sch2.Wires.Count.Should().Be(sch1.Wires.Count);
        sch2.Junctions.Count.Should().Be(sch1.Junctions.Count);
        sch2.NetLabels.Count.Should().Be(sch1.NetLabels.Count);
        sch2.Labels.Count.Should().Be(sch1.Labels.Count);
        sch2.NoConnects.Count.Should().Be(sch1.NoConnects.Count);
        sch2.Buses.Count.Should().Be(sch1.Buses.Count);
        sch2.BusEntries.Count.Should().Be(sch1.BusEntries.Count);
        sch2.Components.Count.Should().Be(sch1.Components.Count);
        sch2.Sheets.Count.Should().Be(sch1.Sheets.Count);
        sch2.LibSymbols.Count.Should().Be(sch1.LibSymbols.Count);
        sch2.Polylines.Count.Should().Be(sch1.Polylines.Count);
        sch2.Circles.Count.Should().Be(sch1.Circles.Count);
        sch2.Rectangles.Count.Should().Be(sch1.Rectangles.Count);
        sch2.Arcs.Count.Should().Be(sch1.Arcs.Count);
        sch2.Beziers.Count.Should().Be(sch1.Beziers.Count);
    }
}
