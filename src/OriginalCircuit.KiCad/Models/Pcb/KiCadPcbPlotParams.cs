namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB plot parameters, mapped from <c>(pcbplotparams ...)</c> inside setup.
/// All properties stored as key-value pairs to handle the many (~35) parameters
/// without requiring individual Has* flags for each one.
/// </summary>
public sealed class KiCadPcbPlotParams
{
    /// <summary>
    /// Gets or sets the plot parameters as ordered key-value-isSymbol tuples.
    /// Keys are the token names, values are the string representations.
    /// IsSymbol indicates whether the value was a bare symbol (unquoted) in the source.
    /// Stored in order to preserve round-trip fidelity.
    /// </summary>
    public List<(string Key, string Value, bool IsSymbol)> Parameters { get; set; } = [];
}
