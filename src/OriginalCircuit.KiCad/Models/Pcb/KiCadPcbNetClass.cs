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

    /// <summary>Gets or sets the net names belonging to this class.</summary>
    public List<string> NetNames { get; set; } = [];
}
