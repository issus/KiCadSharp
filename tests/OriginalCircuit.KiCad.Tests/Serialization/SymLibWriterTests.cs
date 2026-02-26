using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class SymLibWriterTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task Write_MinimalSymLib_ProducesReparsableOutput()
    {
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        lib2.Count.Should().Be(lib1.Count);
    }

    [Fact]
    public async Task Write_SymbolProperties_PreservedCorrectly()
    {
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym1 = lib1["R"]!;

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        var sym2 = lib2["R"]!;

        sym2.Name.Should().Be(sym1.Name);
        sym2.InBom.Should().Be(sym1.InBom);
        sym2.OnBoard.Should().Be(sym1.OnBoard);
        sym2.Description.Should().Be(sym1.Description);
    }

    [Fact]
    public async Task Write_PinAttributes_PreservedCorrectly()
    {
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym1 = lib1["R"]!;

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        var sym2 = lib2["R"]!;

        sym2.Pins.Count.Should().Be(sym1.Pins.Count);
        for (int i = 0; i < sym1.Pins.Count; i++)
        {
            var pin1 = (KiCadSchPin)sym1.Pins[i];
            var pin2 = (KiCadSchPin)sym2.Pins[i];
            pin2.Designator.Should().Be(pin1.Designator);
            pin2.ElectricalType.Should().Be(pin1.ElectricalType);
            pin2.GraphicStyle.Should().Be(pin1.GraphicStyle);
        }
    }

    [Fact]
    public async Task Write_ParameterPositions_PreservedCorrectly()
    {
        var lib1 = await SymLibReader.ReadAsync(TestDataPath("minimal.kicad_sym"));
        var sym1 = lib1["R"]!;

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib1, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        var sym2 = lib2["R"]!;

        sym2.Parameters.Count.Should().Be(sym1.Parameters.Count);
        for (int i = 0; i < sym1.Parameters.Count; i++)
        {
            sym2.Parameters[i].Name.Should().Be(sym1.Parameters[i].Name);
            sym2.Parameters[i].Value.Should().Be(sym1.Parameters[i].Value);
        }
    }

    [Fact]
    public async Task Write_EmptySymLib_ProducesValidOutput()
    {
        var lib = new KiCadSymLib();

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib, ms);
        ms.Position = 0;

        var lib2 = await SymLibReader.ReadAsync(ms);
        lib2.Count.Should().Be(0);
    }
}
