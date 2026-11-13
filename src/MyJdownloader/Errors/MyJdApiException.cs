namespace MyJdownloader.Errors;

using System.Net;

/// <summary>Raised when the service or the device reports an error not covered by a more specific type.</summary>
/// <remarks>
/// This is the fallback of the mapping: any error type that does not select <see cref="MyJdAuthenticationException"/>,
/// <see cref="MyJdDeviceOfflineException"/>, <see cref="MyJdRateLimitException"/> or <see cref="MyJdProtocolException"/>,
/// including unrecognized types. The error type is kept in <see cref="MyJdException.ErrorType"/>, but the message shows
/// <c>UNKNOWN</c> when the type is not safe to display. No retry delay or recovery hint is attached: decide from
/// <see cref="MyJdException.ErrorType"/> and <see cref="MyJdException.StatusCode"/>.
/// </remarks>
public sealed class MyJdApiException : MyJdException
{
    /// <summary>Initializes a new instance of the <see cref="MyJdApiException"/> class.</summary>
    public MyJdApiException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdApiException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public MyJdApiException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdApiException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public MyJdApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdApiException"/> class with the error details reported by the service.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public MyJdApiException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)
        : base(message, errorSource, errorType, statusCode)
    {
    }
}
