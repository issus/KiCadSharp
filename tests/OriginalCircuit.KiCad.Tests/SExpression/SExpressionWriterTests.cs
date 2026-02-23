using FluentAssertions;
using OriginalCircuit.KiCad.SExpression;
using Xunit;

namespace OriginalCircuit.KiCad.Tests.SExpression;

public class SExpressionWriterTests
{
    [Fact]
    public void Write_SimpleExpression_FormatsCorrectly()
    {
        var expr = new SExpressionBuilder("width")
            .AddValue(0.25)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(width 0.25)");
    }

    [Fact]
    public void Write_ExpressionWithStringValue_QuotesString()
    {
        var expr = new SExpressionBuilder("property")
            .AddValue("Reference")
            .AddValue("R1")
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(property \"Reference\" \"R1\")");
    }

    [Fact]
    public void Write_ExpressionWithSymbol_DoesNotQuoteSymbol()
    {
        var expr = new SExpressionBuilder("type")
            .AddSymbol("solid")
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(type solid)");
    }

    [Fact]
    public void Write_BooleanTrue_EmitsYes()
    {
        var expr = new SExpressionBuilder("in_bom")
            .AddBool(true)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(in_bom yes)");
    }

    [Fact]
    public void Write_BooleanFalse_EmitsNo()
    {
        var expr = new SExpressionBuilder("in_bom")
            .AddBool(false)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(in_bom no)");
    }

    [Fact]
    public void Write_NumberWithTrailingZeros_TrimsZeros()
    {
        var expr = new SExpressionBuilder("width")
            .AddValue(1.0)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(width 1)");
    }

    [Fact]
    public void Write_NumberWithDecimal_PreservesSignificantDigits()
    {
        var expr = new SExpressionBuilder("width")
            .AddValue(0.125)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(width 0.125)");
    }

    [Fact]
    public void Write_NestedExpression_IndentsChildren()
    {
        var expr = new SExpressionBuilder("parent")
            .AddChild("child1", b => b.AddValue(1.0))
            .AddChild("child2", b => b
                .AddChild("grandchild", g => g.AddValue("deep")))
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Contain("(child1 1)");
        result.Should().Contain("(child2");
        result.Should().Contain("(grandchild \"deep\")");
    }

    [Fact]
    public void Write_EmptyExpression_JustTokenAndParens()
    {
        var expr = new SExpressionBuilder("token").Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(token)");
    }

    [Fact]
    public void Write_StringWithEscapeCharacters_EscapesProperly()
    {
        var expr = new SExpressionBuilder("text")
            .AddValue("line1\nline2")
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Contain("\\n");
    }

    [Fact]
    public void Write_StringWithQuotes_EscapesQuotes()
    {
        var expr = new SExpressionBuilder("text")
            .AddValue("say \"hello\"")
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Contain("\\\"");
    }

    [Fact]
    public void Write_RoundTrip_SimpleExpression_PreservesData()
    {
        var input = "(width 0.25)";

        var parsed = SExpressionReader.Read(input);
        var written = SExpressionWriter.Write(parsed);
        var reparsed = SExpressionReader.Read(written);

        reparsed.Token.Should().Be(parsed.Token);
        reparsed.GetDouble().Should().Be(parsed.GetDouble());
    }

    [Fact]
    public void Write_RoundTrip_NestedExpression_PreservesStructure()
    {
        var input = """
            (symbol "R" (in_bom yes) (on_board yes)
              (property "Reference" "R" (at 2.032 0 90))
            )
            """;

        var parsed = SExpressionReader.Read(input);
        var written = SExpressionWriter.Write(parsed);
        var reparsed = SExpressionReader.Read(written);

        reparsed.Token.Should().Be("symbol");
        reparsed.GetString(0).Should().Be("R");
        reparsed.GetChild("in_bom")!.GetBool().Should().BeTrue();
        reparsed.GetChild("on_board")!.GetBool().Should().BeTrue();

        var prop = reparsed.GetChild("property");
        prop.Should().NotBeNull();
        prop!.GetString(0).Should().Be("Reference");
        prop.GetString(1).Should().Be("R");

        var at = prop.GetChild("at");
        at!.GetDouble(0).Should().Be(2.032);
        at.GetDouble(1).Should().Be(0);
        at.GetDouble(2).Should().Be(90);
    }

    [Fact]
    public void Write_RoundTrip_MultipleValues_PreservesAllValues()
    {
        var input = "(pad \"1\" smd roundrect)";

        var parsed = SExpressionReader.Read(input);
        var written = SExpressionWriter.Write(parsed);
        var reparsed = SExpressionReader.Read(written);

        reparsed.Token.Should().Be("pad");
        reparsed.GetString(0).Should().Be("1");
        reparsed.Values[1].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("smd");
        reparsed.Values[2].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("roundrect");
    }

    [Fact]
    public void Write_NegativeZero_EmitsZero()
    {
        var expr = new SExpressionBuilder("val")
            .AddValue(-0.0)
            .Build();

        var result = SExpressionWriter.Write(expr);

        result.Should().Be("(val 0)");
    }

    [Fact]
    public async Task WriteAsync_ToStream_ProducesValidOutput()
    {
        var expr = new SExpressionBuilder("width")
            .AddValue(0.25)
            .Build();

        using var stream = new MemoryStream();
        await SExpressionWriter.WriteAsync(expr, stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        text.Trim().Should().Be("(width 0.25)");
    }
}
