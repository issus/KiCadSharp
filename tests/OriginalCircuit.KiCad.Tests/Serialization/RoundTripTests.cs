using Xunit;
using FluentAssertions;
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
}
