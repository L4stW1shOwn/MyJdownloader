namespace MyJdownloader.Errors;

using System.Net;

/// <summary>Raised when the target device is offline or unreachable through the relay.</summary>
/// <remarks>
/// Raised for the error type <c>OFFLINE</c>. Restart and update actions usually take the device offline before it answers,
/// so, like a timeout, this exception does not prove that the action failed: the action may have been executed. Check the
/// device state before repeating a destructive action. The library never retries.
/// </remarks>
public sealed class MyJdDeviceOfflineException : MyJdException
{
    /// <summary>Initializes a new instance of the <see cref="MyJdDeviceOfflineException"/> class.</summary>
    public MyJdDeviceOfflineException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdDeviceOfflineException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public MyJdDeviceOfflineException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdDeviceOfflineException"/> class with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public MyJdDeviceOfflineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MyJdDeviceOfflineException"/> class with the error details reported by the service.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorSource">The side that reported the error.</param>
    /// <param name="errorType">The error type reported by the service.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public MyJdDeviceOfflineException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)
        : base(message, errorSource, errorType, statusCode)
    {
    }
}
