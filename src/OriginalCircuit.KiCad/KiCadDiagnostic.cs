using OriginalCircuit.Eda.Enums;

namespace OriginalCircuit.KiCad;

/// <summary>
/// Represents a non-fatal diagnostic message produced during parsing or validation of a KiCad file.
/// </summary>
/// <param name="Severity">The severity level of the diagnostic.</param>
/// <param name="Message">The diagnostic message text.</param>
/// <param name="Context">Optional context information (e.g., the S-expression token path).</param>
public sealed record KiCadDiagnostic(DiagnosticSeverity Severity, string Message, string? Context = null);
