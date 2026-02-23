using FluentAssertions;
using OriginalCircuit.KiCad.SExpression;
using Xunit;

namespace OriginalCircuit.KiCad.Tests.SExpression;

public class SExpressionReaderTests
{
    [Fact]
    public void Read_SimpleTokenWithStringValue_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(token \"value\")");

        result.Token.Should().Be("token");
        result.Values.Should().HaveCount(1);
        result.Values[0].Should().BeOfType<SExprString>().Which.Value.Should().Be("value");
    }

    [Fact]
    public void Read_NestedExpression_ParsesParentAndChild()
    {
        var result = SExpressionReader.Read("(parent (child \"value\"))");

        result.Token.Should().Be("parent");
        result.Children.Should().HaveCount(1);
        result.Children[0].Token.Should().Be("child");
        result.Children[0].Values[0].Should().BeOfType<SExprString>().Which.Value.Should().Be("value");
    }

    [Fact]
    public void Read_NumericValue_ParsesAsNumber()
    {
        var result = SExpressionReader.Read("(width 0.25)");

        result.Token.Should().Be("width");
        result.GetDouble().Should().Be(0.25);
    }

    [Fact]
    public void Read_NegativeNumber_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(offset -1.27)");

        result.Token.Should().Be("offset");
        result.GetDouble().Should().Be(-1.27);
    }

    [Fact]
    public void Read_BooleanYes_ParsesAsSymbol()
    {
        var result = SExpressionReader.Read("(in_bom yes)");

        result.Token.Should().Be("in_bom");
        result.GetBool().Should().BeTrue();
    }

    [Fact]
    public void Read_BooleanNo_ParsesAsSymbol()
    {
        var result = SExpressionReader.Read("(on_board no)");

        result.Token.Should().Be("on_board");
        result.GetBool().Should().BeFalse();
    }

    [Fact]
    public void Read_CoordinatePair_ParsesXY()
    {
        var result = SExpressionReader.Read("(at 2.54 -1.27 90)");

        result.Token.Should().Be("at");
        var xy = result.GetXY();
        xy.Should().NotBeNull();
        xy!.Value.X.Should().Be(2.54);
        xy.Value.Y.Should().Be(-1.27);
        result.GetDouble(2).Should().Be(90);
    }

    [Fact]
    public void Read_DeeplyNested_HandlesWithoutStackOverflow()
    {
        // Build a deeply nested expression: (a (a (a ... )))
        var depth = 200;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            sb.Append("(a ");
        }
        for (var i = 0; i < depth; i++)
        {
            sb.Append(')');
        }

        var result = SExpressionReader.Read(sb.ToString());

        // Walk to the innermost node
        var current = result;
        for (var i = 0; i < depth - 1; i++)
        {
            current.Children.Should().HaveCount(1);
            current = current.Children[0];
        }
        current.Token.Should().Be("a");
        current.Children.Should().BeEmpty();
    }

    [Fact]
    public void Read_EmptyExpression_ParsesTokenOnly()
    {
        var result = SExpressionReader.Read("(token)");

        result.Token.Should().Be("token");
        result.Values.Should().BeEmpty();
        result.Children.Should().BeEmpty();
    }

    [Fact]
    public void Read_MultipleChildrenWithSameToken_ReturnsAll()
    {
        var result = SExpressionReader.Read("(parent (child 1) (child 2) (child 3))");

        result.Children.Should().HaveCount(3);
        result.GetChildren("child").Should().HaveCount(3);
        result.GetChild("child")!.GetDouble().Should().Be(1);
    }

    [Fact]
    public void Read_QuotedStringWithSpaces_PreservesSpaces()
    {
        var result = SExpressionReader.Read("(property \"Reference\" \"R1\")");

        result.Token.Should().Be("property");
        result.GetString(0).Should().Be("Reference");
        result.GetString(1).Should().Be("R1");
    }

    [Fact]
    public void Read_QuotedStringWithEscapedQuote_HandlesEscape()
    {
        var result = SExpressionReader.Read("(text \"say \\\"hello\\\"\")");

        result.Token.Should().Be("text");
        result.GetString().Should().Be("say \"hello\"");
    }

    [Fact]
    public void Read_MixedValuesAndChildren_ParsesBoth()
    {
        var input = "(symbol \"R\" (in_bom yes) (on_board yes))";
        var result = SExpressionReader.Read(input);

        result.Token.Should().Be("symbol");
        result.GetString(0).Should().Be("R");
        result.Children.Should().HaveCount(2);
        result.GetChild("in_bom")!.GetBool().Should().BeTrue();
        result.GetChild("on_board")!.GetBool().Should().BeTrue();
    }

    [Fact]
    public void Read_RealKiCadSymbolSnippet_ParsesCorrectly()
    {
        var input = """
            (symbol "R" (pin_names (offset 0)) (in_bom yes) (on_board yes)
              (property "Reference" "R" (at 2.032 0 90)
                (effects (font (size 1.27 1.27)))
              )
            )
            """;

        var result = SExpressionReader.Read(input);

        result.Token.Should().Be("symbol");
        result.GetString(0).Should().Be("R");

        var pinNames = result.GetChild("pin_names");
        pinNames.Should().NotBeNull();
        pinNames!.GetChild("offset")!.GetDouble().Should().Be(0);

        var property = result.GetChild("property");
        property.Should().NotBeNull();
        property!.GetString(0).Should().Be("Reference");
        property.GetString(1).Should().Be("R");

        var at = property.GetChild("at");
        at.Should().NotBeNull();
        at!.GetDouble(0).Should().Be(2.032);
        at.GetDouble(1).Should().Be(0);
        at.GetDouble(2).Should().Be(90);
    }

    [Fact]
    public void Read_RealKiCadFootprintSnippet_ParsesCorrectly()
    {
        var input = """
            (footprint "Resistor_SMD:R_0402_1005Metric"
              (layer "F.Cu")
              (at 100.5 50.2 180)
              (pad "1" smd roundrect
                (at -0.48 0)
                (size 0.56 0.62)
                (layers "F.Cu" "F.Paste" "F.Mask")
                (roundrect_rratio 0.25)
              )
            )
            """;

        var result = SExpressionReader.Read(input);

        result.Token.Should().Be("footprint");
        result.GetString(0).Should().Be("Resistor_SMD:R_0402_1005Metric");

        var layer = result.GetChild("layer");
        layer.Should().NotBeNull();
        layer!.GetString(0).Should().Be("F.Cu");

        var pad = result.GetChild("pad");
        pad.Should().NotBeNull();
        pad!.GetString(0).Should().Be("1");
        pad.Values[1].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("smd");
        pad.Values[2].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("roundrect");

        var padAt = pad.GetChild("at");
        padAt!.GetXY().Should().Be((-0.48, 0.0));
    }

    [Fact]
    public void Read_IntegerValue_ParsesViaGetInt()
    {
        var result = SExpressionReader.Read("(net 5)");

        result.GetInt().Should().Be(5);
    }

    [Fact]
    public void Read_UnquotedSymbol_ParsesAsSymbol()
    {
        var result = SExpressionReader.Read("(type solid)");

        result.Token.Should().Be("type");
        result.Values[0].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("solid");
    }

    [Fact]
    public void Read_GetString_ReturnsNullForOutOfRange()
    {
        var result = SExpressionReader.Read("(token)");
        result.GetString(0).Should().BeNull();
        result.GetString(99).Should().BeNull();
    }

    [Fact]
    public void Read_GetDouble_ReturnsNullForNonNumeric()
    {
        var result = SExpressionReader.Read("(token \"text\")");
        result.GetDouble().Should().BeNull();
    }

    [Fact]
    public void Read_GetBool_ReturnsNullForNonBoolSymbol()
    {
        var result = SExpressionReader.Read("(token solid)");
        result.GetBool().Should().BeNull();
    }

    [Fact]
    public void Read_GetChild_ReturnsNullWhenNotFound()
    {
        var result = SExpressionReader.Read("(parent (child 1))");
        result.GetChild("missing").Should().BeNull();
    }

    [Fact]
    public void Read_WhitespaceVariations_ParsesCorrectly()
    {
        var input = "(  token  \r\n  \"value\"  \n  (child  1)  )";
        var result = SExpressionReader.Read(input);

        result.Token.Should().Be("token");
        result.GetString(0).Should().Be("value");
        result.Children.Should().HaveCount(1);
    }

    [Fact]
    public void Read_EscapedNewlineInString_PreservesNewline()
    {
        var result = SExpressionReader.Read("(text \"line1\\nline2\")");
        result.GetString().Should().Be("line1\nline2");
    }

    [Fact]
    public void Read_ZeroValue_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(offset 0)");
        result.GetDouble().Should().Be(0);
    }

    [Fact]
    public void Read_InvalidInput_NoOpenParen_ThrowsFormatException()
    {
        var act = () => SExpressionReader.Read("token value");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_InvalidInput_UnterminatedString_ThrowsFormatException()
    {
        var act = () => SExpressionReader.Read("(token \"unterminated)");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_InvalidInput_MissingCloseParen_ThrowsFormatException()
    {
        var act = () => SExpressionReader.Read("(token \"value\"");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public async Task ReadAsync_FromStream_ParsesCorrectly()
    {
        var input = "(width 0.25)";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));

        var result = await SExpressionReader.ReadAsync(stream);

        result.Token.Should().Be("width");
        result.GetDouble().Should().Be(0.25);
    }
}
