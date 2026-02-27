using System.Text;

namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// Writes S-expression trees back to text with proper formatting.
/// Produces human-readable output matching KiCad conventions.
/// Uses an iterative approach to avoid stack overflow on deeply nested trees.
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
        // Note: entire tree is materialized as a string before streaming.
        // For very large files (50MB+), consider implementing a node-by-node streaming writer.
        var text = Write(expr);
        var writer = new StreamWriter(stream, Encoding.UTF8, 65536, leaveOpen: true);
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteAsync(text.AsMemory(), ct).ConfigureAwait(false);
            await writer.WriteAsync("\n".AsMemory(), ct).ConfigureAwait(false);
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
        WriteNodeIterative(sb, expr);
        return sb.ToString();
    }

    /// <summary>
    /// Default indentation width in spaces per nesting level.
    /// </summary>
    private const int DefaultIndentWidth = 2;

    private struct WriteFrame
    {
        public SExpression Node;
        public int Indent;
        public bool IsCompact;
        public int ChildIndex;
    }

    private static void WriteNodeIterative(StringBuilder sb, SExpression root)
    {
        // Use OriginalIndent from root if available, otherwise default
        var indentWidth = root.OriginalIndent ?? DefaultIndentWidth;

        var stack = new List<WriteFrame>(32);
        stack.Add(new WriteFrame { Node = root, Indent = 0, IsCompact = false, ChildIndex = 0 });

        while (stack.Count > 0)
        {
            var idx = stack.Count - 1;
            var frame = stack[idx];
            var node = frame.Node;

            if (frame.ChildIndex == 0)
            {
                // First visit: write opening token and values
                sb.Append('(');
                sb.Append(node.Token);

                foreach (var value in node.Values)
                {
                    sb.Append(' ');
                    WriteValue(sb, value);
                }

                if (node.Children.Count == 0)
                {
                    sb.Append(')');
                    stack.RemoveAt(idx);
                    continue;
                }

                // Determine compactness: use WasCompact from source if available, else heuristic
                frame.IsCompact = node.WasCompact ?? ShouldUseCompact(node);
                stack[idx] = frame;
            }

            if (frame.ChildIndex < node.Children.Count)
            {
                var child = node.Children[frame.ChildIndex];

                // Write prefix before child
                if (frame.IsCompact)
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append('\n');
                    sb.Append(' ', (frame.Indent + 1) * indentWidth);
                }

                // Advance child index
                frame.ChildIndex++;
                stack[idx] = frame;

                // Push child frame
                stack.Add(new WriteFrame
                {
                    Node = child,
                    Indent = frame.Indent + 1,
                    IsCompact = false,
                    ChildIndex = 0
                });
            }
            else
            {
                // All children processed, write closing
                if (!frame.IsCompact)
                {
                    sb.Append('\n');
                    sb.Append(' ', frame.Indent * indentWidth);
                }
                sb.Append(')');
                stack.RemoveAt(idx);
            }
        }
    }

    private static bool ShouldUseCompact(SExpression node)
    {
        if (node.Children.Count > 2 || node.Values.Count > 0)
            return false;

        foreach (var child in node.Children)
        {
            if (child.Children.Count > 0)
                return false;
        }

        var totalLen = node.Token.Length;
        foreach (var child in node.Children)
        {
            totalLen += 2 + child.Token.Length;
            foreach (var v in child.Values)
            {
                totalLen += 1 + v switch
                {
                    SExprString s => s.Value.Length + 2, // +2 for quotes
                    SExprNumber n => n.ToSExpression().Length,
                    SExprSymbol s => s.Value.Length,
                    _ => 0
                };
            }
        }
        return totalLen < 80;
    }

    private static void WriteValue(StringBuilder sb, ISExpressionValue value)
    {
        switch (value)
        {
            case SExprString s:
                WriteQuotedString(sb, s.Value);
                break;
            case SExprNumber n:
                sb.Append(n.ToSExpression());
                break;
            case SExprSymbol s:
                sb.Append(s.Value);
                break;
        }
    }

    private static void WriteQuotedString(StringBuilder sb, string value)
    {
        if (!NeedsEscaping(value))
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

    private static bool NeedsEscaping(string value)
    {
        foreach (var ch in value)
        {
            if (ch == '"' || ch == '\\' || ch == '\n' || ch == '\r' || ch == '\t')
                return true;
        }
        return false;
    }
}
