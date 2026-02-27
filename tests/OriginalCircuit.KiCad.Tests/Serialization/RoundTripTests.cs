using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class RoundTripTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task SymLib_RoundTrip_PreservesData()
    {
        // Read
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));

        // Write to stream
        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        // Read back
        var lib2 = await SymLibReader.ReadAsync(ms);

        // Compare
        lib2.Count.Should().Be(lib1.Count);
        lib2["R"].Should().NotBeNull();
        lib2["R"]!.Name.Should().Be(lib1["R"]!.Name);
        lib2["R"]!.InBom.Should().Be(lib1["R"]!.InBom);
        lib2["R"]!.OnBoard.Should().Be(lib1["R"]!.OnBoard);
        lib2["R"]!.Pins.Count.Should().Be(lib1["R"]!.Pins.Count);
    }

    [Fact]
    public async Task Footprint_RoundTrip_PreservesData()
    {
        // Read
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        // Write to stream
        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        // Read back
        var fp2 = await FootprintReader.ReadAsync(ms);

        // Compare
        fp2.Name.Should().Be(fp1.Name);
        fp2.LayerName.Should().Be(fp1.LayerName);
        fp2.Description.Should().Be(fp1.Description);
        fp2.Pads.Count.Should().Be(fp1.Pads.Count);
        fp2.Texts.Count.Should().Be(fp1.Texts.Count);
    }

    [Fact]
    public async Task Pcb_RoundTrip_PreservesData()
    {
        // Read
        var pcb1 = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        // Write to stream
        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;

        // Read back
        var pcb2 = await PcbReader.ReadAsync(ms);

        // Compare
        pcb2.Components.Count.Should().Be(pcb1.Components.Count);
        pcb2.Tracks.Count.Should().Be(pcb1.Tracks.Count);
        pcb2.Vias.Count.Should().Be(pcb1.Vias.Count);
        pcb2.Nets.Count.Should().Be(pcb1.Nets.Count);
        pcb2.BoardThickness.ToMm().Should().BeApproximately(pcb1.BoardThickness.ToMm(), 0.01);
    }

    [Fact]
    public async Task Sch_RoundTrip_PreservesData()
    {
        // Read
        var sch1 = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        // Write to stream
        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        // Read back
        var sch2 = await SchReader.ReadAsync(ms);

        // Compare
        sch2.Wires.Count.Should().Be(sch1.Wires.Count);
        sch2.Junctions.Count.Should().Be(sch1.Junctions.Count);
        sch2.NetLabels.Count.Should().Be(sch1.NetLabels.Count);
        sch2.NoConnects.Count.Should().Be(sch1.NoConnects.Count);
        sch2.Labels.Count.Should().Be(sch1.Labels.Count);
    }

    [Fact]
    public async Task Sch_RoundTrip_PreservesLabelTypes()
    {
        var sch = new KiCadSch();
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "LocalNet", LabelType = NetLabelType.Local, Uuid = Guid.NewGuid().ToString("D") });
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "GlobalNet", LabelType = NetLabelType.Global, Uuid = Guid.NewGuid().ToString("D") });
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "HierNet", LabelType = NetLabelType.Hierarchical, Uuid = Guid.NewGuid().ToString("D") });

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.NetLabels.Count.Should().Be(3);

        var labels = sch2.NetLabels.Cast<KiCadSchNetLabel>().ToList();
        labels[0].LabelType.Should().Be(NetLabelType.Local);
        labels[0].Text.Should().Be("LocalNet");
        labels[1].LabelType.Should().Be(NetLabelType.Global);
        labels[1].Text.Should().Be("GlobalNet");
        labels[2].LabelType.Should().Be(NetLabelType.Hierarchical);
        labels[2].Text.Should().Be("HierNet");
    }

    [Fact]
    public async Task SymLib_RoundTrip_PreservesPinProperties()
    {
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        if (lib1.Count == 0) return;

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        var sym1 = lib1["R"];
        var sym2 = lib2["R"];
        sym1.Should().NotBeNull();
        sym2.Should().NotBeNull();
        if (sym1!.Pins.Count > 0)
        {
            var pin1 = sym1.Pins[0];
            var pin2 = sym2!.Pins[0];
            pin2.ElectricalType.Should().Be(pin1.ElectricalType);
            pin2.Location.X.ToMm().Should().BeApproximately(pin1.Location.X.ToMm(), 0.01);
            pin2.Location.Y.ToMm().Should().BeApproximately(pin1.Location.Y.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task Footprint_RoundTrip_PreservesPadProperties()
    {
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        if (fp1.Pads.Count > 0)
        {
            var pad1 = (KiCadPcbPad)fp1.Pads[0];
            var pad2 = (KiCadPcbPad)fp2.Pads[0];
            pad2.Shape.Should().Be(pad1.Shape);
            pad2.PadType.Should().Be(pad1.PadType);
            pad2.Size.X.ToMm().Should().BeApproximately(pad1.Size.X.ToMm(), 0.01);
            pad2.Size.Y.ToMm().Should().BeApproximately(pad1.Size.Y.ToMm(), 0.01);
            pad2.HoleSize.ToMm().Should().BeApproximately(pad1.HoleSize.ToMm(), 0.01);
            pad2.Location.X.ToMm().Should().BeApproximately(pad1.Location.X.ToMm(), 0.01);
            pad2.Location.Y.ToMm().Should().BeApproximately(pad1.Location.Y.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task Footprint_RoundTrip_PreservesPadSolderAndThermalProperties()
    {
        var fp = new KiCadPcbComponent { Name = "TestFP", LayerName = "F.Cu" };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "1",
            PadType = PadType.Smd,
            Shape = PadShape.Rect,
            Size = new CoordPoint(Coord.FromMm(2), Coord.FromMm(1)),
            Layers = ["F.Cu"],
            SolderPasteMargin = Coord.FromMm(0.1),
            SolderPasteRatio = 0.05,
            ThermalWidth = Coord.FromMm(0.3),
            ThermalGap = Coord.FromMm(0.4),
            ZoneConnect = ZoneConnectionType.Solid,
            DieLength = Coord.FromMm(0.5)
        });

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        var pad = (KiCadPcbPad)fp2.Pads[0];
        pad.SolderPasteMargin.ToMm().Should().BeApproximately(0.1, 0.01);
        pad.SolderPasteRatio.Should().BeApproximately(0.05, 0.001);
        pad.ThermalWidth.ToMm().Should().BeApproximately(0.3, 0.01);
        pad.ThermalGap.ToMm().Should().BeApproximately(0.4, 0.01);
        pad.ZoneConnect.Should().Be(ZoneConnectionType.Solid);
        pad.DieLength.ToMm().Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public async Task Pcb_RoundTrip_PreservesTrackAndViaProperties()
    {
        var pcb1 = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        if (pcb1.Tracks.Count > 0)
        {
            var t1 = (KiCadPcbTrack)pcb1.Tracks[0];
            var t2 = (KiCadPcbTrack)pcb2.Tracks[0];
            t2.Start.X.ToMm().Should().BeApproximately(t1.Start.X.ToMm(), 0.01);
            t2.Start.Y.ToMm().Should().BeApproximately(t1.Start.Y.ToMm(), 0.01);
            t2.End.X.ToMm().Should().BeApproximately(t1.End.X.ToMm(), 0.01);
            t2.End.Y.ToMm().Should().BeApproximately(t1.End.Y.ToMm(), 0.01);
            t2.Width.ToMm().Should().BeApproximately(t1.Width.ToMm(), 0.01);
        }
        if (pcb1.Vias.Count > 0)
        {
            var v1 = (KiCadPcbVia)pcb1.Vias[0];
            var v2 = (KiCadPcbVia)pcb2.Vias[0];
            v2.Location.X.ToMm().Should().BeApproximately(v1.Location.X.ToMm(), 0.01);
            v2.Location.Y.ToMm().Should().BeApproximately(v1.Location.Y.ToMm(), 0.01);
            v2.Diameter.ToMm().Should().BeApproximately(v1.Diameter.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task Sch_RoundTrip_PreservesWireAndJunctionProperties()
    {
        var sch1 = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        if (sch1.Wires.Count > 0)
        {
            var w1 = (KiCadSchWire)sch1.Wires[0];
            var w2 = (KiCadSchWire)sch2.Wires[0];
            w2.Vertices[0].X.ToMm().Should().BeApproximately(w1.Vertices[0].X.ToMm(), 0.01);
            w2.Vertices[0].Y.ToMm().Should().BeApproximately(w1.Vertices[0].Y.ToMm(), 0.01);
        }
        if (sch1.Junctions.Count > 0)
        {
            var j1 = (KiCadSchJunction)sch1.Junctions[0];
            var j2 = (KiCadSchJunction)sch2.Junctions[0];
            j2.Location.X.ToMm().Should().BeApproximately(j1.Location.X.ToMm(), 0.01);
            j2.Location.Y.ToMm().Should().BeApproximately(j1.Location.Y.ToMm(), 0.01);
        }
    }
}
