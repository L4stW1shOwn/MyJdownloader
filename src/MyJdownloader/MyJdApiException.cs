namespace MyJdownloader;

using System.Net;

/// <summary>Raised when the service or the device reports an error not covered by a more specific type.</summary>
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
