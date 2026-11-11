namespace MyJdownloader;

using System.Net;

/// <summary>
/// Base class of every error raised by the MyJDownloader client.
/// </summary>
/// <remarks>
/// The origin of the error is exposed as <see cref="ErrorSource"/> rather than <c>Source</c>
/// to avoid hiding <see cref="Exception.Source"/>.
/// </remarks>
public abstract class MyJdException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="MyJdException"/> class.</summary>
    protected MyJdException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    protected MyJdException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    protected MyJdException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdException"/> class with the error details reported by the service.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service, if any.</param>
    /// <param name="statusCode">The HTTP status code of the response, if any.</param>
    protected MyJdException(
        string message,
        MyJdErrorSource errorSource,
        string? errorType,
        HttpStatusCode? statusCode)
        : this(message, errorSource, errorType, statusCode, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdException"/> class with the error details reported by the service and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service, if any.</param>
    /// <param name="statusCode">The HTTP status code of the response, if any.</param>
    /// <param name="innerException">The exception that caused this error, if any.</param>
    protected MyJdException(
        string message,
        MyJdErrorSource errorSource,
        string? errorType,
        HttpStatusCode? statusCode,
        Exception? innerException)
        : base(message, innerException)
    {
        ErrorSource = errorSource;
        ErrorType = errorType;
        StatusCode = statusCode;
    }

    /// <summary>Gets the side that reported the error, or <see langword="null"/> when unknown.</summary>
    public MyJdErrorSource? ErrorSource { get; }

    /// <summary>Gets the error type reported by the service, or <see langword="null"/> when unknown.</summary>
    /// <remarks>
    /// The value comes from the remote service: it is trimmed, capped at 256 characters and normalized to
    /// upper case; control, format (bidirectional overrides, zero-width, tags), line and paragraph separator
    /// characters and unpaired surrogates are replaced with <c>?</c>. It is still untrusted (markup characters
    /// such as <c>&lt;</c> are kept): encode it before displaying it.
    /// </remarks>
    public string? ErrorType { get; }

    /// <summary>Gets the HTTP status code of the response, or <see langword="null"/> when unknown.</summary>
    public HttpStatusCode? StatusCode { get; }
}
