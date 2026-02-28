using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB net class definition, mapped from <c>(net_class NAME "DESC" (clearance ...) ...)</c>.
/// </summary>
public sealed class KiCadPcbNetClass
{
    /// <summary>Gets or sets the net class name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = "";

    /// <summary>Gets or sets the clearance.</summary>
    public Coord Clearance { get; set; }

    /// <summary>Gets or sets the trace width.</summary>
    public Coord TraceWidth { get; set; }

    /// <summary>Gets or sets the via diameter.</summary>
    public Coord ViaDia { get; set; }

    /// <summary>Gets or sets the via drill size.</summary>
    public Coord ViaDrill { get; set; }

    /// <summary>Gets or sets the micro via diameter.</summary>
    public Coord UViaDia { get; set; }

    /// <summary>Gets or sets the micro via drill size.</summary>
    public Coord UViaDrill { get; set; }

    /// <summary>Gets or sets the differential pair width.</summary>
    public Coord DiffPairWidth { get; set; }

    /// <summary>Gets or sets the differential pair gap.</summary>
    public Coord DiffPairGap { get; set; }

    /// <summary>Gets or sets the bus width.</summary>
    public Coord BusWidth { get; set; }

    /// <summary>Gets or sets whether the clearance value was explicitly present in the source file.</summary>
    public bool HasClearance { get; set; }

    /// <summary>Gets or sets whether the trace width value was explicitly present in the source file.</summary>
    public bool HasTraceWidth { get; set; }

    /// <summary>Gets or sets whether the via diameter value was explicitly present in the source file.</summary>
    public bool HasViaDia { get; set; }

    /// <summary>Gets or sets whether the via drill value was explicitly present in the source file.</summary>
    public bool HasViaDrill { get; set; }

    /// <summary>Gets or sets whether the micro via diameter value was explicitly present in the source file.</summary>
    public bool HasUViaDia { get; set; }

    /// <summary>Gets or sets whether the micro via drill value was explicitly present in the source file.</summary>
    public bool HasUViaDrill { get; set; }

    /// <summary>Gets or sets whether the differential pair width value was explicitly present in the source file.</summary>
    public bool HasDiffPairWidth { get; set; }

    /// <summary>Gets or sets whether the differential pair gap value was explicitly present in the source file.</summary>
    public bool HasDiffPairGap { get; set; }

    /// <summary>Gets or sets whether the bus width value was explicitly present in the source file.</summary>
    public bool HasBusWidth { get; set; }

    /// <summary>Gets or sets the net names belonging to this class.</summary>
    public List<string> NetNames { get; set; } = [];
}
