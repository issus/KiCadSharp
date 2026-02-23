using System.Text;

namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// Writes S-expression trees back to text with proper formatting.
/// Produces human-readable output matching KiCad conventions.
/// </summary>
public static class SExpressionWriter
{
    /// <summary>
    /// Writes an S-expression tree to a file.
    /// </summary>
    /// <param name="expr">The root S-expression to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(SExpression expr, string path, CancellationToken ct = default)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous);
        await WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an S-expression tree to a stream.
    /// </summary>
    /// <param name="expr">The root S-expression to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(SExpression expr, Stream stream, CancellationToken ct = default)
    {
        var text = Write(expr);
        var writer = new StreamWriter(stream, Encoding.UTF8, 65536, leaveOpen: true);
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteAsync(text.AsMemory(), ct).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes an S-expression tree to a string with proper formatting.
    /// </summary>
    /// <param name="expr">The root S-expression to write.</param>
    /// <returns>The formatted S-expression string.</returns>
    public static string Write(SExpression expr)
    {
        var sb = new StringBuilder(4096);
        WriteNode(sb, expr, 0);
        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, SExpression expr, int indent)
    {
        sb.Append('(');
        sb.Append(expr.Token);

        // Write inline values
        foreach (var value in expr.Values)
        {
            sb.Append(' ');
            WriteValue(sb, value);
        }

        if (expr.Children.Count == 0)
        {
            sb.Append(')');
            return;
        }

        // If the node has only simple children (no grandchildren), keep them compact
        var allChildrenSimple = true;
        foreach (var child in expr.Children)
        {
            if (child.Children.Count > 0)
            {
                allChildrenSimple = false;
                break;
            }
        }

        if (allChildrenSimple && expr.Children.Count <= 2 && expr.Values.Count == 0)
        {
            // Keep short simple children inline
            var totalLen = expr.Token.Length;
            foreach (var child in expr.Children)
            {
                totalLen += 2 + child.Token.Length;
                foreach (var v in child.Values)
                {
                    totalLen += 1 + v.ToString()!.Length;
                }
            }

            if (totalLen < 80)
            {
                foreach (var child in expr.Children)
                {
                    sb.Append(' ');
                    WriteNode(sb, child, 0);
                }
                sb.Append(')');
                return;
            }
        }

        // Write children on separate lines
        var childIndent = indent + 1;
        foreach (var child in expr.Children)
        {
            sb.AppendLine();
            sb.Append(' ', childIndent * 2);
            WriteNode(sb, child, childIndent);
        }

        sb.AppendLine();
        sb.Append(' ', indent * 2);
        sb.Append(')');
    }

    private static void WriteValue(StringBuilder sb, ISExpressionValue value)
    {
        switch (value)
        {
            case SExprString s:
                WriteQuotedString(sb, s.Value);
                break;
            case SExprNumber n:
                sb.Append(SExprNumber.FormatNumber(n.Value));
                break;
            case SExprSymbol s:
                sb.Append(s.Value);
                break;
        }
    }

    private static void WriteQuotedString(StringBuilder sb, string value)
    {
        if (!NeedsQuoting(value))
        {
            sb.Append('"');
            sb.Append(value);
            sb.Append('"');
            return;
        }

        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        sb.Append('"');
    }

    private static bool NeedsQuoting(string value)
    {
        foreach (var ch in value)
        {
            if (ch == '"' || ch == '\\' || ch == '\n' || ch == '\r' || ch == '\t')
            {
                return true;
            }
        }
        return false;
    }
}
