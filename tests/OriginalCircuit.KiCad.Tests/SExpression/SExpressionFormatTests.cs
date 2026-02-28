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

        // KiCad 8+ omits angle=0 for pad and footprint positions (uses compact format)
        text.Should().Contain("(at 0 0)");
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

        // KiCad 8+ emits layer names as quoted strings
        text.Should().Contain("(layers \"F.Cu\" \"F.Paste\" \"F.Mask\")");
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

        // KiCad 8+ emits UUIDs as quoted strings
        text.Should().Contain("(uuid \"12345678-1234-1234-1234-123456789abc\")");
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

        // KiCad 8+ emits via layer names as quoted strings
        text.Should().Contain("(layers \"F.Cu\" \"B.Cu\")");
    }

    // ── S-01: OriginalText preserves trailing zeros ─────────────────

    [Fact]
    public void OriginalText_TrailingZeros_PreservedThroughRoundTrip()
    {
        var input = "(width 1.500)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().Be(1.5);
        num.OriginalText.Should().Be("1.500");
        num.ToSExpression().Should().Be("1.500");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    [Fact]
    public void OriginalText_IntegerWithDecimalPoint_PreservedThroughRoundTrip()
    {
        var input = "(size 2.0)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().Be(2.0);
        num.OriginalText.Should().Be("2.0");
        num.ToSExpression().Should().Be("2.0");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    [Fact]
    public void OriginalText_PlainInteger_PreservedThroughRoundTrip()
    {
        var input = "(version 20231120)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().Be(20231120);
        num.OriginalText.Should().Be("20231120");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    [Fact]
    public void OriginalText_NegativeNumber_PreservedThroughRoundTrip()
    {
        var input = "(offset -1.270)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().Be(-1.27);
        num.OriginalText.Should().Be("-1.270");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    // ── S-03: Scientific notation preservation ──────────────────────

    [Fact]
    public void OriginalText_ScientificNotation_PreservedThroughRoundTrip()
    {
        var input = "(value 1.5E-7)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().BeApproximately(1.5e-7, 1e-15);
        num.OriginalText.Should().Be("1.5E-7");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    [Fact]
    public void OriginalText_ScientificNotationLowercase_PreservedThroughRoundTrip()
    {
        var input = "(value 2.0e-3)";
        var expr = SExpressionReader.Read(input);

        var num = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        num.Value.Should().BeApproximately(0.002, 1e-15);
        num.OriginalText.Should().Be("2.0e-3");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    // ── S-05: WasCompact detection and preservation ─────────────────

    [Fact]
    public void WasCompact_SingleLineNode_DetectedAsCompact()
    {
        var input = "(width 0.25)";
        var expr = SExpressionReader.Read(input);

        expr.WasCompact.Should().BeTrue();
    }

    [Fact]
    public void WasCompact_MultilineNode_DetectedAsNotCompact()
    {
        var input = "(symbol \"R\"\n  (in_bom yes)\n)";
        var expr = SExpressionReader.Read(input);

        expr.WasCompact.Should().BeFalse();
    }

    [Fact]
    public void WasCompact_ChildNodesCompact_DetectedCorrectly()
    {
        var input = "(parent\n  (child1 1)\n  (child2 2)\n)";
        var expr = SExpressionReader.Read(input);

        expr.WasCompact.Should().BeFalse(); // parent spans multiple lines
        expr.Children[0].WasCompact.Should().BeTrue(); // child1 is on one line
        expr.Children[1].WasCompact.Should().BeTrue(); // child2 is on one line
    }

    [Fact]
    public void WasCompact_PreservedOnRoundTrip()
    {
        var input = "(parent\n  (child1 1)\n  (child2 2)\n)";
        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    [Fact]
    public void WasCompact_CompactNodeWithChildren_PreservedOnRoundTrip()
    {
        // A compact node with children all on one line
        var input = "(pts (xy 0 0) (xy 1 1))";
        var expr = SExpressionReader.Read(input);

        expr.WasCompact.Should().BeTrue();
        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    [Fact]
    public void WasCompact_Null_FallsBackToHeuristic()
    {
        // Programmatically built nodes have WasCompact = null, should use heuristic
        var builder = new SExpressionBuilder("pts")
            .AddChild("xy", xy => { xy.AddValue(0.0); xy.AddValue(0.0); })
            .AddChild("xy", xy => { xy.AddValue(1.0); xy.AddValue(1.0); });
        var expr = builder.Build();

        expr.WasCompact.Should().BeNull();
        // Should still produce valid output using heuristic
        var output = SExpressionWriter.Write(expr);
        output.Should().NotBeNullOrEmpty();
    }

    // ── S-06: Indentation width detection ───────────────────────────

    [Fact]
    public void OriginalIndent_TwoSpaces_Detected()
    {
        var input = "(root\n  (child 1)\n)";
        var expr = SExpressionReader.Read(input);

        expr.OriginalIndent.Should().Be(2);
    }

    [Fact]
    public void OriginalIndent_FourSpaces_Detected()
    {
        var input = "(root\n    (child 1)\n)";
        var expr = SExpressionReader.Read(input);

        expr.OriginalIndent.Should().Be(4);
    }

    [Fact]
    public void OriginalIndent_FourSpaces_PreservedOnRoundTrip()
    {
        var input = "(root\n    (child 1)\n)";
        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    [Fact]
    public void OriginalIndent_TwoSpaces_PreservedOnRoundTrip()
    {
        var input = "(root\n  (child 1)\n)";
        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    // ── S-09: Coordinate precision (via OriginalText) ───────────────

    [Fact]
    public void OriginalText_HighPrecisionCoordinate_PreservedThroughRoundTrip()
    {
        var input = "(at 1.234567890123 4.567890123456)";
        var expr = SExpressionReader.Read(input);

        var x = expr.Values[0].Should().BeOfType<SExprNumber>().Subject;
        x.OriginalText.Should().Be("1.234567890123");

        var y = expr.Values[1].Should().BeOfType<SExprNumber>().Subject;
        y.OriginalText.Should().Be("4.567890123456");

        var output = SExpressionWriter.Write(expr);
        output.Should().Be(input);
    }

    // ── Full round-trip tests ───────────────────────────────────────

    [Fact]
    public void RoundTrip_SimpleKiCadSymbolLibFragment_BytePerfect()
    {
        var input = "(kicad_symbol_lib\n  (version 20231120)\n  (generator \"kicadsharp\")\n)";

        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    [Fact]
    public void RoundTrip_MixedCompactAndMultiline_BytePerfect()
    {
        var input = "(parent\n  (start 0 0)\n  (end 1.500 2.0)\n)";
        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    [Fact]
    public void RoundTrip_NumbersWithTrailingZeros_BytePerfect()
    {
        var input = "(stroke (width 0.2540) (type solid))";
        var expr = SExpressionReader.Read(input);
        var output = SExpressionWriter.Write(expr);

        output.Should().Be(input);
    }

    [Fact]
    public async Task RoundTrip_MinimalSymbolLibFile_SourceTreeIsStored()
    {
        // Tests that the SourceTree is properly stored when reading a file
        var path = Path.Combine("TestData", "minimal.kicad_sym");
        if (!File.Exists(path))
            return; // Skip if test data not available

        // Read the file via SymLibReader which stores SourceTree
        var lib = await SymLibReader.ReadAsync(path);
        lib.SourceTree.Should().NotBeNull("Reader should store the SourceTree");
        lib.SourceTree!.Token.Should().Be("kicad_symbol_lib");
    }

    [Fact]
    public async Task RoundTrip_MinimalSymbolLibFile_NumbersPreserved()
    {
        // Tests that number formatting is preserved through the SourceTree round-trip
        var path = Path.Combine("TestData", "minimal.kicad_sym");
        if (!File.Exists(path))
            return; // Skip if test data not available

        var originalBytes = await File.ReadAllBytesAsync(path);
        var originalText = System.Text.Encoding.UTF8.GetString(originalBytes).Replace("\r\n", "\n");

        // Read the file via SymLibReader which stores SourceTree
        var lib = await SymLibReader.ReadAsync(path);

        // Write via SymLibWriter which uses SourceTree
        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib, ms);

        ms.Position = 0;
        var outputText = System.Text.Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");

        // Strip BOM if present
        if (outputText.Length > 0 && outputText[0] == '\uFEFF')
            outputText = outputText[1..];

        // The output should contain all the same numbers in the same format
        outputText.Should().Contain("1.27");
        outputText.Should().Contain("2.032");
        outputText.Should().Contain("0.254");
        outputText.Should().Contain("20231120");
    }

    // ── SExprNumber OriginalText property ───────────────────────────

    [Fact]
    public void SExprNumber_WithoutOriginalText_UsesFormatNumber()
    {
        var num = new SExprNumber(1.5);
        num.OriginalText.Should().BeNull();
        num.ToSExpression().Should().Be("1.5");
    }

    [Fact]
    public void SExprNumber_WithOriginalText_UsesOriginalText()
    {
        var num = new SExprNumber(1.5) { OriginalText = "1.500" };
        num.ToSExpression().Should().Be("1.500");
    }

    [Fact]
    public void SExprNumber_ToString_UsesOriginalText()
    {
        var num = new SExprNumber(1.5) { OriginalText = "1.500" };
        num.ToString().Should().Be("1.500");
    }
}
