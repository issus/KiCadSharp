using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.KiCad.Models.Pcb;
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

    [Fact]
    public async Task ReadFootprint_OutOfRangeZoneConnect_DefaultsToInherited()
    {
        var input = """
            (footprint "test"
              (layer "F.Cu")
              (pad "1" smd rect (at 0 0) (size 1 1)
                (layers "F.Cu")
                (zone_connect 99)
              )
            )
            """u8.ToArray();
        using var ms = new MemoryStream(input);

        var fp = await FootprintReader.ReadAsync(ms);
        var pad = (KiCadPcbPad)fp.Pads[0];
        pad.ZoneConnect.Should().Be(ZoneConnectionType.Inherited);
    }

    [Fact]
    public async Task ReadSch_ColorClamping_HandlesOutOfRangeValues()
    {
        // Test that color values > 255 are clamped (no overflow)
        var input = """
            (kicad_sch
              (version 20231120)
              (generator "test")
              (wire (pts (xy 0 0) (xy 10 0))
                (stroke (width 0) (type solid) (color 300 -10 256 2.0))
                (uuid "00000000-0000-0000-0000-000000000001")
              )
            )
            """u8.ToArray();
        using var ms = new MemoryStream(input);

        var sch = await SchReader.ReadAsync(ms);
        sch.Wires.Count.Should().Be(1);
        // Should not throw — color values are clamped
    }

    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task ConcurrentParsing_DoesNotCorruptState()
    {
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            var fp = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));
            fp.Name.Should().NotBeNullOrEmpty();
        }));

        await Task.WhenAll(tasks);
    }
}
