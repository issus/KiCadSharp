using System.Collections;
using System.Reflection;
using FluentAssertions;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;
using OriginalCircuit.KiCad.SExpression;
using Xunit;
using Xunit.Abstractions;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Tests.Serialization;

/// <summary>
/// Verifies that all typed properties on model classes are populated during deserialization
/// and survive round-trip through the writer. Complements <see cref="ModelRoundTripVerification"/>
/// which checks S-expression-level fidelity.
/// </summary>
public class PropertyCoverageVerification
{
    private readonly ITestOutputHelper _output;

    public PropertyCoverageVerification(ITestOutputHelper output) => _output = output;

    // ── Property classification ──────────────────────────────────────────

    private static bool IsRawProperty(PropertyInfo prop)
    {
        if (prop.Name == "SourceTree") return true;
        if (prop.Name.EndsWith("Raw", StringComparison.Ordinal)) return true;
        if (prop.PropertyType == typeof(SExpr)) return true;
        if (prop.Name == "GeneratedElements") return true; // List<SExpr>
        // Collection properties of SExpr
        if (IsCollectionOfSExpr(prop)) return true;
        return false;
    }

    private static bool IsCollectionOfSExpr(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        if (type.IsGenericType)
        {
            var elementType = type.GetGenericArguments().FirstOrDefault();
            if (elementType == typeof(SExpr)) return true;
        }
        return false;
    }

    private static bool IsHintProperty(PropertyInfo prop)
    {
        var name = prop.Name;
        // Format distinction flags
        if (name.EndsWith("IsSymbol", StringComparison.Ordinal)) return true;
        if (name.EndsWith("IsChildNode", StringComparison.Ordinal)) return true;
        if (name.StartsWith("Has", StringComparison.Ordinal) && prop.PropertyType == typeof(bool)) return true;
        if (name.EndsWith("Present", StringComparison.Ordinal)) return true;
        if (name == "PositionIncludesAngle") return true;
        if (name == "UsesChildNodeFlags") return true;
        if (name == "UsePcbFillFormat") return true;
        if (name == "UseBodyStyleToken") return true;
        if (name == "HideIsSymbolValue") return true;
        if (name == "HideIsDirectChild") return true;
        if (name == "IsInline") return true;
        if (name == "DoNotAutoplaceHasValue") return true;
        if (name == "UnlockedInAtNode") return true;
        if (name == "UuidAfterEffects") return true;
        if (name == "FillColorOnly") return true;
        if (name == "UuidToken") return true;
        if (name == "RootToken") return true;
        return false;
    }

    private static bool IsComputedProperty(PropertyInfo prop)
    {
        // Get-only properties with no setter are always computed
        if (!prop.CanWrite) return true;

        if (prop.Name == "HasErrors") return true;
        if (prop.Name == "AllComponents") return true;

        // Arc Center/Radius/StartAngle/EndAngle are derived from ArcStart/ArcMid/ArcEnd
        // during parsing. They're recalculated on round-trip and have inherent Coord
        // fixed-point precision drift that makes exact comparison impossible.
        var declType = prop.DeclaringType;
        if (declType != null &&
            (declType.Name == "KiCadSchArc" || declType.Name == "KiCadPcbArc") &&
            prop.Name is "Center" or "Radius" or "StartAngle" or "EndAngle")
            return true;

        return false;
    }

    private static bool IsInfrastructureProperty(PropertyInfo prop)
    {
        if (prop.Name == "Diagnostics") return true;
        if (prop.Name.EndsWith("List", StringComparison.Ordinal) && !prop.PropertyType.IsValueType) return true;
        // Order-tracking collections
        if (prop.Name == "BoardElementOrder") return true;
        if (prop.Name == "OrderedElements") return true;
        if (prop.Name == "OrderedPrimitives") return true;
        if (prop.Name == "GraphicalItemOrder") return true;
        return false;
    }

    /// <summary>
    /// Properties that exist only to satisfy shared EDA interfaces but are never populated
    /// by any KiCad reader because KiCad's format doesn't use them.
    /// </summary>
    private static bool IsInterfaceStubProperty(PropertyInfo prop)
    {
        var declType = prop.DeclaringType;
        if (declType == null) return false;

        var key = $"{declType.Name}.{prop.Name}";
        return key switch
        {
            // KiCad uses string LayerName instead of int Layer — no name→number mapping exists
            "KiCadPcbTrack.Layer" or "KiCadPcbPad.Layer" or "KiCadPcbArc.Layer" or
            "KiCadPcbText.Layer" or "KiCadPcbComponent.Layer" or "KiCadPcbCircle.Layer" or
            "KiCadPcbCurve.Layer" or "KiCadPcbPolygon.Layer" or "KiCadPcbRectangle.Layer" or
            "KiCadPcbVia.StartLayer" or "KiCadPcbVia.EndLayer" => true,

            // IPcbComponent.Height — KiCad files don't carry component height
            "KiCadPcbComponent.Height" => true,
            // IPcbText.StrokeWidth — KiCad uses FontThickness instead
            "KiCadPcbText.StrokeWidth" => true,
            // ISchNoConnect.Color — no_connect has no color in KiCad format
            "KiCadSchNoConnect.Color" => true,
            // ISchParameter.Color — parser populates FontColor instead
            "KiCadSchParameter.Color" => true,
            // ISchLabel.Color — parser populates FontColor instead
            "KiCadSchLabel.Color" => true,
            // ISchNetLabel.Color — parser populates font properties instead
            "KiCadSchNetLabel.Color" => true,

            _ => false
        };
    }

    private static bool IsTypedProperty(PropertyInfo prop)
    {
        if (!prop.CanRead) return false;
        if (prop.GetMethod?.IsPublic != true) return false;
        if (prop.GetIndexParameters().Length > 0) return false;
        if (IsRawProperty(prop)) return false;
        if (IsHintProperty(prop)) return false;
        if (IsComputedProperty(prop)) return false;
        if (IsInfrastructureProperty(prop)) return false;
        if (IsInterfaceStubProperty(prop)) return false;
        return true;
    }

    // ── Default value detection ──────────────────────────────────────────

    private static readonly Dictionary<Type, object?> s_defaultInstances = new();

    private static object? GetDefaultInstance(Type type)
    {
        if (s_defaultInstances.TryGetValue(type, out var cached))
            return cached;

        try
        {
            var instance = Activator.CreateInstance(type);
            s_defaultInstances[type] = instance;
            return instance;
        }
        catch
        {
            s_defaultInstances[type] = null;
            return null;
        }
    }

    private static bool IsPopulated(object instance, PropertyInfo prop)
    {
        object? value;
        try { value = prop.GetValue(instance); }
        catch { return false; }

        // Check companion presence flags before comparing to defaults.
        // Some properties (e.g. InBom defaults to true) are explicitly parsed but match
        // the constructor default, making them appear unpopulated without this check.
        if (HasExplicitPresenceFlag(instance, prop))
            return true;

        var defaultInstance = GetDefaultInstance(instance.GetType());
        if (defaultInstance != null)
        {
            object? defaultValue;
            try { defaultValue = prop.GetValue(defaultInstance); }
            catch { return value != null; }

            return !AreValuesEqual(value, defaultValue);
        }

        // Fallback: check against type defaults
        return !IsDefaultValue(value, prop.PropertyType);
    }

    /// <summary>
    /// Checks if a companion Has*/Present flag indicates the property was explicitly parsed.
    /// </summary>
    private static bool HasExplicitPresenceFlag(object instance, PropertyInfo prop)
    {
        var type = instance.GetType();

        // Check for Has{PropertyName} companion (e.g., HasClearance for Clearance)
        var hasFlag = type.GetProperty($"Has{prop.Name}", BindingFlags.Public | BindingFlags.Instance);
        if (hasFlag != null && hasFlag.PropertyType == typeof(bool))
        {
            try
            {
                if ((bool)hasFlag.GetValue(instance)!) return true;
            }
            catch { /* ignore */ }
        }

        // Check for {PropertyName}Present companion (e.g., InBomPresent for InBom)
        var presentFlag = type.GetProperty($"{prop.Name}Present", BindingFlags.Public | BindingFlags.Instance);
        if (presentFlag != null && presentFlag.PropertyType == typeof(bool))
        {
            try
            {
                if ((bool)presentFlag.GetValue(instance)!) return true;
            }
            catch { /* ignore */ }
        }

        // For KiCadPcbZone hatch properties, check if FillRaw is non-null
        // (hatch values are parsed from the fill node and default to 0)
        if (type.Name == "KiCadPcbZone" && prop.Name.StartsWith("Hatch", StringComparison.Ordinal)
            && prop.Name != "HatchStyle" && prop.Name != "HatchPitch")
        {
            var fillRaw = type.GetProperty("FillRaw", BindingFlags.Public | BindingFlags.Instance);
            if (fillRaw != null)
            {
                try
                {
                    if (fillRaw.GetValue(instance) != null) return true;
                }
                catch { /* ignore */ }
            }
        }

        return false;
    }

    private static bool IsDefaultValue(object? value, Type type)
    {
        if (value == null) return true;
        if (type == typeof(string)) return string.IsNullOrEmpty((string)value);
        if (type == typeof(int)) return (int)value == 0;
        if (type == typeof(double)) return (double)value == 0.0;
        if (type == typeof(bool)) return (bool)value == false;
        if (type == typeof(Coord)) return (Coord)value == Coord.Zero;
        if (type == typeof(CoordPoint)) return (CoordPoint)value == default;
        if (type == typeof(EdaColor)) return (EdaColor)value == default;
        if (type.IsEnum) return (int)value == 0;
        if (value is ICollection col) return col.Count == 0;
        if (value is IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            try { return !enumerator.MoveNext(); }
            finally { (enumerator as IDisposable)?.Dispose(); }
        }
        return false;
    }

    private static bool AreValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return a == null && b == null;

        var type = a.GetType();

        if (type == typeof(Coord))
            return Math.Abs(((Coord)a).ToMm() - ((Coord)b).ToMm()) < 5e-6;
        if (type == typeof(CoordPoint))
        {
            var pa = (CoordPoint)a;
            var pb = (CoordPoint)b;
            return Math.Abs(pa.X.ToMm() - pb.X.ToMm()) < 5e-6 &&
                   Math.Abs(pa.Y.ToMm() - pb.Y.ToMm()) < 5e-6;
        }
        if (type == typeof(EdaColor))
            return ((EdaColor)a).Equals((EdaColor)b);
        if (type == typeof(double))
            return Math.Abs((double)a - (double)b) < 5e-6;
        if (type == typeof(float))
            return Math.Abs((float)a - (float)b) < 5e-4f;

        if (a is string sa && b is string sb)
            return sa == sb;

        if (a is IReadOnlyList<CoordPoint> listA && b is IReadOnlyList<CoordPoint> listB)
        {
            if (listA.Count != listB.Count) return false;
            for (int i = 0; i < listA.Count; i++)
            {
                if (Math.Abs(listA[i].X.ToMm() - listB[i].X.ToMm()) > 5e-6 ||
                    Math.Abs(listA[i].Y.ToMm() - listB[i].Y.ToMm()) > 5e-6)
                    return false;
            }
            return true;
        }

        if (a is ICollection colA && b is ICollection colB)
            return colA.Count == colB.Count;

        return Equals(a, b);
    }

    // ── Object graph walker ──────────────────────────────────────────────

    private static readonly HashSet<string> s_modelNamespaces =
    [
        "OriginalCircuit.KiCad.Models.Pcb",
        "OriginalCircuit.KiCad.Models.Sch"
    ];

    private static bool IsModelType(Type type)
    {
        if (type.Namespace == null) return false;
        return s_modelNamespaces.Contains(type.Namespace) && type.IsClass && !type.IsAbstract;
    }

    private static IEnumerable<object> WalkModelGraph(object root)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<object>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == null || !visited.Add(current)) continue;

            var type = current.GetType();
            if (!IsModelType(type)) continue;

            yield return current;

            // Find collection properties that contain model objects
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0) continue;
                if (!prop.CanRead || prop.GetMethod?.IsPublic != true) continue;

                object? value;
                try { value = prop.GetValue(current); }
                catch { continue; }

                if (value == null) continue;

                // Single model object property
                if (IsModelType(value.GetType()))
                {
                    stack.Push(value);
                    continue;
                }

                // Collection of model objects
                if (value is IEnumerable enumerable && value is not string)
                {
                    foreach (var item in enumerable)
                    {
                        if (item != null && IsModelType(item.GetType()))
                            stack.Push(item);
                    }
                }
            }
        }
    }

    // ── File loading helpers ─────────────────────────────────────────────

    private static IEnumerable<string> GetTestFiles(string pattern)
    {
        var dirs = new[] { Path.Combine("TestData", "RealWorld"), Path.Combine("TestData", "Synthetic") };
        return dirs
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.GetFiles(dir, pattern, SearchOption.AllDirectories));
    }

    private async Task<List<(string path, object document)>> LoadAllDocumentsAsync()
    {
        var results = new List<(string, object)>();

        foreach (var f in GetTestFiles("*.kicad_pcb"))
        {
            try { results.Add((f, await PcbReader.ReadAsync(f))); }
            catch (Exception ex) { _output.WriteLine($"SKIP PCB {Path.GetFileName(f)}: {ex.Message}"); }
        }
        foreach (var f in GetTestFiles("*.kicad_sch"))
        {
            try { results.Add((f, await SchReader.ReadAsync(f))); }
            catch (Exception ex) { _output.WriteLine($"SKIP SCH {Path.GetFileName(f)}: {ex.Message}"); }
        }
        foreach (var f in GetTestFiles("*.kicad_sym"))
        {
            try { results.Add((f, await SymLibReader.ReadAsync(f))); }
            catch (Exception ex) { _output.WriteLine($"SKIP SYM {Path.GetFileName(f)}: {ex.Message}"); }
        }
        foreach (var f in GetTestFiles("*.kicad_mod"))
        {
            var text = await File.ReadAllTextAsync(f);
            if (text.TrimStart().StartsWith("(module")) continue; // Skip KiCad 5
            try { results.Add((f, await FootprintReader.ReadAsync(f))); }
            catch (Exception ex) { _output.WriteLine($"SKIP FP {Path.GetFileName(f)}: {ex.Message}"); }
        }

        return results;
    }

    // ── Test 1: Property population coverage ─────────────────────────────

    [Fact]
    public async Task AllTypedProperties_ArePopulated_InAtLeastOneFile()
    {
        var documents = await LoadAllDocumentsAsync();
        documents.Should().NotBeEmpty("test data must be present");

        // TypeName.PropertyName → (populatedCount, totalInstances)
        var coverage = new Dictionary<string, (int populated, int total)>();

        foreach (var (path, doc) in documents)
        {
            foreach (var obj in WalkModelGraph(doc))
            {
                var type = obj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(IsTypedProperty);

                foreach (var prop in props)
                {
                    var key = $"{type.Name}.{prop.Name}";
                    var (pop, tot) = coverage.GetValueOrDefault(key);
                    tot++;
                    if (IsPopulated(obj, prop)) pop++;
                    coverage[key] = (pop, tot);
                }
            }
        }

        // Report
        var unpopulated = new List<string>();
        foreach (var kvp in coverage.OrderBy(k => k.Key))
        {
            var pct = kvp.Value.total > 0 ? (100.0 * kvp.Value.populated / kvp.Value.total) : 0;
            if (kvp.Value.populated == 0)
            {
                _output.WriteLine($"  ZERO: {kvp.Key} (0/{kvp.Value.total})");
                unpopulated.Add(kvp.Key);
            }
        }

        _output.WriteLine($"\nTotal properties tracked: {coverage.Count}");
        _output.WriteLine($"Properties with coverage: {coverage.Count(k => k.Value.populated > 0)}");
        _output.WriteLine($"Properties with ZERO coverage: {unpopulated.Count}");

        if (unpopulated.Count > 0)
        {
            _output.WriteLine("\n--- Properties never populated across entire corpus ---");
            foreach (var p in unpopulated)
                _output.WriteLine($"  {p}");
        }

        var coveragePct = coverage.Count > 0
            ? 100.0 * coverage.Count(k => k.Value.populated > 0) / coverage.Count
            : 0;
        _output.WriteLine($"\nOverall typed property coverage: {coveragePct:F1}%");
        unpopulated.Should().BeEmpty("all typed properties must be populated by at least one file in the test corpus");
    }

    // ── Test 2: Round-trip property survival ─────────────────────────────

    [Theory]
    [MemberData(nameof(GetPcbFiles))]
    public async Task Pcb_AllProperties_SurviveRoundTrip(string path)
    {
        var original = await PcbReader.ReadAsync(path);
        original.SourceTree = null;

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(original, ms);
        ms.Position = 0;
        var roundTripped = await PcbReader.ReadAsync(ms);

        AssertGraphPropertiesMatch(original, roundTripped, Path.GetFileName(path));
    }

    [Theory]
    [MemberData(nameof(GetSchFiles))]
    public async Task Sch_AllProperties_SurviveRoundTrip(string path)
    {
        var original = await SchReader.ReadAsync(path);
        original.SourceTree = null;

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(original, ms);
        ms.Position = 0;
        var roundTripped = await SchReader.ReadAsync(ms);

        AssertGraphPropertiesMatch(original, roundTripped, Path.GetFileName(path));
    }

    [Theory]
    [MemberData(nameof(GetSymLibFiles))]
    public async Task SymLib_AllProperties_SurviveRoundTrip(string path)
    {
        var original = await SymLibReader.ReadAsync(path);
        original.SourceTree = null;

        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(original, ms);
        ms.Position = 0;
        var roundTripped = await SymLibReader.ReadAsync(ms);

        AssertGraphPropertiesMatch(original, roundTripped, Path.GetFileName(path));
    }

    [Theory]
    [MemberData(nameof(GetFootprintFiles))]
    public async Task Footprint_AllProperties_SurviveRoundTrip(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        if (text.TrimStart().StartsWith("(module")) return;

        var original = await FootprintReader.ReadAsync(path);

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(original, ms);
        ms.Position = 0;
        var roundTripped = await FootprintReader.ReadAsync(ms);

        AssertGraphPropertiesMatch(original, roundTripped, Path.GetFileName(path));
    }

    private void AssertGraphPropertiesMatch(object original, object roundTripped, string fileName)
    {
        var origObjects = WalkModelGraph(original).ToList();
        var rtObjects = WalkModelGraph(roundTripped).ToList();

        // Group by type for comparison
        var origByType = origObjects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());
        var rtByType = rtObjects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

        var mismatches = new List<string>();

        foreach (var (type, origList) in origByType)
        {
            if (!rtByType.TryGetValue(type, out var rtList))
            {
                mismatches.Add($"Type {type.Name}: present in original ({origList.Count}) but missing in round-trip");
                continue;
            }

            var count = Math.Min(origList.Count, rtList.Count);
            if (origList.Count != rtList.Count)
            {
                mismatches.Add($"Type {type.Name}: count mismatch {origList.Count} vs {rtList.Count}");
            }

            for (int i = 0; i < count; i++)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(IsTypedProperty);

                foreach (var prop in props)
                {
                    object? origVal, rtVal;
                    try { origVal = prop.GetValue(origList[i]); }
                    catch { continue; }
                    try { rtVal = prop.GetValue(rtList[i]); }
                    catch { continue; }

                    if (!AreValuesEqual(origVal, rtVal))
                    {
                        mismatches.Add(
                            $"{type.Name}[{i}].{prop.Name}: '{origVal}' → '{rtVal}'");
                    }
                }
            }
        }

        if (mismatches.Count > 0)
        {
            foreach (var m in mismatches.Take(50))
                _output.WriteLine(m);
            if (mismatches.Count > 50)
                _output.WriteLine($"... and {mismatches.Count - 50} more");
        }
        mismatches.Should().BeEmpty($"{fileName} should round-trip all typed properties");
    }

    // ── Test 3: Coverage report ──────────────────────────────────────────

    [Fact]
    public async Task GeneratePropertyCoverageReport()
    {
        var documents = await LoadAllDocumentsAsync();
        if (documents.Count == 0)
        {
            _output.WriteLine("No test data found - skipping report.");
            return;
        }

        // Gather all property info per type
        var typeProperties = new Dictionary<Type, Dictionary<string, (string category, int populated, int total)>>();

        foreach (var (_, doc) in documents)
        {
            foreach (var obj in WalkModelGraph(doc))
            {
                var type = obj.GetType();
                if (!typeProperties.TryGetValue(type, out var propMap))
                {
                    propMap = new Dictionary<string, (string, int, int)>();
                    typeProperties[type] = propMap;

                    // Initialize all properties
                    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.GetIndexParameters().Length > 0) continue;
                        if (!prop.CanRead || prop.GetMethod?.IsPublic != true) continue;

                        var category = IsRawProperty(prop) ? "Raw"
                            : IsHintProperty(prop) ? "Hint"
                            : IsComputedProperty(prop) ? "Computed"
                            : IsInfrastructureProperty(prop) ? "Infrastructure"
                            : IsInterfaceStubProperty(prop) ? "InterfaceStub"
                            : "Typed";
                        propMap.TryAdd(prop.Name, (category, 0, 0));
                    }
                }

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    if (!prop.CanRead || prop.GetMethod?.IsPublic != true) continue;

                    if (!propMap.ContainsKey(prop.Name)) continue;
                    var (cat, pop, tot) = propMap[prop.Name];
                    tot++;
                    if (IsTypedProperty(prop) && IsPopulated(obj, prop)) pop++;
                    else if (!IsTypedProperty(prop))
                    {
                        // For non-typed properties, still check if populated for reporting
                        try
                        {
                            var val = prop.GetValue(obj);
                            if (val != null && !IsDefaultValue(val, prop.PropertyType)) pop++;
                        }
                        catch { /* ignore */ }
                    }
                    propMap[prop.Name] = (cat, pop, tot);
                }
            }
        }

        // Output report
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  PROPERTY COVERAGE REPORT");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        int totalTyped = 0, totalTypedPopulated = 0;
        int totalRaw = 0, totalHint = 0, totalComputed = 0, totalInfra = 0;

        foreach (var (type, propMap) in typeProperties.OrderBy(t => t.Key.Name))
        {
            _output.WriteLine($"\n── {type.Name} ──");

            foreach (var (propName, (category, populated, total)) in propMap.OrderBy(p => p.Value.Item1).ThenBy(p => p.Key))
            {
                var pct = total > 0 ? $"{100.0 * populated / total:F0}%" : "N/A";
                var marker = category == "Typed" && populated == 0 && total > 0 ? " ◄ ZERO" : "";
                _output.WriteLine($"  [{category,-14}] {propName,-40} {populated,5}/{total,-5} ({pct}){marker}");

                switch (category)
                {
                    case "Typed":
                        totalTyped++;
                        if (populated > 0) totalTypedPopulated++;
                        break;
                    case "Raw": totalRaw++; break;
                    case "Hint": totalHint++; break;
                    case "Computed": totalComputed++; break;
                    case "Infrastructure": totalInfra++; break;
                }
            }
        }

        _output.WriteLine("\n═══════════════════════════════════════════════════════════");
        _output.WriteLine("  SUMMARY");
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine($"  Model types discovered:  {typeProperties.Count}");
        _output.WriteLine($"  Typed properties:        {totalTypedPopulated}/{totalTyped} populated ({(totalTyped > 0 ? 100.0 * totalTypedPopulated / totalTyped : 0):F1}%)");
        _output.WriteLine($"  Raw properties:          {totalRaw}");
        _output.WriteLine($"  Hint properties:         {totalHint}");
        _output.WriteLine($"  Computed properties:     {totalComputed}");
        _output.WriteLine($"  Infrastructure:          {totalInfra}");
    }

    // ── Data providers ───────────────────────────────────────────────────

    public static IEnumerable<object[]> GetPcbFiles() =>
        GetTestFiles("*.kicad_pcb").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetSchFiles() =>
        GetTestFiles("*.kicad_sch").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetSymLibFiles() =>
        GetTestFiles("*.kicad_sym").Select(f => new object[] { f });

    public static IEnumerable<object[]> GetFootprintFiles() =>
        GetTestFiles("*.kicad_mod").Select(f => new object[] { f });
}
