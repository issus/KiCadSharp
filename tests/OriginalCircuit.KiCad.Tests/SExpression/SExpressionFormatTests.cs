using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;
using OriginalCircuit.KiCad.SExpression;
using Xunit;

namespace OriginalCircuit.KiCad.Tests.SExpression;

/// <summary>
/// Tests for S-expression formatting fixes that improve round-trip fidelity
/// with KiCad's native file output.
/// </summary>
public class SExpressionFormatTests
{
    // ── S-02: Negative zero preservation ─────────────────────────────

    [Fact]
    public void FormatNumber_NegativeZero_PreservesNegativeSign()
    {
        var result = new SExprNumber(-0.0).ToString();
        result.Should().Be("-0");
    }

    [Fact]
    public void FormatNumber_PositiveZero_EmitsPlainZero()
    {
        var result = new SExprNumber(0.0).ToString();
        result.Should().Be("0");
    }

    [Fact]
    public void Write_NegativeZeroValue_EmitsMinusZero()
    {
        var expr = new SExpressionBuilder("offset")
            .AddValue(-0.0)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(offset -0)");
    }

    // ── S-04: Angle always emitted in position ──────────────────────

    [Fact]
    public async Task BuildPosition_AngleZero_StillEmitsAngle()
    {
        // Create a footprint with a pad at origin with zero rotation
        var fp = new KiCadPcbComponent
        {
            Name = "TestFP",
            LayerName = "F.Cu",
            Location = CoordPoint.Zero,
            Rotation = 0
        };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "1",
            PadType = PadType.Smd,
            Shape = PadShape.Rect,
            Size = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Layers = ["F.Cu"],
            Location = CoordPoint.Zero,
            Rotation = 0
        });

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        // The (at 0 0 0) pattern should appear - angle is always emitted
        text.Should().Contain("(at 0 0 0)");
    }

    [Fact]
    public async Task BuildPosition_NonZeroAngle_EmitsAngle()
    {
        var fp = new KiCadPcbComponent
        {
            Name = "TestFP",
            LayerName = "F.Cu",
            Location = CoordPoint.Zero,
            Rotation = 90
        };

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        text.Should().Contain("(at 0 0 90)");
    }

    // ── S-07: Pad layer names as symbols ────────────────────────────

    [Fact]
    public async Task PadLayers_EmittedAsSymbols_NotQuotedStrings()
    {
        var fp = new KiCadPcbComponent
        {
            Name = "TestFP",
            LayerName = "F.Cu"
        };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "1",
            PadType = PadType.Smd,
            Shape = PadShape.Rect,
            Size = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Layers = ["F.Cu", "F.Paste", "F.Mask"]
        });

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        // Layers should be unquoted symbols, not quoted strings
        text.Should().Contain("(layers F.Cu F.Paste F.Mask)");
        text.Should().NotContain("\"F.Cu\"");
        text.Should().NotContain("\"F.Paste\"");
        text.Should().NotContain("\"F.Mask\"");
    }

    // ── S-12: UUID as unquoted symbol ───────────────────────────────

    [Fact]
    public async Task Uuid_EmittedAsSymbol_NotQuotedString()
    {
        var fp = new KiCadPcbComponent
        {
            Name = "TestFP",
            LayerName = "F.Cu"
        };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "1",
            PadType = PadType.Smd,
            Shape = PadShape.Rect,
            Size = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Layers = ["F.Cu"],
            Uuid = "12345678-1234-1234-1234-123456789abc"
        });

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        // UUID should be an unquoted symbol
        text.Should().Contain("(uuid 12345678-1234-1234-1234-123456789abc)");
        text.Should().NotContain("\"12345678-1234-1234-1234-123456789abc\"");
    }

    // ── S-11: PCB version default ───────────────────────────────────

    [Fact]
    public async Task PcbWriter_DefaultVersion_Is20231120()
    {
        var pcb = new KiCadPcb();

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        text.Should().Contain("(version 20231120)");
        text.Should().NotContain("(version 20231014)");
    }

    // ── Via layers as symbols ───────────────────────────────────────

    [Fact]
    public async Task ViaLayers_EmittedAsSymbols_NotQuotedStrings()
    {
        var pcb = new KiCadPcb();
        pcb.AddVia(new KiCadPcbVia
        {
            Location = CoordPoint.Zero,
            Diameter = Coord.FromMm(0.8),
            HoleSize = Coord.FromMm(0.4),
            StartLayerName = "F.Cu",
            EndLayerName = "B.Cu",
            Net = 1,
            Uuid = "00000000-0000-0000-0000-000000000001"
        });

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();

        // Via layer names should be unquoted symbols
        text.Should().Contain("(layers F.Cu B.Cu)");
        text.Should().NotContain("\"F.Cu\"");
        text.Should().NotContain("\"B.Cu\"");
    }
}
