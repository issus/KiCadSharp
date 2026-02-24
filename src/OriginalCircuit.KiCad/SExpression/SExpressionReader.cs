using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace OriginalCircuit.KiCad.SExpression;

/// <summary>
/// High-performance S-expression parser for KiCad files.
/// Uses byte-based parsing with string interning, cached value objects,
/// pooled parse frames, and ArrayPool-based storage for minimal allocations.
/// </summary>
public static class SExpressionReader
{
    // ---------------------------------------------------------------
    // String intern pool — common KiCad tokens as UTF-8 byte arrays
    // ---------------------------------------------------------------

    private static readonly (byte[] Bytes, string Str)[][] _internBuckets = BuildInternPool();
    private const int InternBucketCount = 256; // power of 2 for fast modulo

    private static (byte[] Bytes, string Str)[][] BuildInternPool()
    {
        string[] tokens =
        [
            "xy", "at", "layer", "width", "uuid", "tstamp", "net", "segment", "via", "pad",
            "fp_line", "fp_text", "gr_text", "property", "fill", "pin", "symbol", "stroke",
            "effects", "font", "size", "pts", "start", "end", "mid", "center", "radius",
            "angle", "offset", "thickness", "clearance", "type", "color", "name", "number",
            "value", "footprint", "reference", "hide", "yes", "no", "none", "solid", "rect",
            "circle", "oval", "smd", "thru_hole", "locked", "version", "generator",
            "kicad_pcb", "kicad_sch", "kicad_symbol_lib", "in_bom", "on_board", "pin_names",
            "pin_numbers", "polyline", "rectangle", "arc", "text", "wire", "label",
            "global_label", "hierarchical_label", "junction", "no_connect", "bus_entry",
            "lib_symbols", "lib_id", "descr", "title", "paper", "title_block", "date",
            "rev", "company", "comment", "sheet", "sheet_instances", "symbol_instances",
            "path", "fp_arc", "fp_circle", "fp_rect", "fp_poly", "gr_line", "gr_arc",
            "gr_circle", "gr_rect", "gr_poly", "zone", "filled_polygon", "polygon",
            "roundrect_rratio", "solder_mask_margin", "solder_paste_margin",
            "solder_paste_ratio", "thermal_bridge_width", "thermal_bridge_angle",
            "layers", "drill", "tstamps", "teardrop", "chamfer_ratio", "chamfer",
            "model", "fp_text_private", "module", "general", "page", "setup",
            "pcbplotparams", "net_class", "dimension", "target", "drawings",
            "tracks", "zones", "groups", "images"
        ];

        var buckets = new (byte[] Bytes, string Str)[InternBucketCount][];
        var builders = new List<(byte[] Bytes, string Str)>[InternBucketCount];

        foreach (var token in tokens)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var bucket = (int)(ComputeHash(bytes, 0, bytes.Length) & (InternBucketCount - 1));
            builders[bucket] ??= [];
            builders[bucket].Add((bytes, token));
        }

        for (var i = 0; i < InternBucketCount; i++)
        {
            buckets[i] = builders[i]?.ToArray() ?? [];
        }

        return buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeHash(byte[] data, int start, int length)
    {
        // FNV-1a hash
        unchecked
        {
            var hash = 2166136261u;
            var end = start + length;
            for (var i = start; i < end; i++)
            {
                hash ^= data[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string InternToken(byte[] data, int start, int length)
    {
        var hash = ComputeHash(data, start, length);
        var candidates = _internBuckets[(int)(hash & (InternBucketCount - 1))];

        for (var i = 0; i < candidates.Length; i++)
        {
            ref var c = ref candidates[i];
            if (c.Bytes.Length == length && data.AsSpan(start, length).SequenceEqual(c.Bytes))
            {
                return c.Str;
            }
        }

        return Encoding.UTF8.GetString(data, start, length);
    }

    // ---------------------------------------------------------------
    // Cached value objects
    // ---------------------------------------------------------------

    private static readonly SExprSymbol _symYes = new("yes");
    private static readonly SExprSymbol _symNo = new("no");
    private static readonly SExprSymbol _symNone = new("none");
    private static readonly SExprSymbol _symSolid = new("solid");
    private static readonly SExprSymbol _symHide = new("hide");
    private static readonly SExprSymbol _symLocked = new("locked");
    private static readonly SExprSymbol _symSmd = new("smd");
    private static readonly SExprSymbol _symThruHole = new("thru_hole");

    // Use a fixed array for O(1) lookup of the 8 cached symbols by interned reference
    private static readonly (string Key, SExprSymbol Value)[] _cachedSymbolPairs =
    [
        ("yes", _symYes), ("no", _symNo), ("none", _symNone), ("solid", _symSolid),
        ("hide", _symHide), ("locked", _symLocked), ("smd", _symSmd), ("thru_hole", _symThruHole),
    ];

    // Cached SExprNumber for integers -1..360
    private static readonly SExprNumber[] _cachedNumbers = BuildCachedNumbers();

    private static SExprNumber[] BuildCachedNumbers()
    {
        var arr = new SExprNumber[362]; // Index 0 = -1, Index 361 = 360
        for (var i = 0; i < arr.Length; i++)
            arr[i] = new SExprNumber(i - 1);
        return arr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SExprNumber GetCachedNumber(double value)
    {
        var intVal = (int)value;
        if (value == intVal && (uint)(intVal + 1) <= 361)
            return _cachedNumbers[intVal + 1];
        return new SExprNumber(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SExprSymbol GetCachedSymbol(string value)
    {
        // Since tokens are interned, we can use reference equality for the common case
        for (var i = 0; i < _cachedSymbolPairs.Length; i++)
        {
            if (ReferenceEquals(_cachedSymbolPairs[i].Key, value))
                return _cachedSymbolPairs[i].Value;
        }
        return new SExprSymbol(value);
    }

    // ---------------------------------------------------------------
    // Power-of-10 table for fast double parsing
    // ---------------------------------------------------------------

    private static readonly double[] _pow10 =
    [
        1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9,
        1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19
    ];

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

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
        var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        return ParseBytes(bytes);
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
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        return ParseBytes(ms.ToArray());
    }

    /// <summary>
    /// Parses an S-expression from a text span.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed root S-expression node.</returns>
    /// <exception cref="FormatException">The text contains invalid S-expression syntax.</exception>
    public static SExpression Read(ReadOnlySpan<char> text)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(text, bytes);
        return ParseBytes(bytes);
    }

    // ---------------------------------------------------------------
    // Byte-based parser core
    // ---------------------------------------------------------------

    private static SExpression ParseBytes(byte[] data)
    {
        var pos = 0;

        // Skip UTF-8 BOM if present
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            pos = 3;

        SkipWhitespace(data, ref pos);

        if (pos >= data.Length || data[pos] != (byte)'(')
            throw new FormatException("Expected '(' at start of S-expression.");

        return ParseExpressionIterative(data, ref pos);
    }

    private static SExpression ParseExpressionIterative(byte[] data, ref int pos)
    {
        // Pre-allocate stack and frame pool to avoid per-node heap allocations.
        // A typical large KiCad file may have nesting depth ~20-30, but we size
        // generously. The frame pool is sized larger since frames are reused.
        var stack = new ParseFrame[128];
        var stackTop = -1;

        var pool = new ParseFramePool(512);

        // Consume opening '('
        pos++;
        SkipWhitespace(data, ref pos);

        var token = ReadToken(data, ref pos);
        if (token is null)
            throw new FormatException($"Expected token after '(' at position {pos}.");

        stack[++stackTop] = pool.Rent(token);

        while (stackTop >= 0)
        {
            SkipWhitespace(data, ref pos);

            if (pos >= data.Length)
            {
                // Return all frames to pool
                for (var i = stackTop; i >= 0; i--)
                    pool.Return(stack[i]);
                throw new FormatException("Unexpected end of input; expected ')'.");
            }

            var ch = data[pos];

            if (ch == (byte)')')
            {
                pos++;
                ref var completed = ref stack[stackTop--];
                var expr = completed.BuildExpression();
                pool.Return(completed);

                if (stackTop < 0)
                    return expr;

                stack[stackTop].AddChild(expr);
            }
            else if (ch == (byte)'(')
            {
                pos++;
                SkipWhitespace(data, ref pos);

                var childToken = ReadToken(data, ref pos);
                if (childToken is null)
                {
                    for (var i = stackTop; i >= 0; i--)
                        pool.Return(stack[i]);
                    throw new FormatException($"Expected token after '(' at position {pos}.");
                }

                if (++stackTop >= stack.Length)
                    Array.Resize(ref stack, stack.Length * 2);

                stack[stackTop] = pool.Rent(childToken);
            }
            else
            {
                var value = ReadValue(data, ref pos);
                stack[stackTop].AddValue(value);
            }
        }

        throw new FormatException("Unexpected end of S-expression parsing.");
    }

    // ---------------------------------------------------------------
    // Token reading (byte-based with interning)
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? ReadToken(byte[] data, ref int pos)
    {
        if (pos >= data.Length)
            return null;

        var start = pos;
        while (pos < data.Length)
        {
            var ch = data[pos];
            if (ch == (byte)'(' || ch == (byte)')' || ch == (byte)'"' || ch <= (byte)' ')
                break;
            pos++;
        }

        if (pos == start)
            return null;

        return InternToken(data, start, pos - start);
    }

    // ---------------------------------------------------------------
    // Value reading (byte-based)
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISExpressionValue ReadValue(byte[] data, ref int pos)
    {
        if (data[pos] == (byte)'"')
            return ReadString(data, ref pos);

        // Read unquoted token
        var start = pos;
        while (pos < data.Length)
        {
            var ch = data[pos];
            if (ch == (byte)'(' || ch == (byte)')' || ch == (byte)'"' || ch <= (byte)' ')
                break;
            pos++;
        }

        var length = pos - start;

        // Try fast double parse first
        if (TryParseFastDouble(data, start, length, out var number))
            return GetCachedNumber(number);

        // It's a symbol
        var symbolStr = InternToken(data, start, length);
        return GetCachedSymbol(symbolStr);
    }

    // ---------------------------------------------------------------
    // Fast double parser for KiCad decimal numbers
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseFastDouble(byte[] data, int start, int length, out double result)
    {
        result = 0;
        if (length == 0) return false;

        var i = start;
        var end = start + length;
        var negative = false;

        if (data[i] == (byte)'-')
        {
            negative = true;
            i++;
            if (i >= end) return false;
        }
        else if (data[i] == (byte)'+')
        {
            i++;
            if (i >= end) return false;
        }

        var firstByte = data[i];
        if (firstByte != (byte)'.' && (uint)(firstByte - (byte)'0') > 9)
            return false;

        // Integer part
        long intPart = 0;
        while (i < end)
        {
            var d = (uint)(data[i] - (byte)'0');
            if (d > 9) break;
            intPart = intPart * 10 + d;
            i++;
        }

        // Optional fractional part
        var fracDigits = 0;
        long fracPart = 0;
        if (i < end && data[i] == (byte)'.')
        {
            i++;
            while (i < end)
            {
                var d = (uint)(data[i] - (byte)'0');
                if (d > 9) break;
                fracPart = fracPart * 10 + (long)d;
                fracDigits++;
                i++;
            }
        }

        if (i != end) return false;

        if (fracDigits == 0)
        {
            result = negative ? -intPart : intPart;
        }
        else if (fracDigits < _pow10.Length)
        {
            result = intPart + fracPart / _pow10[fracDigits];
            if (negative) result = -result;
        }
        else
        {
            return false;
        }

        return true;
    }

    // ---------------------------------------------------------------
    // String reading (byte-based, fast-path for no escapes)
    // ---------------------------------------------------------------

    private static SExprString ReadString(byte[] data, ref int pos)
    {
        pos++; // consume opening quote
        var start = pos;

        // Fast path: scan for closing quote without escapes
        while (pos < data.Length)
        {
            var ch = data[pos];
            if (ch == (byte)'\\')
                return ReadStringSlowPath(data, ref pos, start);
            if (ch == (byte)'"')
            {
                var str = Encoding.UTF8.GetString(data, start, pos - start);
                pos++; // consume closing quote
                return new SExprString(str);
            }
            pos++;
        }

        throw new FormatException("Unterminated string literal.");
    }

    private static SExprString ReadStringSlowPath(byte[] data, ref int pos, int contentStart)
    {
        var sb = new StringBuilder(64);
        if (pos > contentStart)
            sb.Append(Encoding.UTF8.GetString(data, contentStart, pos - contentStart));

        while (pos < data.Length)
        {
            var ch = data[pos];

            if (ch == (byte)'\\')
            {
                pos++;
                if (pos >= data.Length)
                    throw new FormatException("Unexpected end of input in escape sequence.");

                var escaped = data[pos];
                sb.Append(escaped switch
                {
                    (byte)'n' => '\n',
                    (byte)'r' => '\r',
                    (byte)'t' => '\t',
                    (byte)'\\' => '\\',
                    (byte)'"' => '"',
                    _ => (char)escaped
                });
                pos++;
            }
            else if (ch == (byte)'"')
            {
                pos++;
                return new SExprString(sb.ToString());
            }
            else
            {
                if (ch < 0x80)
                {
                    sb.Append((char)ch);
                    pos++;
                }
                else
                {
                    var charStart = pos;
                    pos++;
                    while (pos < data.Length && (data[pos] & 0xC0) == 0x80)
                        pos++;
                    sb.Append(Encoding.UTF8.GetString(data, charStart, pos - charStart));
                }
            }
        }

        throw new FormatException("Unterminated string literal.");
    }

    // ---------------------------------------------------------------
    // Whitespace skipping
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SkipWhitespace(byte[] data, ref int pos)
    {
        while (pos < data.Length && data[pos] <= (byte)' ')
            pos++;
    }

    // ---------------------------------------------------------------
    // Parse frame — struct-based with pooled arrays, no heap per node
    // ---------------------------------------------------------------

    private struct ParseFrame
    {
        public string Token;

        private ISExpressionValue[] _values;
        private int _valueCount;

        private SExpression[] _children;
        private int _childCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(string token, ISExpressionValue[] values, SExpression[] children)
        {
            Token = token;
            _values = values;
            _valueCount = 0;
            _children = children;
            _childCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValue(ISExpressionValue value)
        {
            if (_valueCount == _values.Length)
                GrowValues();
            _values[_valueCount++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddChild(SExpression child)
        {
            if (_childCount == _children.Length)
                GrowChildren();
            _children[_childCount++] = child;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowValues()
        {
            var newArr = ArrayPool<ISExpressionValue>.Shared.Rent(_values.Length * 2);
            Array.Copy(_values, newArr, _valueCount);
            ArrayPool<ISExpressionValue>.Shared.Return(_values, clearArray: false);
            _values = newArr;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowChildren()
        {
            var newArr = ArrayPool<SExpression>.Shared.Rent(_children.Length * 2);
            Array.Copy(_children, newArr, _childCount);
            ArrayPool<SExpression>.Shared.Return(_children, clearArray: false);
            _children = newArr;
        }

        public SExpression BuildExpression()
        {
            ISExpressionValue[] values;
            SExpression[] children;

            if (_valueCount == 0)
                values = [];
            else if (_valueCount == 1)
                values = [_values[0]];
            else
            {
                values = new ISExpressionValue[_valueCount];
                Array.Copy(_values, values, _valueCount);
            }

            if (_childCount == 0)
                children = [];
            else if (_childCount == 1)
                children = [_children[0]];
            else
            {
                children = new SExpression[_childCount];
                Array.Copy(_children, children, _childCount);
            }

            return new SExpression(Token, values, children);
        }

        /// <summary>
        /// Gets the rented arrays so the pool can reclaim them.
        /// </summary>
        public readonly (ISExpressionValue[] Values, SExpression[] Children) GetArrays()
            => (_values, _children);
    }

    // ---------------------------------------------------------------
    // ParseFrame pool — avoids renting from ArrayPool per node
    // ---------------------------------------------------------------

    private sealed class ParseFramePool
    {
        private readonly (ISExpressionValue[] Values, SExpression[] Children)[] _pool;
        private int _count;
        private const int InitialArraySize = 4;

        public ParseFramePool(int capacity)
        {
            _pool = new (ISExpressionValue[], SExpression[])[capacity];
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseFrame Rent(string token)
        {
            ISExpressionValue[] values;
            SExpression[] children;

            if (_count > 0)
            {
                _count--;
                (values, children) = _pool[_count];
            }
            else
            {
                values = ArrayPool<ISExpressionValue>.Shared.Rent(InitialArraySize);
                children = ArrayPool<SExpression>.Shared.Rent(InitialArraySize);
            }

            var frame = new ParseFrame();
            frame.Init(token, values, children);
            return frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(ParseFrame frame)
        {
            var (values, children) = frame.GetArrays();
            if (_count < _pool.Length)
            {
                _pool[_count++] = (values, children);
            }
            else
            {
                ArrayPool<ISExpressionValue>.Shared.Return(values, clearArray: false);
                ArrayPool<SExpression>.Shared.Return(children, clearArray: false);
            }
        }
    }
}
