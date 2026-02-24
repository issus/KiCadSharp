using System.Xml.Linq;
using FluentAssertions;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using OriginalCircuit.KiCad.Rendering;
using OriginalCircuit.KiCad.Serialization;
using Xunit;

namespace OriginalCircuit.KiCad.Tests;

/// <summary>
/// Tests for the rendering system: CoordTransform, KiCadLayerColors,
/// and smoke tests for raster and SVG rendering of KiCad components.
/// </summary>
public sealed class RenderingTests
{
    // ── CoordTransform Tests ────────────────────────────────────────

    [Fact]
    public void CoordTransform_AutoZoom_SetsPositiveScale()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600
        };

        var bounds = new CoordRect(
            Coord.FromMils(0), Coord.FromMils(0),
            Coord.FromMils(1000), Coord.FromMils(800));

        transform.AutoZoom(bounds);

        transform.Scale.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CoordTransform_AutoZoom_CentersOnBounds()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600
        };

        // CoordRect(minX, minY, maxX, maxY) -> Min=(100,200), Max=(500,400)
        var bounds = new CoordRect(
            Coord.FromMils(100), Coord.FromMils(200),
            Coord.FromMils(500), Coord.FromMils(400));

        transform.AutoZoom(bounds);

        var expectedCenterX = (Coord.FromMils(100).ToRaw() + Coord.FromMils(500).ToRaw()) / 2.0;
        var expectedCenterY = (Coord.FromMils(200).ToRaw() + Coord.FromMils(400).ToRaw()) / 2.0;
        transform.CenterX.Should().BeApproximately(expectedCenterX, 1);
        transform.CenterY.Should().BeApproximately(expectedCenterY, 1);
    }

    [Fact]
    public void CoordTransform_WorldToScreen_OriginMapsToScreenCenter()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600,
            Scale = 0.01,
            CenterX = 0,
            CenterY = 0
        };

        var (sx, sy) = transform.WorldToScreen(Coord.FromMils(0), Coord.FromMils(0));

        sx.Should().BeApproximately(400, 0.1);
        sy.Should().BeApproximately(300, 0.1);
    }

    [Fact]
    public void CoordTransform_WorldToScreen_InvertsY()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600,
            Scale = 0.01,
            CenterX = 0,
            CenterY = 0
        };

        var (_, sy1) = transform.WorldToScreen(Coord.FromMils(0), Coord.FromMils(100));
        var (_, sy2) = transform.WorldToScreen(Coord.FromMils(0), Coord.FromMils(-100));

        sy1.Should().BeLessThan(sy2);
    }

    [Fact]
    public void CoordTransform_ScaleValue_ScalesCorrectly()
    {
        var transform = new CoordTransform { Scale = 0.01 };
        var value = Coord.FromMils(100);
        var scaled = transform.ScaleValue(value);

        scaled.Should().BeApproximately(value.ToRaw() * 0.01, 0.1);
    }

    [Fact]
    public void CoordTransform_AutoZoom_EmptyBounds_NoChange()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600,
            Scale = 1.0,
            CenterX = 42
        };

        var bounds = new CoordRect(Coord.Zero, Coord.Zero, Coord.Zero, Coord.Zero);
        transform.AutoZoom(bounds);

        transform.CenterX.Should().Be(42);
    }

    [Fact]
    public void CoordTransform_WorldToScreen_CoordPoint_Overload()
    {
        var transform = new CoordTransform
        {
            ScreenWidth = 800,
            ScreenHeight = 600,
            Scale = 0.01,
            CenterX = 0,
            CenterY = 0
        };

        var point = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0));
        var (sx, sy) = transform.WorldToScreen(point);

        sx.Should().BeApproximately(400, 0.1);
        sy.Should().BeApproximately(300, 0.1);
    }

    // ── KiCadLayerColors Tests ──────────────────────────────────────

    [Theory]
    [InlineData("F.Cu")]
    [InlineData("B.Cu")]
    [InlineData("F.SilkS")]
    [InlineData("B.SilkS")]
    [InlineData("F.Mask")]
    [InlineData("B.Mask")]
    [InlineData("Edge.Cuts")]
    public void KiCadLayerColors_GetColor_ReturnsNonZeroForKnownLayers(string layerName)
    {
        var color = KiCadLayerColors.GetColor(layerName);
        color.Should().NotBe(0u, $"layer '{layerName}' should have a non-zero color");
    }

    [Fact]
    public void KiCadLayerColors_GetColor_UnknownLayer_ReturnsFallback()
    {
        var color = KiCadLayerColors.GetColor("Nonexistent.Layer");
        color.Should().NotBe(0u, "unknown layers should return a fallback color");
    }

    [Fact]
    public void KiCadLayerColors_GetPriority_FrontCopperAboveBack()
    {
        var frontPriority = KiCadLayerColors.GetPriority("F.Cu");
        var backPriority = KiCadLayerColors.GetPriority("B.Cu");
        frontPriority.Should().BeGreaterThan(backPriority,
            "Front copper should have higher draw priority than back copper");
    }

    [Fact]
    public void KiCadLayerColors_GetColor_NullLayer_ReturnsFallback()
    {
        var color = KiCadLayerColors.GetColor(null);
        color.Should().NotBe(0u);
    }

    // ── Raster Renderer: Symbol Library ─────────────────────────────

    [Fact]
    public async Task RasterRenderer_SymLib_ProducesValidPng()
    {
        var lib = await SymLibReader.ReadAsync("TestData/minimal.kicad_sym");
        lib.Components.Should().NotBeEmpty();

        var renderer = new KiCadRasterRenderer();
        foreach (var component in lib.Components)
        {
            using var ms = new MemoryStream();
            await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

            ms.Length.Should().BeGreaterThan(0, $"rendering '{component.Name}' should produce output");
            ms.Position = 0;
            var header = new byte[4];
            ms.Read(header, 0, 4);
            header[0].Should().Be(0x89, "first byte should be PNG signature");
            header[1].Should().Be((byte)'P');
            header[2].Should().Be((byte)'N');
            header[3].Should().Be((byte)'G');
        }
    }

    // ── SVG Renderer: Symbol Library ────────────────────────────────

    [Fact]
    public async Task SvgRenderer_SymLib_ProducesValidSvg()
    {
        var lib = await SymLibReader.ReadAsync("TestData/minimal.kicad_sym");
        lib.Components.Should().NotBeEmpty();

        var renderer = new KiCadSvgRenderer();
        foreach (var component in lib.Components)
        {
            using var ms = new MemoryStream();
            await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

            ms.Length.Should().BeGreaterThan(0);
            ms.Position = 0;
            var svg = new StreamReader(ms).ReadToEnd();
            svg.Should().Contain("<svg");
            svg.Should().Contain("</svg>");
        }
    }

    // ── Raster Renderer: Footprint ──────────────────────────────────

    [Fact]
    public async Task RasterRenderer_Footprint_ProducesValidPng()
    {
        var component = await FootprintReader.ReadAsync("TestData/minimal.kicad_mod");

        var renderer = new KiCadRasterRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;
        var header = new byte[4];
        ms.Read(header, 0, 4);
        header[0].Should().Be(0x89);
    }

    // ── SVG Renderer: Footprint ─────────────────────────────────────

    [Fact]
    public async Task SvgRenderer_Footprint_ProducesValidSvg()
    {
        var component = await FootprintReader.ReadAsync("TestData/minimal.kicad_mod");

        var renderer = new KiCadSvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;
        var svg = new StreamReader(ms).ReadToEnd();
        svg.Should().Contain("<svg");
        svg.Should().Contain("</svg>");
    }

    // ── SVG Element Validation ──────────────────────────────────────

    [Fact]
    public async Task SvgRenderer_SymLib_ContainsBackgroundRect()
    {
        var lib = await SymLibReader.ReadAsync("TestData/minimal.kicad_sym");
        lib.Components.Should().NotBeEmpty();

        var renderer = new KiCadSvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(lib.Components[0], ms, new RenderOptions { Width = 512, Height = 512 });

        ms.Position = 0;
        var doc = XDocument.Load(ms);
        var ns = doc.Root!.Name.Namespace;

        doc.Descendants(ns + "rect").Should().NotBeEmpty("SVG should contain at least the background rect");
    }

    [Fact]
    public async Task SvgRenderer_Footprint_ContainsShapeElements()
    {
        var component = await FootprintReader.ReadAsync("TestData/minimal.kicad_mod");

        var renderer = new KiCadSvgRenderer();
        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 512, Height = 512 });

        ms.Position = 0;
        var doc = XDocument.Load(ms);
        var ns = doc.Root!.Name.Namespace;

        var totalElements = doc.Descendants(ns + "line").Count()
            + doc.Descendants(ns + "rect").Count()
            + doc.Descendants(ns + "ellipse").Count()
            + doc.Descendants(ns + "polygon").Count();

        totalElements.Should().BeGreaterThan(0, "PCB SVG should contain shape elements");
    }

    // ── Real World Files: Render Smoke Tests ────────────────────────

    [Theory]
    [InlineData("TestData/RealWorld/SparkFun_Libraries/SparkFun-Connector.kicad_sym")]
    [InlineData("TestData/RealWorld/KiCadDemo_Ecc83/ecc83-pp.kicad_sym")]
    [InlineData("TestData/RealWorld/KiCadDemo_TinyTapeout/TinyTapeout.kicad_sym")]
    public async Task RasterRenderer_RealWorldSymLib_ProducesOutput(string path)
    {
        if (!File.Exists(path)) return;

        var lib = await SymLibReader.ReadAsync(path);
        var renderer = new KiCadRasterRenderer();

        foreach (var component in lib.Components.Take(5))
        {
            using var ms = new MemoryStream();
            await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });
            ms.Length.Should().BeGreaterThan(0, $"rendering '{component.Name}' should produce output");
        }
    }

    [Theory]
    [InlineData("TestData/RealWorld/KiCad_OfficialLibraries/SOT-23.kicad_mod")]
    [InlineData("TestData/RealWorld/KiCad_OfficialLibraries/BGA-100_11x11mm.kicad_mod")]
    [InlineData("TestData/RealWorld/KiCad_OfficialLibraries/LQFP-144_20x20mm_P0.5mm.kicad_mod")]
    public async Task RasterRenderer_RealWorldFootprint_ProducesOutput(string path)
    {
        if (!File.Exists(path)) return;

        var component = await FootprintReader.ReadAsync(path);
        var renderer = new KiCadRasterRenderer();

        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 256, Height = 256 });
        ms.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("TestData/RealWorld/KiCad_OfficialLibraries/SOT-23.kicad_mod")]
    [InlineData("TestData/RealWorld/KiCad_OfficialLibraries/PinHeader_1x02_P2.54mm_Vertical.kicad_mod")]
    public async Task SvgRenderer_RealWorldFootprint_ProducesValidSvg(string path)
    {
        if (!File.Exists(path)) return;

        var component = await FootprintReader.ReadAsync(path);
        var renderer = new KiCadSvgRenderer();

        using var ms = new MemoryStream();
        await renderer.RenderAsync(component, ms, new RenderOptions { Width = 512, Height = 512 });

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;
        var svg = new StreamReader(ms).ReadToEnd();
        svg.Should().Contain("<svg");
    }
}
