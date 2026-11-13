namespace MyJdownloader.Errors;

/// <summary>
/// Raised when a response violates the MyJDownloader protocol (malformed payload, bad signature, and so on).
/// </summary>
/// <remarks>
/// It is raised when a response cannot be read or does not match the request: a successful response
/// larger than <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/>, a body that cannot be decrypted or deserialized, a device answer whose request id does not match the
/// request, or a device answer that cannot be read with the response type given by the caller. It carries no service error
/// type and no HTTP status code. Treat it as "the response cannot be trusted", usually a bug or a version mismatch: retrying rarely helps.
/// The library itself never attaches the inner detail of a decryption or protocol failure, to avoid
/// exposing a padding oracle. The standard constructors exist for callers (for example tests).
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
