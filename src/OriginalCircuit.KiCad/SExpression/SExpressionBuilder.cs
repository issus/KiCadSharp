namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// Fluent builder for constructing S-expression trees programmatically.
/// </summary>
public sealed class SExpressionBuilder
{
    private readonly string _token;
    private readonly List<ISExpressionValue> _values = [];
    private readonly List<SExpression> _children = [];

    /// <summary>
    /// Initializes a new builder with the specified token name.
    /// </summary>
    /// <param name="token">The token name for the S-expression node.</param>
    public SExpressionBuilder(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        _token = token;
    }

    /// <summary>
    /// Adds a quoted string value to this S-expression.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _values.Add(new SExprString(value));
        return this;
    }

    /// <summary>
    /// Adds a numeric value to this S-expression.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddValue(double value)
    {
        _values.Add(new SExprNumber(value));
        return this;
    }

    /// <summary>
    /// Adds a numeric value with explicit text formatting for round-trip fidelity.
    /// The value is stored as a double for computation, but serialized with the given text.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <param name="formattedText">The text representation to use when serializing.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddFormattedValue(double value, string formattedText)
    {
        _values.Add(new SExprNumber(value) { OriginalText = formattedText });
        return this;
    }

    /// <summary>
    /// Adds an unquoted symbol value to this S-expression.
    /// </summary>
    /// <param name="symbol">The symbol text.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddSymbol(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        _values.Add(new SExprSymbol(symbol));
        return this;
    }

    /// <summary>
    /// Adds a boolean value as a "yes"/"no" symbol.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddBool(bool value)
    {
        _values.Add(new SExprSymbol(value ? "yes" : "no"));
        return this;
    }

    /// <summary>
    /// Adds a pre-built S-expression as a child of this node.
    /// </summary>
    /// <param name="child">The child S-expression to add.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddChild(SExpression child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        return this;
    }

    /// <summary>
    /// Adds a new child S-expression, configured via a delegate.
    /// </summary>
    /// <param name="token">The token name for the child node.</param>
    /// <param name="configure">A delegate to configure the child builder.</param>
    /// <returns>This builder for method chaining.</returns>
    public SExpressionBuilder AddChild(string token, Action<SExpressionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(configure);
        var childBuilder = new SExpressionBuilder(token);
        configure(childBuilder);
        _children.Add(childBuilder.Build());
        return this;
    }

    /// <summary>
    /// Builds the final <see cref="SExpression"/> from the accumulated values and children.
    /// </summary>
    /// <returns>The constructed S-expression node.</returns>
    public SExpression Build()
    {
        return new SExpression(_token, _values.ToArray(), _children.ToArray());
    }
}
