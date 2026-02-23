namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// A value within an S-expression (string, number, or symbol/token).
/// </summary>
public interface ISExpressionValue { }

/// <summary>
/// A double-quoted string value in an S-expression.
/// </summary>
/// <param name="Value">The string content (without surrounding quotes).</param>
public sealed record SExprString(string Value) : ISExpressionValue
{
    /// <inheritdoc />
    public override string ToString() => $"\"{Value}\"";
}

/// <summary>
/// A numeric value in an S-expression.
/// </summary>
/// <param name="Value">The numeric value as a double.</param>
public sealed record SExprNumber(double Value) : ISExpressionValue
{
    /// <inheritdoc />
    public override string ToString() => FormatNumber(Value);

    /// <summary>
    /// Formats a number without trailing zeros and without exponential notation.
    /// </summary>
    internal static string FormatNumber(double value)
    {
        // Use G17 for full precision, then trim trailing zeros after decimal point
        var result = value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);

        // If it contains exponential notation, fall back to fixed-point
        if (result.Contains('E') || result.Contains('e'))
        {
            result = value.ToString("F10", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Trim trailing zeros after decimal point
        if (result.Contains('.'))
        {
            result = result.TrimEnd('0').TrimEnd('.');
        }

        // Handle negative zero
        if (result == "-0")
        {
            result = "0";
        }

        return result;
    }
}

/// <summary>
/// An unquoted symbol/token value in an S-expression (e.g., "yes", "no", layer names).
/// </summary>
/// <param name="Value">The symbol text.</param>
public sealed record SExprSymbol(string Value) : ISExpressionValue
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
