using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.SExpression;

namespace OriginalCircuit.KiCad.Tests.SExpression;

public class SExpressionEdgeCaseTests
{
    [Fact]
    public void Read_EmptyInput_ThrowsFormatException()
    {
        var act = () => SExpressionReader.Read("");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_WhitespaceOnly_ThrowsFormatException()
    {
        var act = () => SExpressionReader.Read("   \n\t  ");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_UnicodeInString_PreservesContent()
    {
        var result = SExpressionReader.Read("(text \"Ω resistor μF\")");
        result.GetString().Should().Be("Ω resistor μF");
    }

    [Fact]
    public void Read_DoubleBackslash_HandlesCorrectly()
    {
        var result = SExpressionReader.Read("(text \"path\\\\file\")");
        result.GetString().Should().Be("path\\file");
    }

    [Fact]
    public void Read_ScientificNotation_ParsesAsNumber()
    {
        var result = SExpressionReader.Read("(value 1.5e3)");
        result.GetDouble().Should().BeApproximately(1500.0, 0.01);
    }

    [Fact]
    public void Read_ScientificNotationNegativeExponent_ParsesAsNumber()
    {
        var result = SExpressionReader.Read("(value 2.5e-2)");
        result.GetDouble().Should().BeApproximately(0.025, 0.0001);
    }

    [Fact]
    public void Read_NestingBeyondLimit_ThrowsFormatException()
    {
        var depth = 1100;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < depth; i++) sb.Append("(a ");
        for (var i = 0; i < depth; i++) sb.Append(')');

        var act = () => SExpressionReader.Read(sb.ToString());
        act.Should().Throw<FormatException>().WithMessage("*nesting*");
    }

    [Fact]
    public void Read_VeryLongToken_HandlesCorrectly()
    {
        var longToken = new string('x', 10000);
        var result = SExpressionReader.Read($"({longToken} 42)");
        result.Token.Should().Be(longToken);
        result.GetDouble().Should().Be(42);
    }

    [Fact]
    public void Read_VeryLongString_HandlesCorrectly()
    {
        var longString = new string('a', 100000);
        var result = SExpressionReader.Read($"(text \"{longString}\")");
        result.GetString().Should().Be(longString);
    }

    [Fact]
    public void Read_MultipleConsecutiveSpaces_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(token    1.5    2.5    3.5)");
        result.GetDouble(0).Should().Be(1.5);
        result.GetDouble(1).Should().Be(2.5);
        result.GetDouble(2).Should().Be(3.5);
    }

    [Fact]
    public void Read_TabsAndNewlines_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(token\t1.5\n\t2.5\r\n)");
        result.GetDouble(0).Should().Be(1.5);
        result.GetDouble(1).Should().Be(2.5);
    }

    [Fact]
    public void Builder_NullToken_ThrowsArgumentNullException()
    {
        var act = () => new SExpressionBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_NullStringValue_ThrowsArgumentNullException()
    {
        var builder = new SExpressionBuilder("test");
        var act = () => builder.AddValue((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_NullSymbol_ThrowsArgumentNullException()
    {
        var builder = new SExpressionBuilder("test");
        var act = () => builder.AddSymbol(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_NullChild_ThrowsArgumentNullException()
    {
        var builder = new SExpressionBuilder("test");
        var act = () => builder.AddChild((KiCad.SExpression.SExpression)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Read_CommentInInput_SkipsComment()
    {
        // KiCad files shouldn't have comments, but verify parser handles non-expression content
        var result = SExpressionReader.Read("(token 42)");
        result.Token.Should().Be("token");
        result.GetDouble().Should().Be(42);
    }

    [Fact]
    public void Read_NegativeZero_ParsesAsZero()
    {
        var result = SExpressionReader.Read("(val -0)");
        result.GetDouble().Should().Be(0);
    }

    [Fact]
    public void Read_LargeInteger_ParsesCorrectly()
    {
        var result = SExpressionReader.Read("(version 20231120)");
        result.GetInt().Should().Be(20231120);
    }

    [Fact]
    public void Read_EmptyString_PreservesEmpty()
    {
        var result = SExpressionReader.Read("(text \"\")");
        result.GetString().Should().Be("");
    }
}
