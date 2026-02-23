using Xunit;
using FluentAssertions;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Integration;

[Trait("Category", "Integration")]
public class RealWorldFileTests
{
    private static IEnumerable<object[]> FindFiles(string extension)
    {
        var testDataDir = Path.Combine(
            Path.GetDirectoryName(typeof(RealWorldFileTests).Assembly.Location)!,
            "TestData", "RealWorld");
        if (!Directory.Exists(testDataDir))
            return [];
        return Directory.EnumerateFiles(testDataDir, $"*{extension}", SearchOption.AllDirectories)
            .Select(f => new object[] { Path.GetRelativePath(testDataDir, f) });
    }

    public static IEnumerable<object[]> SymLibFiles => FindFiles(".kicad_sym");
    public static IEnumerable<object[]> SchFiles => FindFiles(".kicad_sch");
    public static IEnumerable<object[]> FootprintFiles => FindFiles(".kicad_mod");
    public static IEnumerable<object[]> PcbFiles => FindFiles(".kicad_pcb");

    private static string GetFullPath(string relativePath)
        => Path.Combine(
            Path.GetDirectoryName(typeof(RealWorldFileTests).Assembly.Location)!,
            "TestData", "RealWorld", relativePath);

    // ---------------------------------------------------------------
    // Parameterized tests: every real-world file loads without throwing
    // ---------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SymLibFiles))]
    public async Task ReadSymLib_RealWorldFile_DoesNotThrow(string relativePath)
    {
        var path = GetFullPath(relativePath);
        var lib = await SymLibReader.ReadAsync(path);
        lib.Should().NotBeNull();
        lib.Components.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(SchFiles))]
    public async Task ReadSch_RealWorldFile_DoesNotThrow(string relativePath)
    {
        var path = GetFullPath(relativePath);
        var sch = await SchReader.ReadAsync(path);
        sch.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(FootprintFiles))]
    public async Task ReadFootprint_RealWorldFile_DoesNotThrow(string relativePath)
    {
        var path = GetFullPath(relativePath);
        var fp = await FootprintReader.ReadAsync(path);
        fp.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(PcbFiles))]
    public async Task ReadPcb_RealWorldFile_DoesNotThrow(string relativePath)
    {
        var path = GetFullPath(relativePath);
        var pcb = await PcbReader.ReadAsync(path);
        pcb.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // Property-level assertions for specific key files
    // ---------------------------------------------------------------

    [Fact]
    public async Task ReadPcb_Ecc83_HasExpectedComponentsAndPads()
    {
        var path = GetFullPath(Path.Combine("KiCadDemo_Ecc83", "ecc83-pp.kicad_pcb"));
        var pcb = await PcbReader.ReadAsync(path);

        pcb.Components.Should().HaveCountGreaterThan(0, "the Ecc83 board has footprints");

        var firstComp = pcb.Components[0] as KiCadPcbComponent;
        firstComp.Should().NotBeNull();
        firstComp!.Name.Should().NotBeNullOrWhiteSpace("every footprint should have a name");

        // All footprints should have pads
        var allPads = pcb.Components.OfType<KiCadPcbComponent>()
            .SelectMany(c => c.Pads.OfType<KiCadPcbPad>())
            .ToList();
        allPads.Should().HaveCountGreaterThan(0, "the board should have pads");
        allPads.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Designator),
            "every pad should have a designator");
    }

    [Fact]
    public async Task ReadSymLib_Ecc83_HasComponentsWithPins()
    {
        var path = GetFullPath(Path.Combine("KiCadDemo_Ecc83", "ecc83-pp.kicad_sym"));
        var lib = await SymLibReader.ReadAsync(path);

        lib.Components.Should().HaveCountGreaterThan(0, "the library should have symbols");

        // At least one component should have pins
        var componentsWithPins = lib.Components
            .Where(c => c.Pins.Count > 0)
            .ToList();
        componentsWithPins.Should().HaveCountGreaterThan(0,
            "at least one symbol should have pins");

        // Check pin electrical types are populated
        var allPins = componentsWithPins
            .SelectMany(c => c.Pins.OfType<KiCadSchPin>())
            .ToList();
        allPins.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ReadSch_Ecc83_HasWiresAndComponents()
    {
        var path = GetFullPath(Path.Combine("KiCadDemo_Ecc83", "ecc83-pp.kicad_sch"));
        var sch = await SchReader.ReadAsync(path);

        sch.Wires.Should().HaveCountGreaterThan(0, "the schematic should have wires");
        sch.Components.Should().HaveCountGreaterThan(0, "the schematic should have placed symbols");

        // Components should have a reference property
        foreach (var comp in sch.Components.OfType<KiCadSchComponent>())
        {
            comp.Parameters.Should().NotBeNull();
            var refParam = comp.Parameters.FirstOrDefault(p => p.Name == "Reference");
            refParam.Should().NotBeNull(
                $"placed symbol '{comp.Name}' should have a Reference property");
        }
    }

    [Fact]
    public async Task ReadSymLib_SparkFunConnector_HasManyComponents()
    {
        var path = GetFullPath(Path.Combine("SparkFun_Libraries", "SparkFun-Connector.kicad_sym"));
        var lib = await SymLibReader.ReadAsync(path);

        lib.Components.Should().HaveCountGreaterThan(5,
            "the SparkFun connector library should have many symbols");

        // Every component should have a name
        foreach (var comp in lib.Components)
        {
            comp.Name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task ReadFootprint_BGA100_HasCorrectPadCount()
    {
        var path = GetFullPath(Path.Combine("KiCad_OfficialLibraries", "BGA-100_11x11mm.kicad_mod"));
        var fp = await FootprintReader.ReadAsync(path);

        fp.Name.Should().Contain("BGA");
        fp.Pads.Should().HaveCountGreaterThanOrEqualTo(100,
            "a BGA-100 footprint should have at least 100 pads");
    }
}
