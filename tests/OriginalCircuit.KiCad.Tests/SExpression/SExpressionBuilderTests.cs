using FluentAssertions;
using OriginalCircuit.KiCad.SExpression;
using Xunit;

namespace OriginalCircuit.KiCad.Tests.SExpression;

public class SExpressionBuilderTests
{
    [Fact]
    public void Build_EmptyExpression_HasTokenOnly()
    {
        var expr = new SExpressionBuilder("token").Build();

        expr.Token.Should().Be("token");
        expr.Values.Should().BeEmpty();
        expr.Children.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithStringValue_AddsStringValue()
    {
        var expr = new SExpressionBuilder("property")
            .AddValue("Reference")
            .Build();

        expr.Values.Should().HaveCount(1);
        expr.Values[0].Should().BeOfType<SExprString>().Which.Value.Should().Be("Reference");
    }

    [Fact]
    public void Build_WithNumericValue_AddsNumberValue()
    {
        var expr = new SExpressionBuilder("width")
            .AddValue(0.25)
            .Build();

        expr.Values.Should().HaveCount(1);
        expr.Values[0].Should().BeOfType<SExprNumber>().Which.Value.Should().Be(0.25);
    }

    [Fact]
    public void Build_WithSymbol_AddsSymbolValue()
    {
        var expr = new SExpressionBuilder("type")
            .AddSymbol("solid")
            .Build();

        expr.Values.Should().HaveCount(1);
        expr.Values[0].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("solid");
    }

    [Fact]
    public void Build_WithBoolTrue_AddsYesSymbol()
    {
        var expr = new SExpressionBuilder("in_bom")
            .AddBool(true)
            .Build();

        expr.Values[0].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("yes");
    }

    [Fact]
    public void Build_WithBoolFalse_AddsNoSymbol()
    {
        var expr = new SExpressionBuilder("in_bom")
            .AddBool(false)
            .Build();

        expr.Values[0].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("no");
    }

    [Fact]
    public void Build_WithPrebuiltChild_AddsChild()
    {
        var child = new SExpressionBuilder("child")
            .AddValue(42.0)
            .Build();

        var expr = new SExpressionBuilder("parent")
            .AddChild(child)
            .Build();

        expr.Children.Should().HaveCount(1);
        expr.Children[0].Token.Should().Be("child");
        expr.Children[0].GetDouble().Should().Be(42);
    }

    [Fact]
    public void Build_WithConfiguredChild_AddsConfiguredChild()
    {
        var expr = new SExpressionBuilder("parent")
            .AddChild("child", b => b.AddValue("hello"))
            .Build();

        expr.Children.Should().HaveCount(1);
        expr.Children[0].Token.Should().Be("child");
        expr.Children[0].GetString().Should().Be("hello");
    }

    [Fact]
    public void Build_FluentChaining_BuildsComplexExpression()
    {
        var expr = new SExpressionBuilder("symbol")
            .AddValue("R")
            .AddChild("pin_names", b => b
                .AddChild("offset", o => o.AddValue(0.0)))
            .AddChild("in_bom", b => b.AddBool(true))
            .AddChild("on_board", b => b.AddBool(true))
            .AddChild("property", b => b
                .AddValue("Reference")
                .AddValue("R")
                .AddChild("at", a => a
                    .AddValue(2.032)
                    .AddValue(0.0)
                    .AddValue(90.0)))
            .Build();

        expr.Token.Should().Be("symbol");
        expr.GetString(0).Should().Be("R");
        expr.Children.Should().HaveCount(4);

        var pinNames = expr.GetChild("pin_names");
        pinNames.Should().NotBeNull();
        pinNames!.GetChild("offset")!.GetDouble().Should().Be(0);

        var property = expr.GetChild("property");
        property.Should().NotBeNull();
        property!.GetString(0).Should().Be("Reference");

        var at = property.GetChild("at");
        at!.GetDouble(0).Should().Be(2.032);
        at.GetDouble(2).Should().Be(90);
    }

    [Fact]
    public void Build_MultipleValues_PreservesOrder()
    {
        var expr = new SExpressionBuilder("pad")
            .AddValue("1")
            .AddSymbol("smd")
            .AddSymbol("roundrect")
            .Build();

        expr.Values.Should().HaveCount(3);
        expr.Values[0].Should().BeOfType<SExprString>().Which.Value.Should().Be("1");
        expr.Values[1].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("smd");
        expr.Values[2].Should().BeOfType<SExprSymbol>().Which.Value.Should().Be("roundrect");
    }

    [Fact]
    public void Build_MultipleChildren_PreservesOrder()
    {
        var expr = new SExpressionBuilder("parent")
            .AddChild("first", _ => { })
            .AddChild("second", _ => { })
            .AddChild("third", _ => { })
            .Build();

        expr.Children.Should().HaveCount(3);
        expr.Children[0].Token.Should().Be("first");
        expr.Children[1].Token.Should().Be("second");
        expr.Children[2].Token.Should().Be("third");
    }
}
