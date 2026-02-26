using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class ReaderNegativeTests
{
    [Fact]
    public async Task ReadSymLib_WrongRootToken_ThrowsKiCadFileException()
    {
        var input = "(wrong_root (version 20231120))"u8.ToArray();
        using var ms = new MemoryStream(input);

        var act = async () => await SymLibReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>();
    }

    [Fact]
    public async Task ReadSch_WrongRootToken_ThrowsKiCadFileException()
    {
        var input = "(wrong_root (version 20231120))"u8.ToArray();
        using var ms = new MemoryStream(input);

        var act = async () => await SchReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>();
    }

    [Fact]
    public async Task ReadPcb_WrongRootToken_ThrowsKiCadFileException()
    {
        var input = "(wrong_root (version 20231120))"u8.ToArray();
        using var ms = new MemoryStream(input);

        var act = async () => await PcbReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>();
    }

    [Fact]
    public async Task ReadFootprint_WrongRootToken_ThrowsKiCadFileException()
    {
        var input = "(wrong_root (version 20231120))"u8.ToArray();
        using var ms = new MemoryStream(input);

        var act = async () => await FootprintReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>();
    }

    [Fact]
    public async Task ReadSymLib_MalformedSExpression_ThrowsKiCadFileException()
    {
        var input = "(kicad_symbol_lib (version 20231120) unclosed"u8.ToArray();
        using var ms = new MemoryStream(input);

        var act = async () => await SymLibReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>()
            .Where(e => e.InnerException is FormatException);
    }

    [Fact]
    public async Task ReadSch_EmptyFile_ThrowsKiCadFileException()
    {
        using var ms = new MemoryStream(""u8.ToArray());

        var act = async () => await SchReader.ReadAsync(ms);
        await act.Should().ThrowAsync<KiCadFileException>();
    }

    [Fact]
    public async Task ReadPcb_WithUnknownToken_ProducesDiagnostic()
    {
        var input = """
            (kicad_pcb
              (version 20231120)
              (generator "test")
              (completely_unknown_token 42)
            )
            """u8.ToArray();
        using var ms = new MemoryStream(input);

        var pcb = await PcbReader.ReadAsync(ms);
        pcb.Diagnostics.Should().Contain(d => d.Message.Contains("completely_unknown_token"));
    }

    [Fact]
    public async Task ReadSch_WithUnknownToken_ProducesDiagnostic()
    {
        var input = """
            (kicad_sch
              (version 20231120)
              (generator "test")
              (completely_unknown_token 42)
            )
            """u8.ToArray();
        using var ms = new MemoryStream(input);

        var sch = await SchReader.ReadAsync(ms);
        sch.Diagnostics.Should().Contain(d => d.Message.Contains("completely_unknown_token"));
    }
}
