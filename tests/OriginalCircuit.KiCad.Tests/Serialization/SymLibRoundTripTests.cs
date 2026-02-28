using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Sch;
using OriginalCircuit.KiCad.Serialization;

namespace OriginalCircuit.KiCad.Tests.Serialization;

/// <summary>
/// Round-trip tests for symbol library (.kicad_sym) fidelity.
/// Each test creates a symbol library in memory, writes it, reads it back,
/// and verifies that the round-tripped data matches the original.
/// </summary>
public class SymLibRoundTripTests
{
    /// <summary>
    /// Helper: write a lib to a stream, rewind, read it back.
    /// </summary>
    private static async Task<KiCadSymLib> RoundTrip(KiCadSymLib lib)
    {
        using var ms = new MemoryStream();
        await SymLibWriter.WriteAsync(lib, ms);
        ms.Position = 0;
        return await SymLibReader.ReadAsync(ms);
    }

    /// <summary>
    /// Helper: builds a minimal symbol lib containing one symbol that has a sub-symbol
    /// with the given primitives.
    /// </summary>
    private static KiCadSymLib BuildLibWithSubSymbol(string symbolName, Action<KiCadSchComponent> configureSub)
    {
        var lib = new KiCadSymLib { Version = 20231120, Generator = "kicadsharp", GeneratorVersion = "1.0" };
        var comp = new KiCadSchComponent { Name = symbolName, InBom = true, OnBoard = true };
        var sub = new KiCadSchComponent { Name = $"{symbolName}_0_1" };
        configureSub(sub);
        comp.AddSubSymbol(sub);
        lib.Add(comp);
        return lib;
    }

    // ───── Rectangle stroke style/color and fill color ─────

    [Fact]
    public async Task RoundTrip_Rectangle_StrokeStyleAndColor()
    {
        var lib = BuildLibWithSubSymbol("TestRect", sub =>
        {
            sub.AddRectangle(new KiCadSchRectangle
            {
                Corner1 = new CoordPoint(Coord.FromMm(-5), Coord.FromMm(-5)),
                Corner2 = new CoordPoint(Coord.FromMm(5), Coord.FromMm(5)),
                LineWidth = Coord.FromMm(0.5),
                LineStyle = LineStyle.Dash,
                Color = new EdaColor(255, 0, 0, 255),
                FillType = SchFillType.Color,
                FillColor = new EdaColor(0, 255, 0, 255)
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestRect"]!;
        var rect = sym.Rectangles.OfType<KiCadSchRectangle>().Single();

        rect.LineWidth.ToMm().Should().BeApproximately(0.5, 0.01);
        rect.LineStyle.Should().Be(LineStyle.Dash);
        rect.Color.R.Should().Be(255);
        rect.Color.G.Should().Be(0);
        rect.Color.B.Should().Be(0);
        rect.FillType.Should().Be(SchFillType.Color);
        rect.FillColor.G.Should().Be(255);
    }

    // ───── Circle stroke style/color and fill color ─────

    [Fact]
    public async Task RoundTrip_Circle_StrokeStyleAndColor()
    {
        var lib = BuildLibWithSubSymbol("TestCircle", sub =>
        {
            sub.AddCircle(new KiCadSchCircle
            {
                Center = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
                Radius = Coord.FromMm(10),
                LineWidth = Coord.FromMm(0.3),
                LineStyle = LineStyle.Dot,
                Color = new EdaColor(0, 0, 255, 255),
                FillType = SchFillType.Background,
                FillColor = default
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestCircle"]!;
        var circle = sym.Circles.OfType<KiCadSchCircle>().Single();

        circle.LineWidth.ToMm().Should().BeApproximately(0.3, 0.01);
        circle.LineStyle.Should().Be(LineStyle.Dot);
        circle.Color.B.Should().Be(255);
        circle.FillType.Should().Be(SchFillType.Background);
    }

    // ───── Arc stroke style/color and fill ─────

    [Fact]
    public async Task RoundTrip_Arc_StrokeStyleColorAndFill()
    {
        var lib = BuildLibWithSubSymbol("TestArc", sub =>
        {
            sub.AddArc(new KiCadSchArc
            {
                ArcStart = new CoordPoint(Coord.FromMm(5), Coord.FromMm(0)),
                ArcMid = new CoordPoint(Coord.FromMm(0), Coord.FromMm(5)),
                ArcEnd = new CoordPoint(Coord.FromMm(-5), Coord.FromMm(0)),
                LineWidth = Coord.FromMm(0.4),
                LineStyle = LineStyle.DashDot,
                Color = new EdaColor(128, 64, 32, 255),
                FillType = SchFillType.Filled,
                FillColor = default
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestArc"]!;
        var arc = sym.Arcs.OfType<KiCadSchArc>().Single();

        arc.LineWidth.ToMm().Should().BeApproximately(0.4, 0.01);
        arc.LineStyle.Should().Be(LineStyle.DashDot);
        arc.Color.R.Should().Be(128);
        arc.Color.G.Should().Be(64);
        arc.Color.B.Should().Be(32);
        arc.FillType.Should().Be(SchFillType.Filled);
    }

    // ───── Polygon stroke style/color and fill color ─────

    [Fact]
    public async Task RoundTrip_Polygon_StrokeAndFill()
    {
        // Polygons are written as filled polylines (at least 3 points + fill)
        // We create a lib from S-expression to ensure polygon round-trip
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestPoly""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestPoly_0_1""
      (polyline
        (pts (xy 0 0) (xy 5 0) (xy 2.5 5))
        (stroke (width 0.3) (type dash) (color 100 50 25 1))
        (fill (type outline))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestPoly"]!;

        // Should be parsed as polygon (filled, 3+ points)
        sym.Polygons.Should().HaveCount(1);
        var poly = sym.Polygons.OfType<KiCadSchPolygon>().Single();
        poly.LineWidth.ToMm().Should().BeApproximately(0.3, 0.01);
        poly.LineStyle.Should().Be(LineStyle.Dash);
        poly.Color.R.Should().Be(100);
        poly.FillType.Should().Be(SchFillType.Filled);
    }

    // ───── Polyline stroke color and fill ─────

    [Fact]
    public async Task RoundTrip_Polyline_StrokeColor()
    {
        var lib = BuildLibWithSubSymbol("TestPolyline", sub =>
        {
            sub.AddPolyline(new KiCadSchPolyline
            {
                Vertices = [
                    new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
                    new CoordPoint(Coord.FromMm(5), Coord.FromMm(0)),
                    new CoordPoint(Coord.FromMm(10), Coord.FromMm(5))
                ],
                LineWidth = Coord.FromMm(0.25),
                LineStyle = LineStyle.Solid,
                Color = new EdaColor(200, 100, 50, 255)
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestPolyline"]!;
        var poly = sym.Polylines.OfType<KiCadSchPolyline>().Single();

        poly.LineWidth.ToMm().Should().BeApproximately(0.25, 0.01);
        poly.Color.R.Should().Be(200);
        poly.Color.G.Should().Be(100);
    }

    // ───── Bezier stroke style/color and fill ─────

    [Fact]
    public async Task RoundTrip_Bezier_StrokeAndFill()
    {
        var lib = BuildLibWithSubSymbol("TestBezier", sub =>
        {
            sub.AddBezier(new KiCadSchBezier
            {
                ControlPoints = [
                    new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
                    new CoordPoint(Coord.FromMm(2), Coord.FromMm(5)),
                    new CoordPoint(Coord.FromMm(8), Coord.FromMm(5)),
                    new CoordPoint(Coord.FromMm(10), Coord.FromMm(0))
                ],
                LineWidth = Coord.FromMm(0.2),
                LineStyle = LineStyle.DashDotDot,
                Color = new EdaColor(10, 20, 30, 255),
                FillType = SchFillType.Background
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestBezier"]!;
        var bez = sym.Beziers.OfType<KiCadSchBezier>().Single();

        bez.LineWidth.ToMm().Should().BeApproximately(0.2, 0.01);
        bez.LineStyle.Should().Be(LineStyle.DashDotDot);
        bez.Color.R.Should().Be(10);
        bez.Color.G.Should().Be(20);
        bez.FillType.Should().Be(SchFillType.Background);
    }

    // ───── Label font properties ─────

    [Fact]
    public async Task RoundTrip_Label_FontProperties()
    {
        var lib = BuildLibWithSubSymbol("TestLabel", sub =>
        {
            sub.AddLabel(new KiCadSchLabel
            {
                Text = "Hello World",
                Location = new CoordPoint(Coord.FromMm(1), Coord.FromMm(2)),
                Rotation = 90,
                FontSizeHeight = Coord.FromMm(2.54),
                FontSizeWidth = Coord.FromMm(2.54),
                IsBold = true,
                IsItalic = true,
                Justification = TextJustification.MiddleLeft,
                IsMirrored = true
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestLabel"]!;
        var label = sym.Labels.OfType<KiCadSchLabel>().Single();

        label.Text.Should().Be("Hello World");
        label.FontSizeHeight.ToMm().Should().BeApproximately(2.54, 0.01);
        label.FontSizeWidth.ToMm().Should().BeApproximately(2.54, 0.01);
        label.IsBold.Should().BeTrue();
        label.IsItalic.Should().BeTrue();
        label.Justification.Should().Be(TextJustification.MiddleLeft);
        label.IsMirrored.Should().BeTrue();
        label.Rotation.Should().Be(90);
    }

    // ───── Label stroke ─────

    [Fact]
    public async Task RoundTrip_Label_Stroke()
    {
        // Labels can have stroke - test via S-expression parsing
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestLabelStroke""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestLabelStroke_0_1""
      (text ""StrokeText""
        (at 0 0 0)
        (stroke (width 0.1) (type dash) (color 255 128 0 1))
        (effects (font (size 1.27 1.27)))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestLabelStroke"]!;
        var label = sym.Labels.OfType<KiCadSchLabel>().Single();

        label.StrokeWidth.ToMm().Should().BeApproximately(0.1, 0.01);
        label.StrokeLineStyle.Should().Be(LineStyle.Dash);
        label.StrokeColor.R.Should().Be(255);
        label.StrokeColor.G.Should().Be(128);
    }

    // ───── Parameter Id ─────

    [Fact]
    public async Task RoundTrip_Parameter_Id()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestParamId""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (id 0) (at 0 0 0)
      (effects (font (size 1.27 1.27)))
    )
    (property ""Value"" ""TestParamId"" (id 1) (at 0 -2 0)
      (effects (font (size 1.27 1.27)))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestParamId"]!;
        var refParam = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Reference");
        var valParam = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Value");

        refParam.Id.Should().Be(0);
        valParam.Id.Should().Be(1);
    }

    // ───── Parameter FontFace ─────

    [Fact]
    public async Task RoundTrip_Parameter_FontFace()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestFontFace""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (at 0 0 0)
      (effects (font (face ""Arial"") (size 1.27 1.27)))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestFontFace"]!;
        var param = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Reference");
        param.FontFace.Should().Be("Arial");
    }

    // ───── Parameter LineSpacing ─────

    [Fact]
    public async Task RoundTrip_Parameter_LineSpacing()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestLineSpacing""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (at 0 0 0)
      (effects (font (size 1.27 1.27)) (line_spacing 1.5))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestLineSpacing"]!;
        var param = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Reference");
        param.LineSpacing.Should().BeApproximately(1.5, 0.01);
    }

    // ───── Parameter FontColor ─────

    [Fact]
    public async Task RoundTrip_Parameter_FontColor()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestFontColor""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (at 0 0 0)
      (effects (font (size 1.27 1.27) (color 255 0 128 1)))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestFontColor"]!;
        var param = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Reference");
        param.FontColor.R.Should().Be(255);
        param.FontColor.G.Should().Be(0);
        param.FontColor.B.Should().Be(128);
    }

    // ───── Parameter Bold/Italic ─────

    [Fact]
    public async Task RoundTrip_Parameter_BoldItalic()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestBoldItalic""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (at 0 0 0)
      (effects (font (size 1.27 1.27) (bold) (italic)))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestBoldItalic"]!;
        var param = sym.Parameters.OfType<KiCadSchParameter>().First(p => p.Name == "Reference");
        param.IsBold.Should().BeTrue();
        param.IsItalic.Should().BeTrue();
    }

    // ───── Pin name/number font sizes ─────

    [Fact]
    public async Task RoundTrip_Pin_NameNumberFontSizes()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestPinFonts""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestPinFonts_0_1""
      (pin passive line (at 0 0 0) (length 2.54)
        (name ""A"" (effects (font (size 2.0 1.5))))
        (number ""1"" (effects (font (size 1.0 0.8))))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestPinFonts"]!;
        var pin = sym.Pins.OfType<KiCadSchPin>().Single();

        pin.Name.Should().Be("A");
        pin.Designator.Should().Be("1");
        pin.NameFontSizeHeight.ToMm().Should().BeApproximately(2.0, 0.01);
        pin.NameFontSizeWidth.ToMm().Should().BeApproximately(1.5, 0.01);
        pin.NumberFontSizeHeight.ToMm().Should().BeApproximately(1.0, 0.01);
        pin.NumberFontSizeWidth.ToMm().Should().BeApproximately(0.8, 0.01);
    }

    // ───── Pin alternates ─────

    [Fact]
    public async Task RoundTrip_Pin_Alternates()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestPinAlts""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestPinAlts_0_1""
      (pin bidirectional line (at 0 0 0) (length 2.54)
        (name ""PA0"" (effects (font (size 1.27 1.27))))
        (number ""1"" (effects (font (size 1.27 1.27))))
        (alternate ""TIM2_CH1"" input line)
        (alternate ""USART2_TX"" output clock)
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestPinAlts"]!;
        var pin = sym.Pins.OfType<KiCadSchPin>().Single();

        pin.Alternates.Should().HaveCount(2);
        pin.Alternates[0].Name.Should().Be("TIM2_CH1");
        pin.Alternates[0].ElectricalType.Should().Be(PinElectricalType.Input);
        pin.Alternates[0].GraphicStyle.Should().Be(PinGraphicStyle.Line);
        pin.Alternates[1].Name.Should().Be("USART2_TX");
        pin.Alternates[1].ElectricalType.Should().Be(PinElectricalType.Output);
        pin.Alternates[1].GraphicStyle.Should().Be(PinGraphicStyle.Clock);
    }

    // ───── Filled 2-point polyline classification ─────

    [Fact]
    public async Task RoundTrip_FilledTwoPointPolyline_StaysAsPolyline()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestFilled2pt""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestFilled2pt_0_1""
      (polyline
        (pts (xy 0 0) (xy 5 5))
        (stroke (width 0.1) (type solid))
        (fill (type outline))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);
        var sym1 = lib1["TestFilled2pt"]!;

        // A filled 2-point polyline should be a polyline, not a line
        sym1.Polylines.Should().HaveCount(1);
        sym1.Lines.Should().HaveCount(0);

        // Round-trip it
        var lib2 = await RoundTrip(lib1);
        var sym2 = lib2["TestFilled2pt"]!;

        // After round-trip, fill type should be preserved
        sym2.Polylines.Should().HaveCount(1);
        var poly = sym2.Polylines.OfType<KiCadSchPolyline>().Single();
        poly.FillType.Should().Be(SchFillType.Filled);
    }

    // ───── Line round-trip preserves color and style ─────

    [Fact]
    public async Task RoundTrip_Line_StrokeColorAndStyle()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestLine""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestLine_0_1""
      (polyline
        (pts (xy 0 0) (xy 10 10))
        (stroke (width 0.2) (type dash) (color 64 128 192 1))
        (fill (type none))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestLine"]!;

        // 2-point polylines are preserved as polylines (no decomposition into lines)
        sym.Polylines.Should().HaveCount(1);
        var poly = sym.Polylines.OfType<KiCadSchPolyline>().Single();
        poly.LineWidth.ToMm().Should().BeApproximately(0.2, 0.01);
        poly.LineStyle.Should().Be(LineStyle.Dash);
        poly.Color.R.Should().Be(64);
        poly.Color.G.Should().Be(128);
        poly.Color.B.Should().Be(192);
    }

    // ───── Existing minimal.kicad_sym round-trip preserves rectangle fill type ─────

    [Fact]
    public async Task RoundTrip_MinimalFile_PreservesRectangleFillType()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "minimal.kicad_sym");
        var lib1 = await SymLibReader.ReadAsync(path);

        var lib2 = await RoundTrip(lib1);
        var sym1 = lib1["R"]!;
        var sym2 = lib2["R"]!;

        sym2.Rectangles.Should().HaveCount(sym1.Rectangles.Count);
        for (int i = 0; i < sym1.Rectangles.Count; i++)
        {
            var r1 = (KiCadSchRectangle)sym1.Rectangles[i];
            var r2 = (KiCadSchRectangle)sym2.Rectangles[i];
            r2.FillType.Should().Be(r1.FillType);
            r2.LineWidth.ToMm().Should().BeApproximately(r1.LineWidth.ToMm(), 0.01);
        }
    }

    // ───── All shape default styles round-trip as solid ─────

    [Fact]
    public async Task RoundTrip_DefaultLineStyle_PreservedAsSolid()
    {
        var lib = BuildLibWithSubSymbol("TestDefaults", sub =>
        {
            sub.AddRectangle(new KiCadSchRectangle
            {
                Corner1 = new CoordPoint(Coord.FromMm(-1), Coord.FromMm(-1)),
                Corner2 = new CoordPoint(Coord.FromMm(1), Coord.FromMm(1)),
                LineWidth = Coord.FromMm(0.1),
                // LineStyle defaults to Solid
                FillType = SchFillType.None
            });
            sub.AddCircle(new KiCadSchCircle
            {
                Center = CoordPoint.Zero,
                Radius = Coord.FromMm(5),
                LineWidth = Coord.FromMm(0.1)
            });
        });

        var lib2 = await RoundTrip(lib);
        var sym = lib2["TestDefaults"]!;

        var rect = sym.Rectangles.OfType<KiCadSchRectangle>().Single();
        rect.LineStyle.Should().Be(LineStyle.Solid);

        var circle = sym.Circles.OfType<KiCadSchCircle>().Single();
        circle.LineStyle.Should().Be(LineStyle.Solid);
    }

    // ───── Multiple parameters with different IDs ─────

    [Fact]
    public async Task RoundTrip_Parameter_MultipleWithIds()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestMultiParam""
    (in_bom yes)
    (on_board yes)
    (property ""Reference"" ""U"" (id 0) (at 0 5 0)
      (effects (font (size 1.27 1.27)))
    )
    (property ""Value"" ""IC"" (id 1) (at 0 -5 0)
      (effects (font (size 1.27 1.27)))
    )
    (property ""Footprint"" """" (id 2) (at 0 0 0)
      (effects (font (size 1.27 1.27)) (hide))
    )
    (property ""Datasheet"" """" (id 3) (at 0 0 0)
      (effects (font (size 1.27 1.27)) (hide))
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestMultiParam"]!;
        var parameters = sym.Parameters.OfType<KiCadSchParameter>().ToList();

        parameters.Should().HaveCount(4);
        parameters.First(p => p.Name == "Reference").Id.Should().Be(0);
        parameters.First(p => p.Name == "Value").Id.Should().Be(1);
        parameters.First(p => p.Name == "Footprint").Id.Should().Be(2);
        parameters.First(p => p.Name == "Datasheet").Id.Should().Be(3);
        parameters.First(p => p.Name == "Footprint").IsVisible.Should().BeFalse();
    }

    // ───── Pin with hidden name preserves font sizes ─────

    [Fact]
    public async Task RoundTrip_Pin_HiddenNamePreservesFontSizes()
    {
        var symContent = @"(kicad_symbol_lib
  (version 20231120)
  (generator ""kicadsharp"")
  (generator_version ""1.0"")
  (symbol ""TestHiddenPin""
    (in_bom yes)
    (on_board yes)
    (symbol ""TestHiddenPin_0_1""
      (pin passive line (at 0 0 0) (length 2.54)
        (name ""~"" (effects (font (size 1.27 1.27)) (hide)))
        (number ""1"" (effects (font (size 1.27 1.27))))
      )
    )
  )
)";
        using var ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(symContent));
        var lib1 = await SymLibReader.ReadAsync(ms1);

        var lib2 = await RoundTrip(lib1);
        var sym = lib2["TestHiddenPin"]!;
        var pin = sym.Pins.OfType<KiCadSchPin>().Single();

        pin.ShowName.Should().BeFalse();
        pin.ShowDesignator.Should().BeTrue();
        pin.NameFontSizeHeight.ToMm().Should().BeApproximately(1.27, 0.01);
        pin.NameFontSizeWidth.ToMm().Should().BeApproximately(1.27, 0.01);
    }
}
