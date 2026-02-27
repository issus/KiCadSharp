using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.KiCad.Models.Pcb;
using OriginalCircuit.KiCad.SExpression;
using SExpr = OriginalCircuit.KiCad.SExpression.SExpression;

namespace OriginalCircuit.KiCad.Serialization;

/// <summary>
/// Writes KiCad PCB files (<c>.kicad_pcb</c>) from <see cref="KiCadPcb"/> objects.
/// </summary>
public static class PcbWriter
{
    /// <summary>
    /// Writes a PCB document to a file path.
    /// </summary>
    /// <param name="pcb">The PCB document to write.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcb pcb, string path, CancellationToken ct = default)
    {
        var expr = Build(pcb);
        await SExpressionWriter.WriteAsync(expr, path, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PCB document to a stream.
    /// </summary>
    /// <param name="pcb">The PCB document to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async ValueTask WriteAsync(KiCadPcb pcb, Stream stream, CancellationToken ct = default)
    {
        var expr = Build(pcb);
        await SExpressionWriter.WriteAsync(expr, stream, ct).ConfigureAwait(false);
    }

    private static SExpr Build(KiCadPcb pcb)
    {
        var b = new SExpressionBuilder("kicad_pcb")
            .AddChild("version", v => v.AddValue(pcb.Version == 0 ? 20231014 : pcb.Version))
            .AddChild("generator", g => g.AddValue(pcb.Generator ?? "kicadsharp"))
            .AddChild("generator_version", g => g.AddValue(pcb.GeneratorVersion ?? "1.0"));

        // General
        b.AddChild("general", gen =>
        {
            gen.AddChild("thickness", t => t.AddValue(pcb.BoardThickness.ToMm()));
        });

        // Nets
        foreach (var (num, name) in pcb.Nets)
        {
            b.AddChild("net", n =>
            {
                n.AddValue(num);
                n.AddValue(name);
            });
        }

        // Footprints
        foreach (var comp in pcb.Components.OfType<KiCadPcbComponent>())
        {
            b.AddChild(FootprintWriter.BuildFootprint(comp));
        }

        // Tracks
        foreach (var track in pcb.Tracks.OfType<KiCadPcbTrack>())
        {
            b.AddChild(BuildSegment(track));
        }

        // Vias
        foreach (var via in pcb.Vias.OfType<KiCadPcbVia>())
        {
            b.AddChild(BuildVia(via));
        }

        // Arcs
        foreach (var arc in pcb.Arcs.OfType<KiCadPcbArc>())
        {
            b.AddChild(BuildArc(arc));
        }

        // Texts
        foreach (var text in pcb.Texts.OfType<KiCadPcbText>())
        {
            b.AddChild(BuildGrText(text));
        }

        // Zones (from regions)
        foreach (var region in pcb.Regions.OfType<KiCadPcbRegion>())
        {
            b.AddChild(BuildZone(region));
        }

        return b.Build();
    }

    private static SExpr BuildSegment(KiCadPcbTrack track)
    {
        var sb = new SExpressionBuilder("segment")
            .AddChild("start", s => { s.AddValue(track.Start.X.ToMm()); s.AddValue(track.Start.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(track.End.X.ToMm()); e.AddValue(track.End.Y.ToMm()); })
            .AddChild("width", w => w.AddValue(track.Width.ToMm()));

        if (track.LayerName is not null)
            sb.AddChild("layer", l => l.AddSymbol(track.LayerName));

        sb.AddChild("net", n => n.AddValue(track.Net));

        if (track.Uuid is not null)
            sb.AddChild(WriterHelper.BuildUuid(track.Uuid));

        return sb.Build();
    }

    private static SExpr BuildVia(KiCadPcbVia via)
    {
        var vb = new SExpressionBuilder("via");

        if (via.ViaType == ViaType.BlindBuried)
            vb.AddSymbol("blind");
        else if (via.ViaType == ViaType.Micro)
            vb.AddSymbol("micro");

        if (via.IsLocked)
            vb.AddSymbol("locked");

        if (via.IsFree)
            vb.AddChild("free", _ => { });
        if (via.RemoveUnusedLayers)
            vb.AddChild("remove_unused_layers", _ => { });
        if (via.KeepEndLayers)
            vb.AddChild("keep_end_layers", _ => { });

        vb.AddChild(WriterHelper.BuildPosition(via.Location))
          .AddChild("size", s => s.AddValue(via.Diameter.ToMm()))
          .AddChild("drill", d => d.AddValue(via.HoleSize.ToMm()));

        if (via.StartLayerName is not null && via.EndLayerName is not null)
        {
            vb.AddChild("layers", l =>
            {
                l.AddValue(via.StartLayerName);
                l.AddValue(via.EndLayerName);
            });
        }

        vb.AddChild("net", n => n.AddValue(via.Net));

        if (via.Uuid is not null)
            vb.AddChild(WriterHelper.BuildUuid(via.Uuid));

        return vb.Build();
    }

    private static SExpr BuildArc(KiCadPcbArc arc)
    {
        var ab = new SExpressionBuilder("arc")
            .AddChild("start", s => { s.AddValue(arc.ArcStart.X.ToMm()); s.AddValue(arc.ArcStart.Y.ToMm()); })
            .AddChild("mid", m => { m.AddValue(arc.ArcMid.X.ToMm()); m.AddValue(arc.ArcMid.Y.ToMm()); })
            .AddChild("end", e => { e.AddValue(arc.ArcEnd.X.ToMm()); e.AddValue(arc.ArcEnd.Y.ToMm()); })
            .AddChild("width", w => w.AddValue(arc.Width.ToMm()));

        if (arc.LayerName is not null)
            ab.AddChild("layer", l => l.AddSymbol(arc.LayerName));

        ab.AddChild("net", n => n.AddValue(arc.Net));

        if (arc.Uuid is not null)
            ab.AddChild(WriterHelper.BuildUuid(arc.Uuid));

        return ab.Build();
    }

    private static SExpr BuildGrText(KiCadPcbText text)
    {
        var tb = new SExpressionBuilder("gr_text")
            .AddValue(text.Text)
            .AddChild(WriterHelper.BuildPosition(text.Location, text.Rotation));

        if (text.LayerName is not null)
            tb.AddChild("layer", l => l.AddSymbol(text.LayerName));

        tb.AddChild(WriterHelper.BuildTextEffects(text.Height, text.Height, isBold: text.FontBold, isItalic: text.FontItalic));

        if (text.Uuid is not null)
            tb.AddChild(WriterHelper.BuildUuid(text.Uuid));

        return tb.Build();
    }

    private static SExpr BuildZone(KiCadPcbRegion region)
    {
        var zb = new SExpressionBuilder("zone")
            .AddChild("net", n => n.AddValue(region.Net));

        if (region.NetName is not null)
            zb.AddChild("net_name", n => n.AddValue(region.NetName));

        if (region.LayerName is not null)
            zb.AddChild("layer", l => l.AddSymbol(region.LayerName));

        if (region.Priority > 0)
            zb.AddChild("priority", p => p.AddValue(region.Priority));

        if (region.Uuid is not null)
            zb.AddChild(WriterHelper.BuildUuid(region.Uuid));

        zb.AddChild("polygon", p =>
        {
            p.AddChild(WriterHelper.BuildPoints(region.Outline));
        });

        return zb.Build();
    }
}
