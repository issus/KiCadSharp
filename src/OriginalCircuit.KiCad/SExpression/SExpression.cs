namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// Represents an S-expression node (a parenthesized list with a token name and children).
/// For example, <c>(width 0.25)</c> is a node with token "width" and a single numeric value.
/// </summary>
public sealed class SExpression
{
    private readonly ISExpressionValue[] _values;
    private readonly SExpression[] _children;

    /// <summary>
    /// Initializes a new instance of the <see cref="SExpression"/> class.
    /// </summary>
    /// <param name="token">The token name of this S-expression node.</param>
    /// <param name="values">The inline values following the token.</param>
    /// <param name="children">The nested S-expression children.</param>
    public SExpression(string token, List<ISExpressionValue> values, List<SExpression> children)
    {
        Token = token;
        _values = [.. values];
        _children = [.. children];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SExpression"/> class from pre-built arrays.
    /// </summary>
    /// <param name="token">The token name of this S-expression node.</param>
    /// <param name="values">The inline values array.</param>
    /// <param name="children">The nested children array.</param>
    internal SExpression(string token, ISExpressionValue[] values, SExpression[] children)
    {
        Token = token;
        _values = values;
        _children = children;
    }

    /// <summary>
    /// Gets the token name of this S-expression node (e.g., "width", "at", "symbol").
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Gets the inline values following the token (strings, numbers, and symbols).
    /// </summary>
    public IReadOnlyList<ISExpressionValue> Values => _values;

    /// <summary>
    /// Gets the nested child S-expression nodes.
    /// </summary>
    public IReadOnlyList<SExpression> Children => _children;

    /// <summary>
    /// Returns the first child node with the specified token name, or <c>null</c> if not found.
    /// </summary>
    /// <param name="token">The token name to search for.</param>
    /// <returns>The first matching child, or <c>null</c>.</returns>
    public SExpression? GetChild(string token)
    {
        for (var i = 0; i < _children.Length; i++)
        {
            if (string.Equals(_children[i].Token, token, StringComparison.Ordinal))
            {
                return _children[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all child nodes with the specified token name.
    /// </summary>
    /// <param name="token">The token name to search for.</param>
    /// <returns>An enumerable of matching children.</returns>
    public IEnumerable<SExpression> GetChildren(string token)
    {
        for (var i = 0; i < _children.Length; i++)
        {
            if (string.Equals(_children[i].Token, token, StringComparison.Ordinal))
            {
                yield return _children[i];
            }
        }
    }

    /// <summary>
    /// Gets the string content of the value at the specified index.
    /// Returns the string for <see cref="SExprString"/> or the symbol text for <see cref="SExprSymbol"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the value.</param>
    /// <returns>The string value, or <c>null</c> if the index is out of range or the value is not a string/symbol.</returns>
    public string? GetString(int index = 0)
    {
        if ((uint)index >= (uint)_values.Length)
            return null;

        return _values[index] switch
        {
            SExprString s => s.Value,
            SExprSymbol s => s.Value,
            _ => null
        };
    }

    /// <summary>
    /// Gets the numeric value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the value.</param>
    /// <returns>The numeric value, or <c>null</c> if the index is out of range or the value is not a number.</returns>
    public double? GetDouble(int index = 0)
    {
        if ((uint)index >= (uint)_values.Length)
            return null;

        return _values[index] is SExprNumber n ? n.Value : null;
    }

    /// <summary>
    /// Gets the integer value at the specified index (truncated from double).
    /// </summary>
    /// <param name="index">The zero-based index of the value.</param>
    /// <returns>The integer value, or <c>null</c> if the index is out of range or the value is not a number.</returns>
    public int? GetInt(int index = 0)
    {
        var d = GetDouble(index);
        return d.HasValue ? (int)d.Value : null;
    }

    /// <summary>
    /// Gets the boolean value at the specified index, interpreting "yes" as <c>true</c> and "no" as <c>false</c>.
    /// </summary>
    /// <param name="index">The zero-based index of the value.</param>
    /// <returns>The boolean value, or <c>null</c> if the index is out of range or the value is not a yes/no symbol.</returns>
    public bool? GetBool(int index = 0)
    {
        if ((uint)index >= (uint)_values.Length)
            return null;

        if (_values[index] is SExprSymbol s)
        {
            return s.Value switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// Parses a coordinate pair from values starting at the specified index.
    /// Typically used with <c>(xy X Y)</c> or <c>(at X Y)</c> patterns.
    /// </summary>
    /// <param name="index">The zero-based starting index for the X value.</param>
    /// <returns>A tuple of (X, Y) coordinates, or <c>null</c> if insufficient numeric values exist.</returns>
    public (double X, double Y)? GetXY(int index = 0)
    {
        var x = GetDouble(index);
        var y = GetDouble(index + 1);

        if (x.HasValue && y.HasValue)
            return (x.Value, y.Value);

        return null;
    }

    /// <inheritdoc />
    public override string ToString() => $"({Token} ...)";
}
