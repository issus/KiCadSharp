namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad-specific via type.
/// </summary>
public enum ViaType
{
    /// <summary>Standard through via.</summary>
    Through = 0,

    /// <summary>Blind or buried via.</summary>
    BlindBuried = 1,

    /// <summary>Micro via.</summary>
    Micro = 2
}
