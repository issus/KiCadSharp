using FluentAssertions;
using OriginalCircuit.KiCad.Serialization;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.KiCad.Tests.Serialization;

/// <summary>
/// Verifies round-trip fidelity by reading real KiCad files, clearing SourceTree
/// (forcing model-based rebuild), writing back, and comparing against the original
/// S-expression tree. This tests whether all properties survive the model path.
/// </summary>
public class ModelRoundTripVerification
{
    private readonly ITestOutputHelper _output;

    public ModelRoundTripVerification(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// Round-trips a symbol library through the model path and compares S-expression output.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSymLibFiles))]
    public async Task SymLib_ModelRoundTrip_PreservesAllTokens(string path)
    {
        // Read original file as S-expression text
        var originalText = NormalizeText(await File.ReadAllTextAsync(path));

        // Read via model
        var lib = await SymLibReader.ReadAsync(path);

        // Write back
        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib, ms);
        ms.Position = 0;
        var roundTripText = NormalizeText(new StreamReader(ms).ReadToEnd());

        // Parse both into S-expression trees and compare structurally
        var originalTree = SExpressionReader.Read(originalText);
        var roundTripTree = SExpressionReader.Read(roundTripText);

        var diffs = CompareTreesStructural(originalTree, roundTripTree, "");
        if (diffs.Count > 0)
        {
            foreach (var diff in diffs.Take(50))
                _output.WriteLine(diff);
            _output.WriteLine($"Total differences: {diffs.Count}");
        }
        diffs.Should().BeEmpty($"File {Path.GetFileName(path)} should round-trip without data loss");
    }

    /// <summary>
    /// Round-trips a schematic through the model path.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSchFiles))]
    public async Task Sch_ModelRoundTrip_PreservesAllTokens(string path)
    {
        var originalText = NormalizeText(await File.ReadAllTextAsync(path));
        var sch = await SchReader.ReadAsync(path);

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;
        var roundTripText = NormalizeText(new StreamReader(ms).ReadToEnd());

        var originalTree = SExpressionReader.Read(originalText);
        var roundTripTree = SExpressionReader.Read(roundTripText);

        var diffs = CompareTreesStructural(originalTree, roundTripTree, "");
        if (diffs.Count > 0)
        {
            foreach (var diff in diffs.Take(50))
                _output.WriteLine(diff);
            _output.WriteLine($"Total differences: {diffs.Count}");
        }
        diffs.Should().BeEmpty($"File {Path.GetFileName(path)} should round-trip without data loss");
    }

    /// <summary>
    /// Round-trips a footprint through the model path.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetFootprintFiles))]
    public async Task Footprint_ModelRoundTrip_PreservesAllTokens(string path)
    {
        var originalText = NormalizeText(await File.ReadAllTextAsync(path));

        // Skip KiCad 5 legacy files (module token) — library targets KiCad 6.0+
        if (originalText.TrimStart().StartsWith("(module"))
            return;

        var fp = await FootprintReader.ReadAsync(path);

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;
        var roundTripText = NormalizeText(new StreamReader(ms).ReadToEnd());

        var originalTree = SExpressionReader.Read(originalText);
        var roundTripTree = SExpressionReader.Read(roundTripText);

        var diffs = CompareTreesStructural(originalTree, roundTripTree, "");
        if (diffs.Count > 0)
        {
            foreach (var diff in diffs.Take(50))
                _output.WriteLine(diff);
            _output.WriteLine($"Total differences: {diffs.Count}");
        }
        diffs.Should().BeEmpty($"File {Path.GetFileName(path)} should round-trip without data loss");
    }

    /// <summary>
    /// Round-trips a PCB through the model path.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetPcbFiles))]
    public async Task Pcb_ModelRoundTrip_PreservesAllTokens(string path)
    {
        var originalText = NormalizeText(await File.ReadAllTextAsync(path));
        var pcb = await PcbReader.ReadAsync(path);

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;
        var roundTripText = NormalizeText(new StreamReader(ms).ReadToEnd());

        var originalTree = SExpressionReader.Read(originalText);
        var roundTripTree = SExpressionReader.Read(roundTripText);

        var diffs = CompareTreesStructural(originalTree, roundTripTree, "");
        if (diffs.Count > 0)
        {
            foreach (var diff in diffs.Take(50))
                _output.WriteLine(diff);
            _output.WriteLine($"Total differences: {diffs.Count}");
        }
        diffs.Should().BeEmpty($"File {Path.GetFileName(path)} should round-trip without data loss");
    }

    // -- Data providers --

    public static IEnumerable<object[]> GetSymLibFiles() =>
        GetTestFiles("*.kicad_sym").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetSchFiles() =>
        GetTestFiles("*.kicad_sch").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetFootprintFiles() =>
        GetTestFiles("*.kicad_mod").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetPcbFiles() =>
        GetTestFiles("*.kicad_pcb").Select(f => new object[] { f });

    private static IEnumerable<string> GetTestFiles(string pattern)
    {
        var dir = Path.Combine("TestData", "RealWorld");
        if (!Directory.Exists(dir))
            return [];
        return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
    }

    // -- Structural comparison --

    private static string NormalizeText(string text)
    {
        // Strip BOM and normalize line endings
        text = text.Replace("\r\n", "\n").TrimStart('\uFEFF');
        return text;
    }

    /// <summary>
    /// Compares two S-expression trees structurally, reporting differences in tokens,
    /// values, and children. Ignores formatting differences (whitespace, number format).
    /// </summary>
    private static List<string> CompareTreesStructural(SExpr original, SExpr roundTrip, string path)
    {
        var diffs = new List<string>();
        var currentPath = string.IsNullOrEmpty(path) ? original.Token : $"{path}/{original.Token}";

        // Compare token
        if (original.Token != roundTrip.Token)
        {
            diffs.Add($"TOKEN MISMATCH at {currentPath}: '{original.Token}' vs '{roundTrip.Token}'");
            return diffs; // Don't compare children if tokens differ
        }

        // Compare values (structurally, not textually)
        var origValues = original.Values;
        var rtValues = roundTrip.Values;

        if (origValues.Count != rtValues.Count)
        {
            diffs.Add($"VALUE COUNT at {currentPath}: {origValues.Count} vs {rtValues.Count} " +
                       $"(orig: [{string.Join(", ", origValues.Select(v => v.ToString()))}] " +
                       $"vs rt: [{string.Join(", ", rtValues.Select(v => v.ToString()))}])");
        }
        else
        {
            for (int i = 0; i < origValues.Count; i++)
            {
                if (!ValuesEqual(origValues[i], rtValues[i]))
                {
                    diffs.Add($"VALUE at {currentPath}[{i}]: '{origValues[i].ToString()}' vs '{rtValues[i].ToString()}'");
                }
            }
        }

        // Compare children by building token-indexed groups
        var origChildren = original.Children;
        var rtChildren = roundTrip.Children;

        if (origChildren.Count != rtChildren.Count)
        {
            var origTokens = origChildren.Select(c => c.Token).GroupBy(t => t).Select(g => $"{g.Key}({g.Count()})");
            var rtTokens = rtChildren.Select(c => c.Token).GroupBy(t => t).Select(g => $"{g.Key}({g.Count()})");
            diffs.Add($"CHILD COUNT at {currentPath}: {origChildren.Count} vs {rtChildren.Count}");

            // Find missing and extra tokens
            var origTokenCounts = origChildren.GroupBy(c => c.Token).ToDictionary(g => g.Key, g => g.Count());
            var rtTokenCounts = rtChildren.GroupBy(c => c.Token).ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in origTokenCounts)
            {
                if (!rtTokenCounts.TryGetValue(kvp.Key, out var rtCount))
                    diffs.Add($"  MISSING child '{kvp.Key}' (x{kvp.Value}) at {currentPath}");
                else if (rtCount != kvp.Value)
                    diffs.Add($"  COUNT MISMATCH child '{kvp.Key}': {kvp.Value} vs {rtCount} at {currentPath}");
            }
            foreach (var kvp in rtTokenCounts)
            {
                if (!origTokenCounts.ContainsKey(kvp.Key))
                    diffs.Add($"  EXTRA child '{kvp.Key}' (x{kvp.Value}) at {currentPath}");
            }
        }
        else
        {
            // Compare children pairwise (same order)
            for (int i = 0; i < origChildren.Count; i++)
            {
                diffs.AddRange(CompareTreesStructural(origChildren[i], rtChildren[i], currentPath));
            }
        }

        return diffs;
    }

    private static bool ValuesEqual(ISExpressionValue a, ISExpressionValue b)
    {
        if (a is SExprNumber numA && b is SExprNumber numB)
        {
            // Compare numerically with tolerance for Coord fixed-point precision loss.
            // The Coord system uses ~393701 units/mm, giving ~2.5e-6 mm error per unit.
            // A tolerance of 5e-6 accommodates this inherent limitation.
            return Math.Abs(numA.Value - numB.Value) < 5e-6 ||
                   (numA.Value != 0 && Math.Abs((numA.Value - numB.Value) / numA.Value) < 5e-6);
        }

        // For strings and symbols, compare text
        return a.ToString() == b.ToString();
    }
}
