using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class FootprintWriterTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    [Fact]
    public async Task Write_MinimalFootprint_ProducesReparsableOutput()
    {
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.Name.Should().Be(fp1.Name);
        fp2.LayerName.Should().Be(fp1.LayerName);
    }

    [Fact]
    public async Task Write_PadAttributes_PreservedCorrectly()
    {
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.Pads.Count.Should().Be(fp1.Pads.Count);
        for (int i = 0; i < fp1.Pads.Count; i++)
        {
            var p1 = (KiCadPcbPad)fp1.Pads[i];
            var p2 = (KiCadPcbPad)fp2.Pads[i];
            p2.Designator.Should().Be(p1.Designator);
            p2.Location.X.ToMm().Should().BeApproximately(p1.Location.X.ToMm(), 0.01);
            p2.Location.Y.ToMm().Should().BeApproximately(p1.Location.Y.ToMm(), 0.01);
            p2.Size.X.ToMm().Should().BeApproximately(p1.Size.X.ToMm(), 0.01);
            p2.Size.Y.ToMm().Should().BeApproximately(p1.Size.Y.ToMm(), 0.01);
        }
    }

    [Fact]
    public async Task Write_TextElements_PreservedCorrectly()
    {
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.Texts.Count.Should().Be(fp1.Texts.Count);
    }

    [Fact]
    public async Task Write_Description_PreservedCorrectly()
    {
        var fp1 = await FootprintReader.ReadAsync(TestDataPath("minimal.kicad_mod"));

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp1, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.Description.Should().Be(fp1.Description);
    }

    [Fact]
    public async Task Write_PadWithSolderPasteMargin_ProducesToken()
    {
        var fp = new KiCadPcbComponent { Name = "TestFP", LayerName = "F.Cu" };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "1",
            PadType = PadType.Smd,
            Shape = PadShape.Rect,
            Size = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Layers = ["F.Cu"],
            SolderPasteMargin = Coord.FromMm(0.1)
        });

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("solder_paste_margin");
    }

    [Fact]
    public async Task Write_3DModel_PreservesZValues()
    {
        var fp = new KiCadPcbComponent
        {
            Name = "TestFP",
            LayerName = "F.Cu",
            Model3D = "test.wrl",
            Model3DOffsetZ = 1.5,
            Model3DScaleZ = 2.0,
            Model3DRotationZ = 45.0
        };

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.Model3DOffsetZ.Should().BeApproximately(1.5, 0.01);
        fp2.Model3DScaleZ.Should().BeApproximately(2.0, 0.01);
        fp2.Model3DRotationZ.Should().BeApproximately(45.0, 0.01);
    }
}
