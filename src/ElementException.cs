using System;

namespace Promptuarium;

/// <summary>
/// Base class for all Promptuarium exceptions
/// </summary>
public class PromptuariumException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptuariumException"/> class.
    /// </summary>
    public PromptuariumException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptuariumException"/> class.
    /// </summary>
    /// <param name="message">The exception message</param>

    public PromptuariumException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptuariumException"/> class.
    /// </summary>
    /// <param name="message">The exception message</param>
    /// <param name="innerException">The inner exception</param>
    public PromptuariumException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
