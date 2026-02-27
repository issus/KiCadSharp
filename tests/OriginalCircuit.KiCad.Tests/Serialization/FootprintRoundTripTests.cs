using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class FootprintRoundTripTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    private static async Task<KiCadPcbComponent> RoundTrip(KiCadPcbComponent fp)
    {
        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;
        return await FootprintReader.ReadAsync(ms);
    }

    // ===== Phase A: Simple writer pass-through =====

    [Fact]
    public async Task RoundTrip_PathPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Path.Should().Be("/test/path");

        var fp2 = await RoundTrip(fp);
        fp2.Path.Should().Be("/test/path");
    }

    [Fact]
    public async Task RoundTrip_UuidPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Uuid.Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var fp2 = await RoundTrip(fp);
        fp2.Uuid.Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    [Fact]
    public async Task RoundTrip_SolderPasteRatioPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.SolderPasteRatio.Should().BeApproximately(-10, 0.01);

        var fp2 = await RoundTrip(fp);
        fp2.SolderPasteRatio.Should().BeApproximately(-10, 0.01);
    }

    [Fact]
    public async Task RoundTrip_ZoneConnectPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.ZoneConnect.Should().Be(ZoneConnectionType.ThermalRelief);

        var fp2 = await RoundTrip(fp);
        fp2.ZoneConnect.Should().Be(ZoneConnectionType.ThermalRelief);
    }

    [Fact]
    public async Task RoundTrip_AutoplaceCostPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.AutoplaceCost90.Should().Be(5);
        fp.AutoplaceCost180.Should().Be(10);

        var fp2 = await RoundTrip(fp);
        fp2.AutoplaceCost90.Should().Be(5);
        fp2.AutoplaceCost180.Should().Be(10);
    }

    // ===== Phase B: fp_text improvements =====

    [Fact]
    public async Task RoundTrip_FontFacePreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.FontName.Should().Be("Arial");

        var fp2 = await RoundTrip(fp);
        var refText2 = fp2.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText2.FontName.Should().Be("Arial");
    }

    [Fact]
    public async Task RoundTrip_FontWidthPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.FontWidth.ToMm().Should().BeApproximately(0.8, 0.01);

        var fp2 = await RoundTrip(fp);
        var refText2 = fp2.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText2.FontWidth.ToMm().Should().BeApproximately(0.8, 0.01);
    }

    [Fact]
    public async Task RoundTrip_FontThicknessPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.FontThickness.ToMm().Should().BeApproximately(0.15, 0.01);

        var fp2 = await RoundTrip(fp);
        var refText2 = fp2.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText2.FontThickness.ToMm().Should().BeApproximately(0.15, 0.01);
    }

    [Fact]
    public async Task RoundTrip_JustificationPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.Justification.Should().Be(TextJustification.TopLeft);
        refText.IsMirrored.Should().BeTrue();

        var fp2 = await RoundTrip(fp);
        var refText2 = fp2.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText2.Justification.Should().Be(TextJustification.TopLeft);
        refText2.IsMirrored.Should().BeTrue();
    }

    [Fact]
    public async Task RoundTrip_FontBoldItalicPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var refText = fp.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText.FontBold.Should().BeTrue();
        refText.FontItalic.Should().BeTrue();

        var fp2 = await RoundTrip(fp);
        var refText2 = fp2.Texts.OfType<KiCadPcbText>().First(t => t.TextType == "reference");
        refText2.FontBold.Should().BeTrue();
        refText2.FontItalic.Should().BeTrue();
    }

    // ===== Phase C: Stroke/fill on graphical elements =====

    [Fact]
    public async Task RoundTrip_LineStrokeStylePreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var line = fp.Tracks.OfType<KiCadPcbTrack>().First();
        line.StrokeStyle.Should().Be(LineStyle.Dash);

        var fp2 = await RoundTrip(fp);
        var line2 = fp2.Tracks.OfType<KiCadPcbTrack>().First();
        line2.StrokeStyle.Should().Be(LineStyle.Dash);
    }

    [Fact]
    public async Task RoundTrip_LineStrokeColorPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var line = fp.Tracks.OfType<KiCadPcbTrack>().First();
        line.StrokeColor.R.Should().Be(255);
        line.StrokeColor.G.Should().Be(0);
        line.StrokeColor.B.Should().Be(0);

        var fp2 = await RoundTrip(fp);
        var line2 = fp2.Tracks.OfType<KiCadPcbTrack>().First();
        line2.StrokeColor.R.Should().Be(255);
        line2.StrokeColor.G.Should().Be(0);
        line2.StrokeColor.B.Should().Be(0);
    }

    [Fact]
    public async Task RoundTrip_LineFillPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var line = fp.Tracks.OfType<KiCadPcbTrack>().First();
        line.FillType.Should().Be(SchFillType.Filled);

        var fp2 = await RoundTrip(fp);
        var line2 = fp2.Tracks.OfType<KiCadPcbTrack>().First();
        line2.FillType.Should().Be(SchFillType.Filled);
    }

    [Fact]
    public async Task RoundTrip_ArcStrokeColorPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var arc = fp.Arcs.OfType<KiCadPcbArc>().First();
        arc.StrokeColor.G.Should().Be(128);

        var fp2 = await RoundTrip(fp);
        var arc2 = fp2.Arcs.OfType<KiCadPcbArc>().First();
        arc2.StrokeColor.G.Should().Be(128);
    }

    // ===== Phase D: Transform fixes - rect and circle preservation =====

    [Fact]
    public async Task RoundTrip_RectanglePreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Rectangles.Should().HaveCount(1);
        var rect = fp.Rectangles[0];
        rect.Start.X.ToMm().Should().BeApproximately(-2, 0.01);
        rect.Start.Y.ToMm().Should().BeApproximately(-1.5, 0.01);
        rect.End.X.ToMm().Should().BeApproximately(2, 0.01);
        rect.End.Y.ToMm().Should().BeApproximately(1.5, 0.01);
        rect.Uuid.Should().Be("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        var fp2 = await RoundTrip(fp);
        fp2.Rectangles.Should().HaveCount(1);
        var rect2 = fp2.Rectangles[0];
        rect2.Start.X.ToMm().Should().BeApproximately(-2, 0.01);
        rect2.Start.Y.ToMm().Should().BeApproximately(-1.5, 0.01);
        rect2.End.X.ToMm().Should().BeApproximately(2, 0.01);
        rect2.End.Y.ToMm().Should().BeApproximately(1.5, 0.01);
        rect2.Uuid.Should().Be("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }

    [Fact]
    public async Task RoundTrip_CirclePreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Circles.Should().HaveCount(1);
        var circle = fp.Circles[0];
        circle.Center.X.ToMm().Should().BeApproximately(0, 0.01);
        circle.Center.Y.ToMm().Should().BeApproximately(0, 0.01);
        circle.End.X.ToMm().Should().BeApproximately(1, 0.01);
        circle.End.Y.ToMm().Should().BeApproximately(0, 0.01);
        circle.StrokeStyle.Should().Be(LineStyle.Dot);
        circle.Uuid.Should().Be("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var fp2 = await RoundTrip(fp);
        fp2.Circles.Should().HaveCount(1);
        var circle2 = fp2.Circles[0];
        circle2.Center.X.ToMm().Should().BeApproximately(0, 0.01);
        circle2.Center.Y.ToMm().Should().BeApproximately(0, 0.01);
        circle2.End.X.ToMm().Should().BeApproximately(1, 0.01);
        circle2.End.Y.ToMm().Should().BeApproximately(0, 0.01);
        circle2.StrokeStyle.Should().Be(LineStyle.Dot);
        circle2.Uuid.Should().Be("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Fact]
    public async Task RoundTrip_RectangleNotDecomposedToLines()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        // fp_rect should NOT produce track entries (old behavior was 4 lines per rect)
        // There is 1 fp_line in the test data, so only 1 track
        fp.Tracks.Should().HaveCount(1);
        fp.Rectangles.Should().HaveCount(1);
    }

    [Fact]
    public async Task RoundTrip_CircleNotConvertedToArc()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        // fp_circle should NOT produce arc entries (old behavior was converting to arc)
        // There is 1 fp_arc in the test data, so only 1 arc
        fp.Arcs.Should().HaveCount(1);
        fp.Circles.Should().HaveCount(1);
    }

    // ===== Phase E: New elements =====

    [Fact]
    public async Task RoundTrip_PolygonPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Polygons.Should().HaveCount(1);
        var poly = fp.Polygons[0];
        poly.Points.Should().HaveCount(4);
        poly.LayerName.Should().Be("F.Cu");
        poly.FillType.Should().Be(SchFillType.Filled);
        poly.Uuid.Should().Be("22222222-3333-4444-5555-666666666666");

        var fp2 = await RoundTrip(fp);
        fp2.Polygons.Should().HaveCount(1);
        var poly2 = fp2.Polygons[0];
        poly2.Points.Should().HaveCount(4);
        poly2.Points[0].X.ToMm().Should().BeApproximately(-1, 0.01);
        poly2.Points[0].Y.ToMm().Should().BeApproximately(-1, 0.01);
        poly2.LayerName.Should().Be("F.Cu");
        poly2.FillType.Should().Be(SchFillType.Filled);
        poly2.Uuid.Should().Be("22222222-3333-4444-5555-666666666666");
    }

    [Fact]
    public async Task RoundTrip_Multiple3DModelsPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        fp.Models3D.Should().HaveCount(2);
        fp.Models3D[0].Path.Should().Contain("SOIC-8_3.9x4.9mm");
        fp.Models3D[1].Path.Should().Contain("SOIC-8_alt");
        fp.Models3D[1].OffsetZ.Should().BeApproximately(1.5, 0.01);

        var fp2 = await RoundTrip(fp);
        fp2.Models3D.Should().HaveCount(2);
        fp2.Models3D[0].Path.Should().Contain("SOIC-8_3.9x4.9mm");
        fp2.Models3D[1].Path.Should().Contain("SOIC-8_alt");
        fp2.Models3D[1].OffsetZ.Should().BeApproximately(1.5, 0.01);
        fp2.Models3D[1].Scale.X.ToMm().Should().BeApproximately(1.1, 0.01);
        fp2.Models3D[1].RotationZ.Should().BeApproximately(30, 0.01);
    }

    [Fact]
    public async Task RoundTrip_PadDrillOvalD2Preserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var pad2 = fp.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "2");
        pad2.HoleType.Should().Be(PadHoleType.Slot);
        pad2.HoleSize.ToMm().Should().BeApproximately(0.8, 0.01);
        pad2.DrillSizeY.ToMm().Should().BeApproximately(1.2, 0.01);

        var fp2 = await RoundTrip(fp);
        var pad2b = fp2.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "2");
        pad2b.HoleType.Should().Be(PadHoleType.Slot);
        pad2b.HoleSize.ToMm().Should().BeApproximately(0.8, 0.01);
        pad2b.DrillSizeY.ToMm().Should().BeApproximately(1.2, 0.01);
    }

    [Fact]
    public async Task RoundTrip_PadDrillOffsetPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var pad1 = fp.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "1");
        pad1.DrillOffset.X.ToMm().Should().BeApproximately(0.1, 0.01);
        pad1.DrillOffset.Y.ToMm().Should().BeApproximately(-0.05, 0.01);

        var fp2 = await RoundTrip(fp);
        var pad1b = fp2.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "1");
        pad1b.DrillOffset.X.ToMm().Should().BeApproximately(0.1, 0.01);
        pad1b.DrillOffset.Y.ToMm().Should().BeApproximately(-0.05, 0.01);
    }

    [Fact]
    public async Task RoundTrip_PadChamferPreserved()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));
        var pad1 = fp.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "1");
        pad1.ChamferRatio.Should().BeApproximately(0.1, 0.001);
        pad1.ChamferCorners.Should().Contain("top_left");
        pad1.ChamferCorners.Should().Contain("bottom_right");

        var fp2 = await RoundTrip(fp);
        var pad1b = fp2.Pads.OfType<KiCadPcbPad>().First(p => p.Designator == "1");
        pad1b.ChamferRatio.Should().BeApproximately(0.1, 0.001);
        pad1b.ChamferCorners.Should().Contain("top_left");
        pad1b.ChamferCorners.Should().Contain("bottom_right");
    }

    // ===== Phase F: Misc tokens =====

    [Fact]
    public async Task RoundTrip_LockedFlagOnShapes()
    {
        var fp = new KiCadPcbComponent { Name = "LockTest", LayerName = "F.Cu" };
        fp.AddTrack(new KiCadPcbTrack
        {
            Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
            End = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Width = Coord.FromMm(0.1),
            IsLocked = true,
            Uuid = "lock-test-line"
        });
        fp.AddRectangle(new KiCadPcbRectangle
        {
            Start = new CoordPoint(Coord.FromMm(-1), Coord.FromMm(-1)),
            End = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
            Width = Coord.FromMm(0.1),
            IsLocked = true,
            LayerName = "F.Cu",
            Uuid = "lock-test-rect"
        });

        var fp2 = await RoundTrip(fp);
        fp2.Tracks.OfType<KiCadPcbTrack>().First().IsLocked.Should().BeTrue();
        fp2.Rectangles[0].IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task RoundTrip_PlacedFlag()
    {
        var fp = new KiCadPcbComponent
        {
            Name = "PlacedTest",
            LayerName = "F.Cu",
            IsPlaced = true
        };

        var fp2 = await RoundTrip(fp);
        fp2.IsPlaced.Should().BeTrue();
    }

    [Fact]
    public async Task RoundTrip_ComplexFootprintPreservesAllBasicProperties()
    {
        var fp = await FootprintReader.ReadAsync(TestDataPath("complex.kicad_mod"));

        var fp2 = await RoundTrip(fp);

        fp2.Name.Should().Be(fp.Name);
        fp2.LayerName.Should().Be(fp.LayerName);
        fp2.Description.Should().Be(fp.Description);
        fp2.Tags.Should().Be(fp.Tags);
        fp2.Clearance.ToMm().Should().BeApproximately(fp.Clearance.ToMm(), 0.01);
        fp2.SolderMaskMargin.ToMm().Should().BeApproximately(fp.SolderMaskMargin.ToMm(), 0.01);
        fp2.SolderPasteMargin.ToMm().Should().BeApproximately(fp.SolderPasteMargin.ToMm(), 0.01);
        fp2.ThermalWidth.ToMm().Should().BeApproximately(fp.ThermalWidth.ToMm(), 0.01);
        fp2.ThermalGap.ToMm().Should().BeApproximately(fp.ThermalGap.ToMm(), 0.01);
        fp2.Attributes.Should().Be(fp.Attributes);
        fp2.Pads.Count.Should().Be(fp.Pads.Count);
        fp2.Texts.Count.Should().Be(fp.Texts.Count);
        fp2.Tracks.Count.Should().Be(fp.Tracks.Count);
        fp2.Arcs.Count.Should().Be(fp.Arcs.Count);
        fp2.Rectangles.Count.Should().Be(fp.Rectangles.Count);
        fp2.Circles.Count.Should().Be(fp.Circles.Count);
        fp2.Polygons.Count.Should().Be(fp.Polygons.Count);
    }

    [Fact]
    public async Task RoundTrip_BezierCurve()
    {
        var fp = new KiCadPcbComponent { Name = "CurveTest", LayerName = "F.Cu" };
        fp.AddCurve(new KiCadPcbCurve
        {
            Points =
            [
                new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
                new CoordPoint(Coord.FromMm(1), Coord.FromMm(2)),
                new CoordPoint(Coord.FromMm(3), Coord.FromMm(2)),
                new CoordPoint(Coord.FromMm(4), Coord.FromMm(0))
            ],
            Width = Coord.FromMm(0.1),
            LayerName = "F.SilkS",
            Uuid = "curve-test"
        });

        var fp2 = await RoundTrip(fp);
        fp2.Curves.Should().HaveCount(1);
        fp2.Curves[0].Points.Should().HaveCount(4);
        fp2.Curves[0].Points[1].X.ToMm().Should().BeApproximately(1, 0.01);
        fp2.Curves[0].Points[1].Y.ToMm().Should().BeApproximately(2, 0.01);
        fp2.Curves[0].LayerName.Should().Be("F.SilkS");
        fp2.Curves[0].Uuid.Should().Be("curve-test");
    }

    [Fact]
    public async Task RoundTrip_PadWithAllProperties()
    {
        var fp = new KiCadPcbComponent { Name = "PadTest", LayerName = "F.Cu" };
        fp.AddPad(new KiCadPcbPad
        {
            Designator = "A1",
            PadType = PadType.ThruHole,
            Shape = PadShape.RoundRect,
            Size = new CoordPoint(Coord.FromMm(2), Coord.FromMm(1.5)),
            HoleSize = Coord.FromMm(1),
            DrillOffset = new CoordPoint(Coord.FromMm(0.2), Coord.FromMm(-0.1)),
            CornerRadiusPercentage = 25,
            Layers = ["*.Cu", "*.Mask"],
            SolderMaskExpansion = Coord.FromMm(0.05),
            SolderPasteMargin = Coord.FromMm(0.02),
            SolderPasteRatio = 0.1,
            Clearance = Coord.FromMm(0.15),
            ThermalWidth = Coord.FromMm(0.3),
            ThermalGap = Coord.FromMm(0.4),
            ZoneConnect = ZoneConnectionType.Solid,
            DieLength = Coord.FromMm(0.5),
            ThermalBridgeAngle = 45,
            RemoveUnusedLayers = true,
            KeepEndLayers = true,
            ChamferRatio = 0.15,
            ChamferCorners = ["top_left", "top_right"],
            PadProperty = "pad_prop_bga",
            PinFunction = "CLK",
            PinType = "passive",
            Uuid = "pad-all-props"
        });

        var fp2 = await RoundTrip(fp);
        var pad = (KiCadPcbPad)fp2.Pads[0];
        pad.Designator.Should().Be("A1");
        pad.DrillOffset.X.ToMm().Should().BeApproximately(0.2, 0.01);
        pad.DrillOffset.Y.ToMm().Should().BeApproximately(-0.1, 0.01);
        pad.SolderMaskExpansion.ToMm().Should().BeApproximately(0.05, 0.01);
        pad.SolderPasteRatio.Should().BeApproximately(0.1, 0.001);
        pad.ThermalBridgeAngle.Should().BeApproximately(45, 0.01);
        pad.RemoveUnusedLayers.Should().BeTrue();
        pad.KeepEndLayers.Should().BeTrue();
        pad.ChamferRatio.Should().BeApproximately(0.15, 0.001);
        pad.ChamferCorners.Should().Contain("top_left");
        pad.ChamferCorners.Should().Contain("top_right");
        pad.PadProperty.Should().Be("pad_prop_bga");
        pad.PinFunction.Should().Be("CLK");
        pad.PinType.Should().Be("passive");
    }
}
