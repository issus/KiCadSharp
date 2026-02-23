using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class SchReaderTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesCorrectly()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.Should().NotBeNull();
        sch.Version.Should().Be(20231120);
        sch.Uuid.Should().Be("12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesWires()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.Wires.Should().HaveCount(1);
        var wire = sch.Wires[0] as KiCadSchWire;
        wire.Should().NotBeNull();
        wire!.Vertices.Should().HaveCount(2);
        wire.Uuid.Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesJunctions()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.Junctions.Should().HaveCount(1);
        var junction = sch.Junctions[0] as KiCadSchJunction;
        junction.Should().NotBeNull();
        junction!.Uuid.Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesNetLabels()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.NetLabels.Should().HaveCount(1);
        sch.NetLabels[0].Text.Should().Be("NET1");
    }

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesNoConnects()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.NoConnects.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadAsync_MinimalSch_ParsesTextLabels()
    {
        var sch = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        sch.Labels.Should().HaveCount(1);
        sch.Labels[0].Text.Should().Be("Hello World");
    }
}
