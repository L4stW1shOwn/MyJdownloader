# Errors

> Package `MyJdownloader` · namespace `MyJdownloader`

Every error that the library raises for a service or device answer derives from `MyJdException`. Catching this base type is enough to handle "the MyJD call failed"; catching a subtype lets you react differently to an expired login, an offline device or a rate limit. The library performs **no network retry**: resilience (retry, backoff, circuit breaking) is the caller's responsibility, and the exception types carry the information needed to decide.

## Hierarchy

```
System.Exception
└── MyJdException                       (abstract)
    ├── MyJdApiException                (any other service or device error)
    ├── MyJdAuthenticationException     (login or session rejected)
    ├── MyJdDeviceOfflineException      (device offline or unreachable through the relay)
    ├── MyJdProtocolException           (unreadable or inconsistent response)
    └── MyJdRateLimitException          (rate limited or temporarily unavailable)
```

The five subtypes are `sealed`. Errors that are not `MyJdException` (cancellation, timeouts, network errors, argument errors) are documented in the [When each is thrown](#other-exceptions) section.

## MyJdException

`public abstract class MyJdException : Exception`

Base class of every error raised by the MyJDownloader client. The origin of the error is exposed as `ErrorSource` rather than `Source`, so `Exception.Source` is not hidden.

| Member | Signature | Description |
|---|---|---|
| `ErrorSource` | `MyJdErrorSource? ErrorSource { get; }` | The side that reported the error, or `null` when unknown. |
| `ErrorType` | `string? ErrorType { get; }` | The error type reported by the service, or `null` when unknown. Trimmed, normalized to upper case, capped at 256 characters, with unsafe characters replaced by `?`. Untrusted: encode it before displaying it. |
| `StatusCode` | `HttpStatusCode? StatusCode { get; }` | The HTTP status code of the response, or `null` when unknown. |
| constructors | `protected MyJdException()`, `protected MyJdException(string message)`, `protected MyJdException(string message, Exception innerException)`, `protected MyJdException(string message, MyJdErrorSource errorSource, string? errorType, HttpStatusCode? statusCode)`, `protected MyJdException(string message, MyJdErrorSource errorSource, string? errorType, HttpStatusCode? statusCode, Exception? innerException)` | Protected: only the subtypes below are instantiated by callers. |

**Remarks**

- `ErrorType` is the service error code, for example `TOKEN_INVALID` or `OFFLINE`. Unrecognized codes are kept in this property and are mapped to `MyJdApiException`. The exception message shows `UNKNOWN` instead of a code that is not safe to display.
- The messages never contain secrets or inner details of a decryption failure.

## MyJdApiException

`public sealed class MyJdApiException : MyJdException`

Raised when the service or the device reports an error that is not covered by a more specific type, including any error code the library does not recognize.

| Member | Signature | Description |
|---|---|---|
| constructors | `MyJdApiException()`, `MyJdApiException(string message)`, `MyJdApiException(string message, Exception innerException)`, `MyJdApiException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)` | Standard constructors; the last one carries the details reported by the service. |

## MyJdAuthenticationException

`public sealed class MyJdAuthenticationException : MyJdException`

Raised when authentication fails or the session can no longer be used. Typically the password or e-mail is wrong, the account is not confirmed, or the session could not be renewed.

| Member | Signature | Description |
|---|---|---|
| constructors | `MyJdAuthenticationException()`, `MyJdAuthenticationException(string message)`, `MyJdAuthenticationException(string message, Exception innerException)`, `MyJdAuthenticationException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)` | Standard constructors; the last one carries the details reported by the service. |

**Remarks**

- When a session renewal is rejected by the server and the rejected session is still the current one, the client disconnects (`IsConnected` becomes `false`) and raises `SessionChanged` with a `null` session before this exception reaches the caller. Ask the user to connect again, or restore a session you still hold.

## MyJdDeviceOfflineException

`public sealed class MyJdDeviceOfflineException : MyJdException`

Raised when the target device is offline or unreachable through the relay.

| Member | Signature | Description |
|---|---|---|
| constructors | `MyJdDeviceOfflineException()`, `MyJdDeviceOfflineException(string message)`, `MyJdDeviceOfflineException(string message, Exception innerException)`, `MyJdDeviceOfflineException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode)` | Standard constructors; the last one carries the details reported by the service. |

**Remarks**

- Device restart and update actions usually make the device go offline before it answers. A `MyJdDeviceOfflineException` (like a timeout) therefore does not prove the action failed: check the state of the device before repeating a destructive action.

## MyJdProtocolException

`public sealed class MyJdProtocolException : MyJdException`

Raised when a response violates the MyJDownloader protocol: malformed payload, bad signature, a response that does not match the request, or a device answer that cannot be read with the given type metadata.

| Member | Signature | Description |
|---|---|---|
| constructors | `MyJdProtocolException()`, `MyJdProtocolException(string message)`, `MyJdProtocolException(string message, Exception innerException)`, `MyJdProtocolException(string message, MyJdErrorSource errorSource)` | The last one records the side that reported the failure. `ErrorType` and `StatusCode` are `null`. |

**Remarks**

- Raised when a successful response is larger than `MyJDownloaderOptions.MaxResponseBodyLength` (1 MiB by default), with the message "The MyJD server response exceeds the configured size limit." for server calls or "The MyJD device response exceeds the configured size limit." for device calls and binary answers, when its body cannot be decrypted or deserialized, when a device answer's request id does not match the request, or when a device answer cannot be read with the response type given by the caller. An oversized body on a non-success response does not raise this exception: the status is mapped as an error of unknown type (`MyJdApiException`, `ErrorType` `UNKNOWN`).
- The library never attaches the inner detail of a decryption or protocol failure, to avoid exposing a padding oracle. Treat this exception as "the response cannot be trusted", not as a diagnostic.
- Usually a bug or a version mismatch, not a transient condition: retrying rarely helps.

## MyJdRateLimitException

`public sealed class MyJdRateLimitException : MyJdException`

Raised when the service rejects a request because of rate limiting or because it is temporarily overloaded or under maintenance.

| Member | Signature | Description |
|---|---|---|
| `RetryAfter` | `TimeSpan? RetryAfter { get; }` | The delay after which the request may be retried, or `null` when the service did not provide one. |
| constructors | `MyJdRateLimitException()`, `MyJdRateLimitException(string message)`, `MyJdRateLimitException(string message, Exception innerException)`, `MyJdRateLimitException(string message, MyJdErrorSource errorSource, string errorType, HttpStatusCode statusCode, TimeSpan? retryAfter)` | The last one carries the details reported by the service and the retry delay. |

**Remarks**

- The library does not retry this exception. Wait for `RetryAfter` when it is provided, then decide whether to call again.
- `RetryAfter` comes from the HTTP `Retry-After` header (a delay or a date). It is never negative and is capped at 10 minutes. When it is `null`, the service gave no delay and the caller must choose one, for example with backoff.

## MyJdErrorSource

`public enum MyJdErrorSource`

Identifies which side of the MyJDownloader relay reported an error.

| Member | Signature | Description |
|---|---|---|
| `Server` | `Server = 0` | The error was reported by the MyJDownloader server (the relay). |
| `Device` | `Device = 1` | The error was reported by the target JDownloader device. |

## When each is thrown

| Exception | Thrown when the answer carries one of these error types | Typical operation |
|---|---|---|
| `MyJdAuthenticationException` | `AUTH_FAILED`, `EMAIL_INVALID`, `EMAIL_FORBIDDEN`, `TOKEN_INVALID`, `ERROR_EMAIL_NOT_CONFIRMED` | `ConnectAsync` with rejected credentials; any call whose session renewal is rejected |
| `MyJdDeviceOfflineException` | `OFFLINE` | Any device call (`IDeviceChannel`, facades) when the device is unreachable |
| `MyJdRateLimitException` | `TOO_MANY_REQUESTS`, `OVERLOAD`, `MAINTENANCE` | Any call the server throttles or refuses while busy |
| `MyJdProtocolException` | No error type: the response is malformed, its signature is wrong, or it does not match the request | Any call; also a misconfigured response type on `IDeviceChannel.CallAsync` |
| `MyJdApiException` | Any other error type, including unrecognized ones | Any call |

The mapping is applied to the `src` and `type` fields of the error payload. A missing or unrecognized `src` falls back to the side that carried the answer.

### Other exceptions

Not every failure is a `MyJdException`. The following are raised as they are:

| Exception | Meaning |
|---|---|
| `ArgumentException`, `ArgumentNullException` | Invalid argument (for example an invalid device id, path or option). Synchronous. |
| `InvalidOperationException` | The client is not connected, and the operation needs a session. |
| `OperationCanceledException` | The caller cancelled the operation. |
| `TimeoutException` | The request timed out. For device actions the action may have been executed. |
| `HttpRequestException` | A network error occurred. For device actions the action may have been executed. |
| `IOException` | The response body could not be read. |
| `AggregateException` | Several `SessionChanged` handlers threw. A single handler failure is rethrown as is. |
| `FormatException` | `MyJdSession.FromJson` received data that is not a valid session. The message contains no value from the input. |

## Retry and resilience

The library never retries a failed call on its own, for three reasons: a device action may already have run when a timeout or a network error occurs, a retry hides a protocol problem, and the right policy depends on the application. Build your policy around the types above:

- retry only idempotent operations after `TimeoutException`, `HttpRequestException` or `MyJdDeviceOfflineException`, and only after checking the device state for destructive actions;
- honor `MyJdRateLimitException.RetryAfter` when it is provided;
- do not retry `MyJdAuthenticationException` with the same session: connect again or restore a valid session.

### Example

```csharp
using Microsoft.Extensions.Logging;
using MyJdownloader;

try
{
    IReadOnlyList<DeviceInfo> devices = await client.GetDevicesAsync(cancellationToken);
}
catch (MyJdRateLimitException ex) when (ex.RetryAfter is not null)
{
    // The library never retries: wait as the server asked, then decide to call again.
    await Task.Delay(ex.RetryAfter.Value, cancellationToken);
}
catch (MyJdAuthenticationException)
{
    // The session can no longer be used: ask the user to connect again.
}
catch (MyJdException ex)
{
    // ErrorType is untrusted: it is logged as a template argument, never interpolated.
    logger.LogWarning("MyJD call failed with {ErrorType} (HTTP {StatusCode}).", ex.ErrorType, ex.StatusCode);
}
```

## See also

- [Client API](client.md): the methods that throw these exceptions
- [Device channel](device-channel.md): the generic call and its exceptions
- [Common types](common-types.md)
- [Getting started](../guides/getting-started.md)
- [Error handling](../guides/error-handling.md)
- [Sessions and authentication](../guides/sessions-and-authentication.md)
- [ADR-005: error handling](../adr/ADR-005-error-handling.md)
