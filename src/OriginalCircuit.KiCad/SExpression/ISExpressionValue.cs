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
    /// <summary>
    /// The original text representation from the parsed file, if available.
    /// When set, this is used for serialization instead of <see cref="FormatNumber"/>
    /// to preserve exact formatting (trailing zeros, scientific notation, etc.).
    /// </summary>
    public string? OriginalText { get; init; }

    /// <inheritdoc />
    public override string ToString() => OriginalText ?? FormatNumber(Value);

    /// <summary>
    /// Returns the S-expression representation of this number.
    /// Uses <see cref="OriginalText"/> if available, otherwise formats the value.
    /// </summary>
    public string ToSExpression() => OriginalText ?? FormatNumber(Value);

    /// <summary>
    /// Formats a number without trailing zeros and without exponential notation.
    /// </summary>
    internal static string FormatNumber(double value)
    {
        // Use R (round-trip) format which produces the shortest representation
        // that round-trips through double.Parse. This avoids G17's tendency to
        // produce very long representations while still preserving precision.
        var result = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

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
