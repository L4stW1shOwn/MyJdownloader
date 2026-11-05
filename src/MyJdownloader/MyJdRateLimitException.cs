namespace MyJdownloader;

using System.Net;

/// <summary>Raised when the service rejects a request because of rate limiting.</summary>
public sealed class MyJdRateLimitException : MyJdException
{
    /// <summary>Initializes a new instance of the <see cref="MyJdRateLimitException"/> class.</summary>
    public MyJdRateLimitException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdRateLimitException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public MyJdRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdRateLimitException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public MyJdRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdRateLimitException"/> class with the error details reported by the service.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="retryAfter">The delay after which the request may be retried, if the service provided one.</param>
    public MyJdRateLimitException(
        string message,
        MyJdErrorSource errorSource,
        string errorType,
        HttpStatusCode statusCode,
        TimeSpan? retryAfter)
        : base(message, errorSource, errorType, statusCode) => this.RetryAfter = retryAfter;

    /// <summary>Gets the delay after which the request may be retried, or <see langword="null"/> when not provided.</summary>
    public TimeSpan? RetryAfter { get; }
}
