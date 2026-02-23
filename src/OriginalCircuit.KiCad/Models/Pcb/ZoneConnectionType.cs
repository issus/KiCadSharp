namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad zone connection type for pads.
/// </summary>
public enum ZoneConnectionType
{
    /// <summary>Inherited from parent zone or footprint.</summary>
    Inherited = 0,

    /// <summary>No connection to zone.</summary>
    None = 1,

    /// <summary>Thermal relief connection.</summary>
    ThermalRelief = 2,

    /// <summary>Solid connection.</summary>
    Solid = 3
}
