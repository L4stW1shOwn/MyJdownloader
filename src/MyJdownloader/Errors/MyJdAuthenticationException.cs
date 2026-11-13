namespace MyJdownloader.Errors;

using System.Net;

/// <summary>Raised when authentication fails or the session can no longer be used.</summary>
/// <remarks>
/// Raised for the error types <c>AUTH_FAILED</c>, <c>EMAIL_INVALID</c>, <c>EMAIL_FORBIDDEN</c>, <c>TOKEN_INVALID</c> and
/// <c>ERROR_EMAIL_NOT_CONFIRMED</c>, whichever side reported them. It is thrown by <see cref="MyJDownloaderClient.ConnectAsync"/>
/// when the credentials are rejected, and by a call whose automatic session renewal is rejected. In that second case, if the
/// rejected session is still the current one, the client has already dropped it (<see cref="MyJDownloaderClient.IsConnected"/>
/// is <see langword="false"/>). Do not retry with the same session: connect again with the account credentials, or restore a
/// session that is still valid.
/// </remarks>
public sealed class MyJdAuthenticationException : MyJdException
{
    /// <summary>Initializes a new instance of the <see cref="MyJdAuthenticationException"/> class.</summary>
    public MyJdAuthenticationException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdAuthenticationException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public MyJdAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdAuthenticationException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public MyJdAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdAuthenticationException"/> class with the error details reported by the service.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public MyJdAuthenticationException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)
        : base(message, errorSource, errorType, statusCode)
    {
    }
}
