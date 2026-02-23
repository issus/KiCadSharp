namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad footprint attribute flags.
/// </summary>
[Flags]
public enum FootprintAttribute
{
    /// <summary>No special attributes.</summary>
    None = 0,

    /// <summary>Surface-mount device.</summary>
    Smd = 1,

    /// <summary>Through-hole device.</summary>
    ThroughHole = 2,

    /// <summary>Board-only (not in BOM or position files).</summary>
    BoardOnly = 4,

    /// <summary>Exclude from position files.</summary>
    ExcludeFromPosFiles = 8,

    /// <summary>Exclude from BOM.</summary>
    ExcludeFromBom = 16
}
