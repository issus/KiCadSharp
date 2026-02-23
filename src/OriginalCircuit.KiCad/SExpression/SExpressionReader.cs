using System.Globalization;
using System.Text;

namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// High-performance S-expression parser for KiCad files.
/// Uses a stack-based iterative approach to handle deeply nested files
/// without risk of stack overflow.
/// </summary>
public static class SExpressionReader
{
    /// <summary>
    /// Reads and parses an S-expression from a file path.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed root S-expression node.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="FormatException">The file contains invalid S-expression syntax.</exception>
    public static async ValueTask<SExpression> ReadAsync(string path, CancellationToken ct = default)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadAsync(stream, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads and parses an S-expression from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed root S-expression node.</returns>
    /// <exception cref="FormatException">The stream contains invalid S-expression syntax.</exception>
    public static async ValueTask<SExpression> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 65536, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return Read(text.AsSpan());
    }

    /// <summary>
    /// Parses an S-expression from a text span.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed root S-expression node.</returns>
    /// <exception cref="FormatException">The text contains invalid S-expression syntax.</exception>
    public static SExpression Read(ReadOnlySpan<char> text)
    {
        var pos = 0;
        SkipWhitespace(text, ref pos);

        if (pos >= text.Length || text[pos] != '(')
        {
            throw new FormatException("Expected '(' at start of S-expression.");
        }

        var result = ParseExpression(text, ref pos);

        SkipWhitespace(text, ref pos);
        if (pos < text.Length)
        {
            throw new FormatException($"Unexpected content after root S-expression at position {pos}.");
        }

        return result;
    }

    private static SExpression ParseExpression(ReadOnlySpan<char> text, ref int pos)
    {
        // Stack-based iterative parser to avoid stack overflow on deeply nested files.
        // Each frame represents an in-progress S-expression being parsed.
        var frameStack = new Stack<ParseFrame>(64);

        // Consume opening '('
        pos++;
        SkipWhitespace(text, ref pos);

        var token = ReadToken(text, ref pos);
        if (token is null)
        {
            throw new FormatException($"Expected token after '(' at position {pos}.");
        }

        frameStack.Push(new ParseFrame(token));

        while (frameStack.Count > 0)
        {
            SkipWhitespace(text, ref pos);

            if (pos >= text.Length)
            {
                throw new FormatException("Unexpected end of input; expected ')'.");
            }

            var ch = text[pos];

            if (ch == ')')
            {
                // Close current expression
                pos++;
                var completed = frameStack.Pop();
                var expr = new SExpression(completed.Token, completed.Values, completed.Children);

                if (frameStack.Count == 0)
                {
                    return expr;
                }

                // Add completed expression as a child of the parent
                frameStack.Peek().Children.Add(expr);
            }
            else if (ch == '(')
            {
                // Start a new child expression
                pos++;
                SkipWhitespace(text, ref pos);

                var childToken = ReadToken(text, ref pos);
                if (childToken is null)
                {
                    throw new FormatException($"Expected token after '(' at position {pos}.");
                }

                frameStack.Push(new ParseFrame(childToken));
            }
            else
            {
                // Read a value
                var value = ReadValue(text, ref pos);
                frameStack.Peek().Values.Add(value);
            }
        }

        throw new FormatException("Unexpected end of S-expression parsing.");
    }

    private static string? ReadToken(ReadOnlySpan<char> text, ref int pos)
    {
        if (pos >= text.Length)
            return null;

        var start = pos;
        while (pos < text.Length)
        {
            var ch = text[pos];
            if (ch == '(' || ch == ')' || ch == '"' || char.IsWhiteSpace(ch))
                break;
            pos++;
        }

        if (pos == start)
            return null;

        return text[start..pos].ToString();
    }

    private static ISExpressionValue ReadValue(ReadOnlySpan<char> text, ref int pos)
    {
        if (text[pos] == '"')
        {
            return ReadString(text, ref pos);
        }

        // Read unquoted token
        var start = pos;
        while (pos < text.Length)
        {
            var ch = text[pos];
            if (ch == '(' || ch == ')' || ch == '"' || char.IsWhiteSpace(ch))
                break;
            pos++;
        }

        var tokenSpan = text[start..pos];

        // Try to parse as a number
        if (double.TryParse(tokenSpan, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
        {
            return new SExprNumber(number);
        }

        return new SExprSymbol(tokenSpan.ToString());
    }

    private static SExprString ReadString(ReadOnlySpan<char> text, ref int pos)
    {
        // Consume opening quote
        pos++;
        var sb = new StringBuilder(64);

        while (pos < text.Length)
        {
            var ch = text[pos];

            if (ch == '\\')
            {
                pos++;
                if (pos >= text.Length)
                {
                    throw new FormatException("Unexpected end of input in escape sequence.");
                }

                var escaped = text[pos];
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped
                });
                pos++;
            }
            else if (ch == '"')
            {
                pos++;
                return new SExprString(sb.ToString());
            }
            else
            {
                sb.Append(ch);
                pos++;
            }
        }

        throw new FormatException("Unterminated string literal.");
    }

    private static void SkipWhitespace(ReadOnlySpan<char> text, ref int pos)
    {
        while (pos < text.Length)
        {
            if (char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Represents a frame in the stack-based parser, tracking the in-progress S-expression.
    /// </summary>
    private sealed class ParseFrame
    {
        public string Token { get; }
        public List<ISExpressionValue> Values { get; } = [];
        public List<SExpression> Children { get; } = [];

        public ParseFrame(string token)
        {
            Token = token;
        }
    }
}
