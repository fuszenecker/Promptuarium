using System;

namespace Promptuarium;

public class PromptuariumException : Exception
{
    public PromptuariumException()
    {
    }

    public PromptuariumException(string message)
        : base(message)
    {
    }

    public PromptuariumException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
