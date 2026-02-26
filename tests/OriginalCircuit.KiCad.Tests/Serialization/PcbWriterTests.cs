using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class PcbWriterTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task Write_MinimalPcb_ProducesReparsableOutput()
    {
        var pcb1 = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.Components.Count.Should().Be(pcb1.Components.Count);
        pcb2.Tracks.Count.Should().Be(pcb1.Tracks.Count);
        pcb2.Vias.Count.Should().Be(pcb1.Vias.Count);
    }

    [Fact]
    public async Task Write_BoardThickness_PreservedCorrectly()
    {
        var pcb1 = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.BoardThickness.ToMm().Should().BeApproximately(pcb1.BoardThickness.ToMm(), 0.01);
    }

    [Fact]
    public async Task Write_Nets_PreservedCorrectly()
    {
        var pcb1 = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.Nets.Count.Should().Be(pcb1.Nets.Count);
    }

    [Fact]
    public async Task Write_EmptyPcb_ProducesValidOutput()
    {
        var pcb = new KiCadPcb();

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.Components.Count.Should().Be(0);
    }
}
