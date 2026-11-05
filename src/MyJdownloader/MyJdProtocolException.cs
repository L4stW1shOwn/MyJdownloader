namespace MyJdownloader;

/// <summary>
/// Raised when a response violates the MyJDownloader protocol (malformed payload, bad signature, and so on).
/// </summary>
/// <remarks>
/// The inner detail of protocol failures is deliberately not attached, to avoid leaking
/// cryptographic or payload material through the exception chain.
/// </remarks>
public sealed class MyJdProtocolException : MyJdException
{
    /// <summary>Initializes a new instance of the <see cref="MyJdProtocolException"/> class.</summary>
    public MyJdProtocolException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdProtocolException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public MyJdProtocolException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdProtocolException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public MyJdProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdProtocolException"/> class with the side that reported the failure.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    public MyJdProtocolException(string message, MyJdErrorSource errorSource)
        : base(message, errorSource, null, null)
    {
    }
}
