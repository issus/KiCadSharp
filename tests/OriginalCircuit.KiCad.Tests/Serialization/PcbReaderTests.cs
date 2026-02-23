using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class PcbReaderTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesCorrectly()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Should().NotBeNull();
        pcb.Version.Should().Be(20231014);
        pcb.Generator.Should().Be("kicadsharp");
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesNets()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Nets.Should().HaveCount(3);
        pcb.Nets[1].Name.Should().Be("GND");
        pcb.Nets[2].Name.Should().Be("VCC");
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesFootprints()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Components.Should().HaveCount(1);
        var fp = pcb.Components[0] as KiCadPcbComponent;
        fp.Should().NotBeNull();
        fp!.Name.Should().Be("R_0805_2012Metric");
        fp.Pads.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesTracks()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Tracks.Should().HaveCount(1);
        var track = pcb.Tracks[0] as KiCadPcbTrack;
        track.Should().NotBeNull();
        track!.LayerName.Should().Be("F.Cu");
        track.Net.Should().Be(1);
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesVias()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Vias.Should().HaveCount(1);
        var via = pcb.Vias[0] as KiCadPcbVia;
        via.Should().NotBeNull();
        via!.StartLayerName.Should().Be("F.Cu");
        via.EndLayerName.Should().Be("B.Cu");
        via.Net.Should().Be(1);
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesTexts()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.Texts.Should().HaveCount(1);
        var text = pcb.Texts[0] as KiCadPcbText;
        text.Should().NotBeNull();
        text!.Text.Should().Be("Board Title");
    }

    [Fact]
    public async Task ReadAsync_MinimalPcb_ParsesBoardThickness()
    {
        var pcb = await PcbReader.ReadAsync(TestDataPath("minimal.kicad_pcb"));

        pcb.BoardThickness.ToMm().Should().BeApproximately(1.6, 0.01);
    }
}
