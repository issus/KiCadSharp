namespace OriginalCircuit.KiCad;

/// <summary>
/// Exception thrown when a KiCad file cannot be parsed or contains invalid format data.
/// </summary>
public class KiCadFileException : Exception
{
    /// <summary>
    /// Gets the file path associated with this error, if available.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the byte position in the file where the error occurred, if available.
    /// </summary>
    public int? Position { get; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="KiCadFileException"/> class with file context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The file path where the error occurred.</param>
    /// <param name="position">The byte position where the error occurred.</param>
    /// <param name="innerException">The inner exception.</param>
    public KiCadFileException(string message, string? filePath, int? position = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
        Position = position;
    }
}
