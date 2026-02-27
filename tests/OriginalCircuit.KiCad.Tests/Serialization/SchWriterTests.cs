using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class SchWriterTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task Write_MinimalSch_ProducesReparsableOutput()
    {
        var sch1 = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.Wires.Count.Should().Be(sch1.Wires.Count);
        sch2.Junctions.Count.Should().Be(sch1.Junctions.Count);
        sch2.NetLabels.Count.Should().Be(sch1.NetLabels.Count);
    }

    [Fact]
    public async Task Write_WireVertices_PreservedCorrectly()
    {
        var sch1 = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));
        if (sch1.Wires.Count == 0) return;

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        for (int i = 0; i < sch1.Wires.Count; i++)
        {
            var w1 = sch1.Wires[i];
            var w2 = sch2.Wires[i];
            w2.Vertices.Count.Should().Be(w1.Vertices.Count);
            for (int j = 0; j < w1.Vertices.Count; j++)
            {
                w2.Vertices[j].X.ToMm().Should().BeApproximately(w1.Vertices[j].X.ToMm(), 0.01);
                w2.Vertices[j].Y.ToMm().Should().BeApproximately(w1.Vertices[j].Y.ToMm(), 0.01);
            }
        }
    }

    [Fact]
    public async Task Write_EmptySch_ProducesValidOutput()
    {
        var sch = new KiCadSch();

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.Wires.Count.Should().Be(0);
    }

    [Fact]
    public async Task Write_Labels_PreservedCorrectly()
    {
        var sch1 = await SchReader.ReadAsync(TestDataPath("minimal.kicad_sch"));
        if (sch1.Labels.Count == 0 && sch1.NetLabels.Count == 0) return;

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch1, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.Labels.Count.Should().Be(sch1.Labels.Count);
        sch2.NetLabels.Count.Should().Be(sch1.NetLabels.Count);
    }

    [Fact]
    public async Task Write_GlobalLabel_UsesCorrectToken()
    {
        var sch = new KiCadSch();
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "VCC", LabelType = NetLabelType.Global, Uuid = Guid.NewGuid().ToString("D") });

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("global_label");
        text.Should().NotContain("(label \"VCC\"");
    }

    [Fact]
    public async Task Write_HierarchicalLabel_UsesCorrectToken()
    {
        var sch = new KiCadSch();
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "DATA", LabelType = NetLabelType.Hierarchical, Uuid = Guid.NewGuid().ToString("D") });

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("hierarchical_label");
    }

    [Fact]
    public async Task Write_LocalLabel_UsesLabelToken()
    {
        var sch = new KiCadSch();
        sch.AddNetLabel(new KiCadSchNetLabel { Text = "NET1", LabelType = NetLabelType.Local, Uuid = Guid.NewGuid().ToString("D") });

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("(label \"NET1\"");
    }
}
