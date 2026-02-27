using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

public class PcbRoundTripTests
{
    private static string TestDataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    private static async Task<(KiCadPcb Original, KiCadPcb Reparsed, string Text)> RoundTrip(string file)
    {
        var pcb1 = await PcbReader.ReadAsync(TestDataPath(file));
        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb1, ms);
        ms.Position = 0;
        var text = new StreamReader(ms).ReadToEnd();
        ms.Position = 0;
        var pcb2 = await PcbReader.ReadAsync(ms);
        return (pcb1, pcb2, text);
    }

    // -- Phase A: Locked flags --

    [Fact]
    public async Task RoundTrip_LockedSegment_PreservesLockedFlag()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        var lockedTrack = orig.Tracks.OfType<KiCadPcbTrack>().FirstOrDefault(t => t.IsLocked);
        lockedTrack.Should().NotBeNull("there should be a locked segment in the test data");

        text.Should().Contain("(segment locked");

        var reLockedTrack = reparsed.Tracks.OfType<KiCadPcbTrack>().FirstOrDefault(t => t.IsLocked);
        reLockedTrack.Should().NotBeNull("locked flag should survive round-trip");
    }

    [Fact]
    public async Task RoundTrip_LockedArc_PreservesLockedFlag()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        var lockedArc = orig.Arcs.OfType<KiCadPcbArc>().FirstOrDefault(a => a.IsLocked);
        lockedArc.Should().NotBeNull("there should be a locked arc in the test data");

        text.Should().Contain("(arc locked");

        var reLockedArc = reparsed.Arcs.OfType<KiCadPcbArc>().FirstOrDefault(a => a.IsLocked);
        reLockedArc.Should().NotBeNull("locked flag should survive round-trip");
    }

    // -- Phase A: gr_text hide --

    [Fact]
    public async Task RoundTrip_HiddenGrText_PreservesHideFlag()
    {
        var (orig, reparsed, _) = await RoundTrip("roundtrip.kicad_pcb");

        var hiddenText = orig.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.IsHidden);
        hiddenText.Should().NotBeNull("there should be a hidden gr_text in test data");

        var reHiddenText = reparsed.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.IsHidden);
        reHiddenText.Should().NotBeNull("hidden flag should survive round-trip");
        reHiddenText!.Text.Should().Be("Hidden Text");
    }

    // -- Phase A: Font width --

    [Fact]
    public async Task RoundTrip_GrText_PreservesFontWidth()
    {
        var (orig, reparsed, _) = await RoundTrip("roundtrip.kicad_pcb");

        var boldText = orig.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.FontBold);
        boldText.Should().NotBeNull();
        boldText!.Height.ToMm().Should().BeApproximately(1.5, 0.01);
        boldText.FontWidth.ToMm().Should().BeApproximately(1.2, 0.01);

        var reBoldText = reparsed.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.FontBold);
        reBoldText.Should().NotBeNull();
        reBoldText!.Height.ToMm().Should().BeApproximately(1.5, 0.01);
        reBoldText.FontWidth.ToMm().Should().BeApproximately(1.2, 0.01);
    }

    // -- Phase A: Font thickness --

    [Fact]
    public async Task RoundTrip_GrText_PreservesFontThickness()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        var boldText = orig.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.FontBold);
        boldText.Should().NotBeNull();
        boldText!.FontThickness.ToMm().Should().BeApproximately(0.15, 0.01);

        text.Should().Contain("thickness");

        var reBoldText = reparsed.Texts.OfType<KiCadPcbText>().FirstOrDefault(t => t.FontBold);
        reBoldText.Should().NotBeNull();
        reBoldText!.FontThickness.ToMm().Should().BeApproximately(0.15, 0.01);
    }

    // -- Phase A: Footprint UUID --

    [Fact]
    public async Task RoundTrip_FootprintUuid_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        var comp = orig.Components.OfType<KiCadPcbComponent>().First();
        comp.Uuid.Should().NotBeNull();

        text.Should().Contain(comp.Uuid!);

        var reComp = reparsed.Components.OfType<KiCadPcbComponent>().First();
        reComp.Uuid.Should().Be(comp.Uuid);
    }

    // -- Phase B: Board-level graphics --

    [Fact]
    public async Task RoundTrip_GraphicLines_ArePreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.GraphicLines.Count.Should().Be(2);
        text.Should().Contain("gr_line");

        reparsed.GraphicLines.Count.Should().Be(2);
        reparsed.GraphicLines[0].LayerName.Should().Be("Edge.Cuts");
        reparsed.GraphicLines[0].Start.X.ToMm().Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public async Task RoundTrip_GraphicArc_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.GraphicArcs.Count.Should().Be(1);
        text.Should().Contain("gr_arc");

        reparsed.GraphicArcs.Count.Should().Be(1);
        reparsed.GraphicArcs[0].LayerName.Should().Be("F.SilkS");
        reparsed.GraphicArcs[0].StrokeStyle.Should().Be(LineStyle.Dash);
    }

    [Fact]
    public async Task RoundTrip_GraphicCircle_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.GraphicCircles.Count.Should().Be(1);
        text.Should().Contain("gr_circle");

        reparsed.GraphicCircles.Count.Should().Be(1);
        reparsed.GraphicCircles[0].Center.X.ToMm().Should().BeApproximately(50, 0.01);
    }

    [Fact]
    public async Task RoundTrip_GraphicRect_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.GraphicRects.Count.Should().Be(1);
        text.Should().Contain("gr_rect");

        reparsed.GraphicRects.Count.Should().Be(1);
        reparsed.GraphicRects[0].Start.X.ToMm().Should().BeApproximately(60, 0.01);
        reparsed.GraphicRects[0].End.X.ToMm().Should().BeApproximately(80, 0.01);
    }

    [Fact]
    public async Task RoundTrip_GraphicPoly_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.GraphicPolys.Count.Should().Be(1);
        text.Should().Contain("gr_poly");

        reparsed.GraphicPolys.Count.Should().Be(1);
        reparsed.GraphicPolys[0].Points.Count.Should().Be(3);
    }

    // -- Phase C: Document metadata --

    [Fact]
    public async Task RoundTrip_Paper_IsPreserved()
    {
        var (orig, _, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.PaperRaw.Should().NotBeNull();
        text.Should().Contain("(paper");
        text.Should().Contain("A4");
    }

    [Fact]
    public async Task RoundTrip_TitleBlock_IsPreserved()
    {
        var (orig, _, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.TitleBlockRaw.Should().NotBeNull();
        text.Should().Contain("title_block");
        text.Should().Contain("Test Board");
    }

    [Fact]
    public async Task RoundTrip_Layers_IsPreserved()
    {
        var (orig, _, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.LayersRaw.Should().NotBeNull();
        text.Should().Contain("(layers");
        text.Should().Contain("F.Cu");
        text.Should().Contain("B.Cu");
    }

    [Fact]
    public async Task RoundTrip_Setup_IsPreserved()
    {
        var (orig, _, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.SetupRaw.Should().NotBeNull();
        text.Should().Contain("(setup");
        text.Should().Contain("pad_to_mask_clearance");
    }

    // -- Phase C: Net classes --

    [Fact]
    public async Task RoundTrip_NetClass_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.NetClasses.Count.Should().Be(1);
        orig.NetClasses[0].Name.Should().Be("Default");
        orig.NetClasses[0].Description.Should().Contain("default net class");
        orig.NetClasses[0].Clearance.ToMm().Should().BeApproximately(0.2, 0.01);
        orig.NetClasses[0].TraceWidth.ToMm().Should().BeApproximately(0.25, 0.01);
        orig.NetClasses[0].NetNames.Should().Contain("GND");
        orig.NetClasses[0].NetNames.Should().Contain("VCC");

        text.Should().Contain("net_class");

        reparsed.NetClasses.Count.Should().Be(1);
        reparsed.NetClasses[0].Name.Should().Be("Default");
        reparsed.NetClasses[0].NetNames.Count.Should().Be(2);
    }

    // -- Phase D: Zone --

    [Fact]
    public async Task RoundTrip_Zone_IsPreserved()
    {
        var (orig, reparsed, text) = await RoundTrip("roundtrip.kicad_pcb");

        orig.Zones.Count.Should().Be(1);
        var zone = orig.Zones[0];
        zone.Net.Should().Be(1);
        zone.NetName.Should().Be("GND");
        zone.LayerName.Should().Be("F.Cu");
        zone.Name.Should().Be("GND_zone");
        zone.HatchStyle.Should().Be("edge");
        zone.Priority.Should().Be(1);
        zone.MinThickness.ToMm().Should().BeApproximately(0.2, 0.01);
        zone.IsFilled.Should().BeTrue();
        zone.ThermalGap.ToMm().Should().BeApproximately(0.5, 0.01);
        zone.Outline.Count.Should().Be(4);

        text.Should().Contain("(zone");
        text.Should().Contain("GND_zone");
        text.Should().Contain("(hatch edge");

        reparsed.Zones.Count.Should().Be(1);
        var reZone = reparsed.Zones[0];
        reZone.Net.Should().Be(1);
        reZone.NetName.Should().Be("GND");
        reZone.Name.Should().Be("GND_zone");
        reZone.HatchStyle.Should().Be("edge");
        reZone.Priority.Should().Be(1);
        reZone.Outline.Count.Should().Be(4);
        reZone.IsFilled.Should().BeTrue();
    }

    // -- Phase E: Status flags --

    [Fact]
    public async Task RoundTrip_SegmentStatus_IsPreserved()
    {
        var pcb = new KiCadPcb();
        pcb.AddTrack(new KiCadPcbTrack
        {
            Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
            End = new CoordPoint(Coord.FromMm(10), Coord.FromMm(0)),
            Width = Coord.FromMm(0.25),
            LayerName = "F.Cu",
            Status = 0x30000
        });

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;
        var text = new StreamReader(ms).ReadToEnd();
        text.Should().Contain("status");

        ms.Position = 0;
        var pcb2 = await PcbReader.ReadAsync(ms);
        var track = pcb2.Tracks.OfType<KiCadPcbTrack>().First();
        track.Status.Should().Be(0x30000);
    }

    // -- Programmatic graphic element round-trip --

    [Fact]
    public async Task RoundTrip_ProgrammaticGraphics_ArePreserved()
    {
        var pcb = new KiCadPcb();
        pcb.GraphicLineList.Add(new KiCadPcbGraphicLine
        {
            Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
            End = new CoordPoint(Coord.FromMm(50), Coord.FromMm(50)),
            StrokeWidth = Coord.FromMm(0.2),
            LayerName = "Edge.Cuts",
            Uuid = "test-line-uuid"
        });
        pcb.GraphicCircleList.Add(new KiCadPcbGraphicCircle
        {
            Center = new CoordPoint(Coord.FromMm(25), Coord.FromMm(25)),
            End = new CoordPoint(Coord.FromMm(30), Coord.FromMm(25)),
            StrokeWidth = Coord.FromMm(0.1),
            LayerName = "F.SilkS",
            Uuid = "test-circle-uuid"
        });

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.GraphicLines.Count.Should().Be(1);
        pcb2.GraphicLines[0].Uuid.Should().Be("test-line-uuid");
        pcb2.GraphicLines[0].Start.X.ToMm().Should().BeApproximately(0, 0.01);
        pcb2.GraphicLines[0].End.X.ToMm().Should().BeApproximately(50, 0.01);

        pcb2.GraphicCircles.Count.Should().Be(1);
        pcb2.GraphicCircles[0].Uuid.Should().Be("test-circle-uuid");
        pcb2.GraphicCircles[0].Center.X.ToMm().Should().BeApproximately(25, 0.01);
    }

    // -- Programmatic zone round-trip --

    [Fact]
    public async Task RoundTrip_ProgrammaticZone_IsPreserved()
    {
        var pcb = new KiCadPcb();
        pcb.ZoneList.Add(new KiCadPcbZone
        {
            Net = 1,
            NetName = "GND",
            LayerName = "F.Cu",
            Name = "TestZone",
            Priority = 2,
            HatchStyle = "edge",
            HatchPitch = 0.5,
            MinThickness = Coord.FromMm(0.25),
            Uuid = "test-zone-uuid",
            Outline =
            [
                new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
                new CoordPoint(Coord.FromMm(50), Coord.FromMm(0)),
                new CoordPoint(Coord.FromMm(50), Coord.FromMm(50)),
                new CoordPoint(Coord.FromMm(0), Coord.FromMm(50)),
            ]
        });

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.Zones.Count.Should().Be(1);
        pcb2.Zones[0].Net.Should().Be(1);
        pcb2.Zones[0].NetName.Should().Be("GND");
        pcb2.Zones[0].Name.Should().Be("TestZone");
        pcb2.Zones[0].Priority.Should().Be(2);
        pcb2.Zones[0].HatchStyle.Should().Be("edge");
        pcb2.Zones[0].MinThickness.ToMm().Should().BeApproximately(0.25, 0.01);
        pcb2.Zones[0].Outline.Count.Should().Be(4);
    }

    // -- Programmatic net class round-trip --

    [Fact]
    public async Task RoundTrip_ProgrammaticNetClass_IsPreserved()
    {
        var pcb = new KiCadPcb();
        pcb.NetClassList.Add(new KiCadPcbNetClass
        {
            Name = "Power",
            Description = "Power nets",
            Clearance = Coord.FromMm(0.3),
            TraceWidth = Coord.FromMm(0.5),
            ViaDia = Coord.FromMm(0.8),
            ViaDrill = Coord.FromMm(0.4),
            NetNames = ["VCC", "GND", "+5V"]
        });

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        pcb2.NetClasses.Count.Should().Be(1);
        pcb2.NetClasses[0].Name.Should().Be("Power");
        pcb2.NetClasses[0].Description.Should().Be("Power nets");
        pcb2.NetClasses[0].Clearance.ToMm().Should().BeApproximately(0.3, 0.01);
        pcb2.NetClasses[0].TraceWidth.ToMm().Should().BeApproximately(0.5, 0.01);
        pcb2.NetClasses[0].NetNames.Should().Contain("VCC");
        pcb2.NetClasses[0].NetNames.Should().Contain("GND");
        pcb2.NetClasses[0].NetNames.Should().Contain("+5V");
    }

    // -- Overall round-trip element counts --

    [Fact]
    public async Task RoundTrip_AllElementCounts_ArePreserved()
    {
        var (orig, reparsed, _) = await RoundTrip("roundtrip.kicad_pcb");

        reparsed.Version.Should().Be(orig.Version);
        reparsed.Components.Count.Should().Be(orig.Components.Count);
        reparsed.Tracks.Count.Should().Be(orig.Tracks.Count);
        reparsed.Vias.Count.Should().Be(orig.Vias.Count);
        reparsed.Arcs.Count.Should().Be(orig.Arcs.Count);
        reparsed.Texts.Count.Should().Be(orig.Texts.Count);
        reparsed.Nets.Count.Should().Be(orig.Nets.Count);
        reparsed.GraphicLines.Count.Should().Be(orig.GraphicLines.Count);
        reparsed.GraphicArcs.Count.Should().Be(orig.GraphicArcs.Count);
        reparsed.GraphicCircles.Count.Should().Be(orig.GraphicCircles.Count);
        reparsed.GraphicRects.Count.Should().Be(orig.GraphicRects.Count);
        reparsed.GraphicPolys.Count.Should().Be(orig.GraphicPolys.Count);
        reparsed.Zones.Count.Should().Be(orig.Zones.Count);
        reparsed.NetClasses.Count.Should().Be(orig.NetClasses.Count);
    }
}
