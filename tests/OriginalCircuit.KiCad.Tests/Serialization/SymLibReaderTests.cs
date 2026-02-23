using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class SymLibReaderTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task ReadAsync_MinimalSymLib_ParsesCorrectly()
    {
        var lib = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));

        lib.Should().NotBeNull();
        lib.Version.Should().Be(20231120);
        lib.Generator.Should().Be("kicadsharp");
        lib.Count.Should().Be(1);
    }

    [Fact]
    public async Task ReadAsync_MinimalSymLib_ParsesSymbolProperties()
    {
        var lib = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym = lib["R"];

        sym.Should().NotBeNull();
        sym!.Name.Should().Be("R");
        sym.InBom.Should().BeTrue();
        sym.OnBoard.Should().BeTrue();
        sym.Description.Should().Be("Resistor");
    }

    [Fact]
    public async Task ReadAsync_MinimalSymLib_ParsesPins()
    {
        var lib = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym = lib["R"]!;

        sym.Pins.Should().HaveCount(2);
        var pin1 = sym.Pins.OfType<KiCadSchPin>().First(p => p.Designator == "1");
        pin1.ElectricalType.Should().Be(PinElectricalType.Passive);
        pin1.GraphicStyle.Should().Be(PinGraphicStyle.Line);
        pin1.Orientation.Should().Be(PinOrientation.Down);
    }

    [Fact]
    public async Task ReadAsync_MinimalSymLib_ParsesRectangle()
    {
        var lib = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym = lib["R"]!;

        sym.Rectangles.Should().HaveCount(1);
        var rect = sym.Rectangles[0] as KiCadSchRectangle;
        rect.Should().NotBeNull();
        rect!.FillType.Should().Be(SchFillType.Background);
    }

    [Fact]
    public async Task ReadAsync_MinimalSymLib_ParsesParameters()
    {
        var lib = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym = lib["R"]!;

        sym.Parameters.Should().HaveCountGreaterThanOrEqualTo(2);
        var refParam = sym.Parameters.First(p => p.Name == "Reference");
        refParam.Value.Should().Be("R");
    }

    [Fact]
    public async Task ReadAsync_FromStream_WorksCorrectly()
    {
        await using var stream = File.OpenRead(TestDataPath("minimal.kicad_sym"));
        var lib = await SymLibReader.ReadAsync(stream);

        lib.Count.Should().Be(1);
        lib["R"].Should().NotBeNull();
    }
}
