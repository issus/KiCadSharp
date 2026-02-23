using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class FootprintReaderTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task ReadAsync_MinimalFootprint_ParsesCorrectly()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Should().NotBeNull();
        fp.Name.Should().Be("R_0805_2012Metric");
        fp.LayerName.Should().Be("F.Cu");
        fp.Description.Should().Be("Resistor SMD 0805");
        fp.Tags.Should().Be("resistor smd");
    }

    [Fact]
    public async Task ReadAsync_MinimalFootprint_ParsesPads()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Pads.Should().HaveCount(2);

        var pad1 = fp.Pads[0] as KiCadPcbPad;
        pad1.Should().NotBeNull();
        pad1!.Designator.Should().Be("1");
        pad1.PadType.Should().Be(PadType.Smd);
        pad1.Shape.Should().Be(PadShape.RoundRect);
        pad1.CornerRadiusPercentage.Should().Be(24); // 0.243902 * 100 truncated
    }

    [Fact]
    public async Task ReadAsync_MinimalFootprint_ParsesTexts()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Texts.Should().HaveCount(2);
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.Text.Should().Be("REF**");
    }

    [Fact]
    public async Task ReadAsync_MinimalFootprint_ParsesLines()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadAsync_MinimalFootprint_ParsesAttributes()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Attributes.Should().HaveFlag(FootprintAttribute.Smd);
    }

    [Fact]
    public async Task ReadAsync_MinimalFootprint_Parses3DModel()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        fp.Model3D.Should().Contain("R_0805_2012Metric.wrl");
    }
}
