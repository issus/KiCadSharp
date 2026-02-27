using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

/// <summary>
/// Round-trip tests for the 15 additional data features (FP-21 through SCH-10).
/// Each test constructs an S-expression string with the feature, parses it,
/// writes it, re-parses it, and verifies the feature survived the round-trip.
/// </summary>
public class DataFeatureRoundTripTests
{
    private static async Task<KiCadPcbComponent> ReadFootprintFromString(string sexpr)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sexpr));
        return await FootprintReader.ReadAsync(ms);
    }

    private static async Task<KiCadPcb> ReadPcbFromString(string sexpr)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sexpr));
        return await PcbReader.ReadAsync(ms);
    }

    private static async Task<KiCadSch> ReadSchFromString(string sexpr)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sexpr));
        return await SchReader.ReadAsync(ms);
    }

    // -------------------------------------------------------
    // FP-21: fp_text_private
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_FpTextPrivate_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (fp_text_private ""private_field"" (at 1 2) (layer ""F.Fab"")
                (effects (font (size 1 1) (thickness 0.15))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.TextPrivateRaw.Should().HaveCount(1);
        fp.TextPrivateRaw[0].Token.Should().Be("fp_text_private");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.TextPrivateRaw.Should().HaveCount(1);
        fp2.TextPrivateRaw[0].Token.Should().Be("fp_text_private");
    }

    // -------------------------------------------------------
    // FP-22/56: render_cache on fp_text
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_FpTextRenderCache_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (fp_text reference ""REF**"" (at 0 0) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15)))
                (render_cache ""REF**"" 0
                    (polygon (pts (xy 0 0) (xy 1 0) (xy 1 1) (xy 0 1)))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.Texts.Should().HaveCount(1);
        var text = (KiCadPcbText)fp.Texts[0];
        text.RenderCache.Should().NotBeNull();
        text.RenderCache!.Token.Should().Be("render_cache");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        var text2 = (KiCadPcbText)fp2.Texts[0];
        text2.RenderCache.Should().NotBeNull();
        text2.RenderCache!.Token.Should().Be("render_cache");
    }

    // -------------------------------------------------------
    // FP-23: teardrop on footprint
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_Teardrop_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (teardrop (type padvia)
                (width_percent 40) (max_width 0.5)
                (best_length_percent 50) (max_length 1))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.TeardropRaw.Should().NotBeNull();
        fp.TeardropRaw!.Token.Should().Be("teardrop");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.TeardropRaw.Should().NotBeNull();
        fp2.TeardropRaw!.Token.Should().Be("teardrop");
    }

    // -------------------------------------------------------
    // FP-24: net_tie_pad_groups
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_NetTiePadGroups_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (net_tie_pad_groups ""1,2"" ""3,4"")
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.NetTiePadGroupsRaw.Should().NotBeNull();
        fp.NetTiePadGroupsRaw!.Token.Should().Be("net_tie_pad_groups");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.NetTiePadGroupsRaw.Should().NotBeNull();
        fp2.NetTiePadGroupsRaw!.Token.Should().Be("net_tie_pad_groups");
    }

    // -------------------------------------------------------
    // FP-80: Pad custom primitives
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_PadPrimitives_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (pad ""1"" smd custom (at 0 0) (size 1 1) (layers ""F.Cu"")
                (primitives
                    (gr_poly (pts (xy -0.5 -0.5) (xy 0 -0.25) (xy 0.5 -0.5) (xy 0 0.5))
                        (width 0.1) (fill yes))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.Pads.Should().HaveCount(1);
        var pad = (KiCadPcbPad)fp.Pads[0];
        pad.PrimitivesRaw.Should().NotBeNull();
        pad.PrimitivesRaw!.Token.Should().Be("primitives");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        var pad2 = (KiCadPcbPad)fp2.Pads[0];
        pad2.PrimitivesRaw.Should().NotBeNull();
        pad2.PrimitivesRaw!.Token.Should().Be("primitives");
    }

    // -------------------------------------------------------
    // FP-83: locked on individual pads
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_PadLocked_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (pad ""1"" thru_hole circle locked (at 0 0) (size 1.5 1.5) (drill 0.8) (layers ""*.Cu""))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        var pad = (KiCadPcbPad)fp.Pads[0];
        pad.IsLocked.Should().BeTrue();

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        var pad2 = (KiCadPcbPad)fp2.Pads[0];
        pad2.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task Footprint_PadNotLocked_DefaultsFalse()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (pad ""1"" thru_hole circle (at 0 0) (size 1.5 1.5) (drill 0.8) (layers ""*.Cu""))
        )";
        var fp = await ReadFootprintFromString(sexpr);
        var pad = (KiCadPcbPad)fp.Pads[0];
        pad.IsLocked.Should().BeFalse();
    }

    // -------------------------------------------------------
    // FP-84: fp_text_box
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_FpTextBox_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (fp_text_box ""Hello World"" (at 5 5) (size 10 5) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.TextBoxesRaw.Should().HaveCount(1);
        fp.TextBoxesRaw[0].Token.Should().Be("fp_text_box");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.TextBoxesRaw.Should().HaveCount(1);
        fp2.TextBoxesRaw[0].Token.Should().Be("fp_text_box");
    }

    // -------------------------------------------------------
    // FP-88: private_layers on footprint
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_PrivateLayers_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (private_layers ""User.1"" ""User.2"")
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.PrivateLayersRaw.Should().NotBeNull();
        fp.PrivateLayersRaw!.Token.Should().Be("private_layers");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.PrivateLayersRaw.Should().NotBeNull();
        fp2.PrivateLayersRaw!.Token.Should().Be("private_layers");
    }

    // -------------------------------------------------------
    // FP-89: Zones within footprints
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_Zones_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (zone (net 0) (net_name """") (layer ""F.Cu"") (name ""zone1"")
                (polygon (pts (xy 0 0) (xy 10 0) (xy 10 10) (xy 0 10))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.ZonesRaw.Should().HaveCount(1);
        fp.ZonesRaw[0].Token.Should().Be("zone");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.ZonesRaw.Should().HaveCount(1);
        fp2.ZonesRaw[0].Token.Should().Be("zone");
    }

    // -------------------------------------------------------
    // FP-90: Groups within footprints
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_Groups_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (group ""my_group"" (id ""abc-123"") (members ""def-456"" ""ghi-789""))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        fp.GroupsRaw.Should().HaveCount(1);
        fp.GroupsRaw[0].Token.Should().Be("group");

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        fp2.GroupsRaw.Should().HaveCount(1);
        fp2.GroupsRaw[0].Token.Should().Be("group");
    }

    // -------------------------------------------------------
    // FP-92: unlocked on fp_text
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_FpTextUnlocked_RoundTrips()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (fp_text reference ""REF**"" unlocked (at 0 -1) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15))))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        var text = (KiCadPcbText)fp.Texts[0];
        text.IsUnlocked.Should().BeTrue();

        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);
        var text2 = (KiCadPcbText)fp2.Texts[0];
        text2.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Footprint_FpTextNotUnlocked_DefaultsFalse()
    {
        var sexpr = @"(footprint ""TestFP"" (layer ""F.Cu"")
            (fp_text reference ""REF**"" (at 0 -1) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15))))
        )";
        var fp = await ReadFootprintFromString(sexpr);
        var text = (KiCadPcbText)fp.Texts[0];
        text.IsUnlocked.Should().BeFalse();
    }

    // -------------------------------------------------------
    // PCB-58: render_cache on gr_text
    // -------------------------------------------------------
    [Fact]
    public async Task Pcb_GrTextRenderCache_RoundTrips()
    {
        var sexpr = @"(kicad_pcb (version 20231014) (generator ""test"")
            (gr_text ""Board Title"" (at 100 50) (layer ""F.SilkS"")
                (effects (font (size 1.5 1.5) (thickness 0.3)))
                (render_cache ""Board Title"" 0
                    (polygon (pts (xy 0 0) (xy 1 0) (xy 1 1) (xy 0 1)))))
        )";
        var pcb = await ReadPcbFromString(sexpr);

        pcb.Texts.Should().HaveCount(1);
        var text = (KiCadPcbText)pcb.Texts[0];
        text.RenderCache.Should().NotBeNull();
        text.RenderCache!.Token.Should().Be("render_cache");

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        var text2 = (KiCadPcbText)pcb2.Texts[0];
        text2.RenderCache.Should().NotBeNull();
        text2.RenderCache!.Token.Should().Be("render_cache");
    }

    // -------------------------------------------------------
    // PCB-59: knockout on text
    // -------------------------------------------------------
    [Fact]
    public async Task Pcb_GrTextKnockout_RoundTrips()
    {
        var sexpr = @"(kicad_pcb (version 20231014) (generator ""test"")
            (gr_text ""Board Title"" (at 100 50) (layer ""F.SilkS"" knockout)
                (effects (font (size 1.5 1.5) (thickness 0.3))))
        )";
        var pcb = await ReadPcbFromString(sexpr);

        var text = (KiCadPcbText)pcb.Texts[0];
        text.IsKnockout.Should().BeTrue();

        using var ms = new MemoryStream();
        await PcbWriter.WriteAsync(pcb, ms);
        ms.Position = 0;

        var pcb2 = await PcbReader.ReadAsync(ms);
        var text2 = (KiCadPcbText)pcb2.Texts[0];
        text2.IsKnockout.Should().BeTrue();
    }

    [Fact]
    public async Task Pcb_GrTextNotKnockout_DefaultsFalse()
    {
        var sexpr = @"(kicad_pcb (version 20231014) (generator ""test"")
            (gr_text ""Board Title"" (at 100 50) (layer ""F.SilkS"")
                (effects (font (size 1.5 1.5) (thickness 0.3))))
        )";
        var pcb = await ReadPcbFromString(sexpr);
        var text = (KiCadPcbText)pcb.Texts[0];
        text.IsKnockout.Should().BeFalse();
    }

    // -------------------------------------------------------
    // SCH-10: images at schematic level
    // -------------------------------------------------------
    [Fact]
    public async Task Sch_Images_RoundTrips()
    {
        var sexpr = @"(kicad_sch (version 20231120) (generator ""test"")
            (uuid ""12345678-1234-1234-1234-123456789abc"")
            (image (at 100 50) (uuid ""aabbccdd-1234-5678-9abc-def012345678"")
                (data ""iVBORw0KGgo=""))
        )";
        var sch = await ReadSchFromString(sexpr);

        sch.ImagesRaw.Should().HaveCount(1);
        sch.ImagesRaw[0].Token.Should().Be("image");

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.ImagesRaw.Should().HaveCount(1);
        sch2.ImagesRaw[0].Token.Should().Be("image");
    }

    [Fact]
    public async Task Sch_MultipleImages_RoundTrips()
    {
        var sexpr = @"(kicad_sch (version 20231120) (generator ""test"")
            (uuid ""12345678-1234-1234-1234-123456789abc"")
            (image (at 100 50) (uuid ""aabbccdd-1234-5678-9abc-def012345678"")
                (data ""iVBORw0KGgo=""))
            (image (at 200 100) (uuid ""bbccddee-1234-5678-9abc-def012345678"")
                (data ""R0lGODlhAQAB""))
        )";
        var sch = await ReadSchFromString(sexpr);

        sch.ImagesRaw.Should().HaveCount(2);

        using var ms = new MemoryStream();
        await SchWriter.WriteAsync(sch, ms);
        ms.Position = 0;

        var sch2 = await SchReader.ReadAsync(ms);
        sch2.ImagesRaw.Should().HaveCount(2);
    }

    // -------------------------------------------------------
    // Combined footprint test: all features at once
    // -------------------------------------------------------
    [Fact]
    public async Task Footprint_AllNewFeatures_RoundTrips()
    {
        var sexpr = @"(footprint ""FullFeatureFP"" (layer ""F.Cu"")
            (private_layers ""User.1"")
            (net_tie_pad_groups ""1,2"")
            (fp_text reference ""REF**"" unlocked (at 0 -1) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15)))
                (render_cache ""REF**"" 0
                    (polygon (pts (xy 0 0) (xy 1 0) (xy 1 1)))))
            (fp_text_private ""private1"" (at 1 2) (layer ""F.Fab"")
                (effects (font (size 1 1) (thickness 0.15))))
            (fp_text_box ""box text"" (at 5 5) (size 10 5) (layer ""F.SilkS"")
                (effects (font (size 1 1) (thickness 0.15))))
            (pad ""1"" smd custom locked (at 0 0) (size 1 1) (layers ""F.Cu"")
                (primitives
                    (gr_line (start 0 0) (end 1 1) (width 0.1))))
            (teardrop (type padvia) (width_percent 40))
            (zone (net 0) (net_name """") (layer ""F.Cu"")
                (polygon (pts (xy 0 0) (xy 10 0) (xy 10 10))))
            (group ""grp"" (id ""abc"") (members ""def""))
        )";
        var fp = await ReadFootprintFromString(sexpr);

        // Verify all features parsed
        fp.PrivateLayersRaw.Should().NotBeNull();
        fp.NetTiePadGroupsRaw.Should().NotBeNull();
        fp.TextPrivateRaw.Should().HaveCount(1);
        fp.TextBoxesRaw.Should().HaveCount(1);
        fp.TeardropRaw.Should().NotBeNull();
        fp.ZonesRaw.Should().HaveCount(1);
        fp.GroupsRaw.Should().HaveCount(1);

        var text = (KiCadPcbText)fp.Texts[0];
        text.IsUnlocked.Should().BeTrue();
        text.RenderCache.Should().NotBeNull();

        var pad = (KiCadPcbPad)fp.Pads[0];
        pad.IsLocked.Should().BeTrue();
        pad.PrimitivesRaw.Should().NotBeNull();

        // Round-trip
        using var ms = new MemoryStream();
        await FootprintWriter.WriteAsync(fp, ms);
        ms.Position = 0;

        var fp2 = await FootprintReader.ReadAsync(ms);

        fp2.PrivateLayersRaw.Should().NotBeNull();
        fp2.NetTiePadGroupsRaw.Should().NotBeNull();
        fp2.TextPrivateRaw.Should().HaveCount(1);
        fp2.TextBoxesRaw.Should().HaveCount(1);
        fp2.TeardropRaw.Should().NotBeNull();
        fp2.ZonesRaw.Should().HaveCount(1);
        fp2.GroupsRaw.Should().HaveCount(1);

        var text2 = (KiCadPcbText)fp2.Texts[0];
        text2.IsUnlocked.Should().BeTrue();
        text2.RenderCache.Should().NotBeNull();

        var pad2 = (KiCadPcbPad)fp2.Pads[0];
        pad2.IsLocked.Should().BeTrue();
        pad2.PrimitivesRaw.Should().NotBeNull();
    }
}
