namespace OriginalCircuit.KiCad;

/// <summary>
/// Exception thrown when a KiCad file cannot be parsed or contains invalid format data.
/// </summary>
public class KiCadFileException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadFileException"/> class.
    /// </summary>
    public KiCadFileException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadFileException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public KiCadFileException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadFileException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public KiCadFileException(string message, Exception innerException) : base(message, innerException) { }
}
