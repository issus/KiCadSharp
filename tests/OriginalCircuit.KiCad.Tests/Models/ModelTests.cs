using Xunit;
using FluentAssertions;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.Models.Sch;

namespace OriginalCircuit.KiCad.Tests.Models;

public class ModelTests
{
    [Fact]
    public void KiCadPcbTrack_Bounds_IncludesWidth()
    {
        var track = new KiCadPcbTrack
        {
            Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
            End = new CoordPoint(Coord.FromMm(10), Coord.FromMm(0)),
            Width = Coord.FromMm(2)
        };

        var bounds = track.Bounds;

        // Width is 2mm, so bounds should extend 1mm above and below
        bounds.Min.Y.ToMm().Should().BeApproximately(-1, 0.01);
        bounds.Max.Y.ToMm().Should().BeApproximately(1, 0.01);
        bounds.Min.X.ToMm().Should().BeApproximately(-1, 0.01);
        bounds.Max.X.ToMm().Should().BeApproximately(11, 0.01);
    }

    [Fact]
    public void KiCadSchComponent_AddPin_NullThrowsArgumentNullException()
    {
        var component = new KiCadSchComponent();
        var act = () => component.AddPin(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadSchComponent_AddSubSymbol_NullThrowsArgumentNullException()
    {
        var component = new KiCadSchComponent();
        var act = () => component.AddSubSymbol(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadPcb_AddComponent_NullThrowsArgumentNullException()
    {
        var pcb = new KiCadPcb();
        var act = () => pcb.AddComponent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadSch_AddWire_NullThrowsArgumentNullException()
    {
        var sch = new KiCadSch();
        var act = () => sch.AddWire(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadSymLib_Add_NullThrowsArgumentNullException()
    {
        var lib = new KiCadSymLib();
        var act = () => lib.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadPcbComponent_AddPad_NullThrowsArgumentNullException()
    {
        var component = new KiCadPcbComponent();
        var act = () => component.AddPad(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KiCadSchComponent_HasErrors_WhenNoErrors_ReturnsFalse()
    {
        var component = new KiCadSchComponent();
        component.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void KiCadSch_HasErrors_WhenNoErrors_ReturnsFalse()
    {
        var sch = new KiCadSch();
        sch.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void KiCadPcb_HasErrors_WhenNoErrors_ReturnsFalse()
    {
        var pcb = new KiCadPcb();
        pcb.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void KiCadPcbText_Bounds_EstimatesTextExtent()
    {
        var text = new KiCadPcbText
        {
            Text = "Hello",
            Location = new CoordPoint(Coord.FromMm(10), Coord.FromMm(20)),
            Height = Coord.FromMm(1.27)
        };

        var bounds = text.Bounds;

        // Bounds should not be a zero-size point
        var width = bounds.Max.X.ToMm() - bounds.Min.X.ToMm();
        var height = bounds.Max.Y.ToMm() - bounds.Min.Y.ToMm();
        width.Should().BeGreaterThan(0);
        height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void KiCadSchParameter_Bounds_EstimatesTextExtent()
    {
        var param = new KiCadSchParameter
        {
            Value = "R1",
            Location = new CoordPoint(Coord.FromMm(5), Coord.FromMm(10)),
            FontSizeWidth = Coord.FromMm(1.27),
            FontSizeHeight = Coord.FromMm(1.27)
        };

        var bounds = param.Bounds;
        var width = bounds.Max.X.ToMm() - bounds.Min.X.ToMm();
        width.Should().BeGreaterThan(0);
    }

    [Fact]
    public void KiCadPcbComponent_Model3D_DefaultScale()
    {
        var component = new KiCadPcbComponent();
        // Default scale should be 1,1
        component.Model3DScale.X.ToMm().Should().BeApproximately(1.0, 0.01);
        component.Model3DScale.Y.ToMm().Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void KiCadSchComponent_LocationAndRotation_DefaultsToZero()
    {
        var component = new KiCadSchComponent();
        component.Location.X.ToMm().Should().Be(0);
        component.Location.Y.ToMm().Should().Be(0);
        component.Rotation.Should().Be(0);
        component.IsMirroredX.Should().BeFalse();
        component.IsMirroredY.Should().BeFalse();
    }
}
