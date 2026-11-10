# Error handling

Errors raised by the relay or by the device derive from `MyJdException`, which
lets you handle the service errors in one place. Network errors, timeouts and
cancellation keep their standard .NET types, so standard resilience policies
keep working on them
([ADR-005](../adr/ADR-005-error-handling.md)). The library never retries a call.

## The exception hierarchy

```text
Exception
├── MyJdException                       (abstract base: every service error)
│   ├── MyJdApiException                (any error not covered below)
│   ├── MyJdAuthenticationException     (credentials or session rejected)
│   ├── MyJdDeviceOfflineException      (the device is offline)
│   ├── MyJdRateLimitException          (rate limited, overloaded, maintenance)
│   └── MyJdProtocolException           (answer missing, unreadable or too large)
```

Each `MyJdException` exposes:

| Member         | Type                  | Meaning                                                                   |
|----------------|-----------------------|---------------------------------------------------------------------------|
| `ErrorSource`  | `MyJdErrorSource?`    | `Server` (the relay) or `Device` (the JDownloader instance).             |
| `ErrorType`    | `string?`             | The error code reported by the service, for example `TOKEN_INVALID`. Untrusted text. |
| `StatusCode`   | `HttpStatusCode?`     | The HTTP status of the answer, when known.                               |
| `RetryAfter`   | `TimeSpan?`           | On `MyJdRateLimitException` only: the delay the service asked for, if any. |

`ErrorType` is capped at 256 characters and normalized, but it still comes from the
remote service. Encode it before you display it.

## Which error maps to which type

The error code decides the type:

| Exception                          | Error codes                                                                 |
|------------------------------------|-----------------------------------------------------------------------------|
| `MyJdAuthenticationException`      | `AUTH_FAILED`, `EMAIL_INVALID`, `EMAIL_FORBIDDEN`, `TOKEN_INVALID`, `ERROR_EMAIL_NOT_CONFIRMED` |
| `MyJdDeviceOfflineException`       | `OFFLINE`                                                                   |
| `MyJdRateLimitException`           | `TOO_MANY_REQUESTS`, `OVERLOAD`, `MAINTENANCE`                              |
| `MyJdApiException`                 | Any other code, including `NOT_AVAILABLE` (for example an unknown captcha id). |
| `MyJdProtocolException`            | No service code: the answer is missing, unreadable, does not match the request, or is over 1 MiB. |

`TOKEN_INVALID` is normally handled by the client itself: it renews the session
once and replays the call. You see it as a `MyJdAuthenticationException` only when
the renewal was rejected. Then, connect again with the password
([Sessions and authentication](sessions-and-authentication.md)).

## Other exceptions you will see

| Exception                              | When                                                                  |
|----------------------------------------|-----------------------------------------------------------------------|
| `ArgumentException`, `ArgumentNullException` | An invalid argument. Thrown synchronously, before the task exists.   |
| `InvalidOperationException`            | The client is not connected.                                          |
| `OperationCanceledException`           | The `CancellationToken` was cancelled.                                |
| `TimeoutException`                     | No complete answer within `HttpClient.Timeout`.                       |
| `HttpRequestException`                 | Network error.                                                        |
| `IOException`                          | The response body could not be read.                                  |
| `AggregateException`                   | Several `SessionChanged` handlers threw.                              |

## Argument validation

The facades check the strings you pass before they send anything. A text value
must not be blank, must not contain control characters or lone surrogates, and
must not exceed the maximum length of its category. Optional texts are checked only
when you give them. A bad value throws an
`ArgumentException` that names the argument (`ParamName`). It is thrown
synchronously, before the task exists and before any request, so nothing
reaches the relay or the device.

The length is `string.Length`, that is a count of UTF-16 code units, not of
visible characters. An emoji counts as 2.

| Category | Maximum length | Examples |
|---|---|---|
| Identifiers | 1 024 | Config keys and interface names, types, file, package, hoster and user names, formats, host names, site keys, archive and extension ids |
| File system paths | 32 767 | Download directories and paths, extraction paths, link grabber destination folders, config storage files |
| Long text | 65 536 | URLs, captcha results and tokens, passwords, regular expressions, event subscription patterns, free text |

On top of these limits, every device call, whatever the facade, rejects
more than 1 024 arguments, or arguments that total more than 1 048 576 UTF-16
code units. A non-string argument counts its JSON text (escaped form), and each
argument adds 3 for the quotes and separator. The cap bounds the request but is
not the HTTP body size, which escaping and encryption expand. This covers lists
(link text, id arrays) and direct `IDeviceChannel.CallAsync` calls. It throws an
`ArgumentException` on `args`, before encryption and any HTTP request.

```csharp
try
{
    await device.Captcha.SolveAsync(jobId, tooLongResult, cancellationToken: cancellationToken);
}
catch (ArgumentException ex)
{
    logger.LogWarning("Invalid argument {Argument}", ex.ParamName);
}
```

## What to catch

Catch the most specific type you can act on. Do not swallow the base type
silently.

```csharp
using System.Net;
using MyJdownloader;

try
{
    await device.Config.SetAsync(entry.InterfaceName!, entry.Storage, entry.Key!, value, cancellationToken);
}
catch (MyJdRateLimitException ex) when (ex.RetryAfter is { } wait)
{
    logger.LogWarning("Rate limited, retry in {Seconds} s", wait.TotalSeconds);
    // Decide whether replaying this action is safe before you do it.
}
catch (MyJdAuthenticationException)
{
    // The session can no longer be renewed: connect again with the password.
}
catch (MyJdDeviceOfflineException)
{
    // The device is offline. Do not retry in a tight loop.
}
catch (MyJdApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Unknown id, for example an expired captcha job or event subscription.
}
catch (TimeoutException)
{
    // The device may have executed the action. Check its state before you replay it.
}
```

The messages built by the library contain only the error source, the error code
and the HTTP status. Log `ErrorType` and `StatusCode` as structured values. Do not
log request URLs (they contain the session token). See
[Logging and security](logging-and-security.md).

## Rate limiting

`MyJdRateLimitException` covers `TOO_MANY_REQUESTS`, `OVERLOAD` and `MAINTENANCE`.
When the service sends a delay, `RetryAfter` carries it. Otherwise `RetryAfter` is
`null` and the delay is yours to choose.

## Device offline

`MyJdDeviceOfflineException` means that the relay cannot reach the device. Do not
retry at a high rate: wait longer, or give up and report it to the user.

## Timeouts and network errors on actions

A `TimeoutException` or `HttpRequestException` does not mean the action failed.
The device may have executed it. The library never replays such a call, so the
decision is yours:

- Read-only calls (`Get...`, `Query...`, `List...`) are safe to replay.
- Actions (`Start`, `Restart`, `AddLinks`, `Set...`, `Remove...`) must be checked
  first, for example by reading the state back with
  `device.DownloadController.GetCurrentStateAsync()` before a second `StartAsync`.

## Retry policy

The library does not retry network errors, rate limits or device errors. Add a
policy in your own code or through the HTTP pipeline of the named client
([Dependency injection](dependency-injection.md)).

### Retry only read-only calls

A small retry for `MyJdRateLimitException`, limited to idempotent reads:

```csharp
using MyJdownloader;

static async Task<T> RetryReadAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken ct)
{
    const int maxAttempts = 3;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            return await read(ct);
        }
        catch (MyJdRateLimitException ex) when (attempt < maxAttempts)
        {
            // Use the delay the service asked for, or a growing default.
            await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(5 * attempt), ct);
        }
    }
}

// Usage: a read-only call only.
IReadOnlyList<DeviceInfo> devices = await RetryReadAsync(ct => client.GetDevicesAsync(ct), cancellationToken);
```

### Retry at the HTTP level

`AddMyJDownloader(...).AddStandardResilienceHandler()` (package
`Microsoft.Extensions.Http.Resilience`) adds a standard retry policy to the named
client. Device calls are `POST` requests and the policy is not specific to
read-only calls. Read the defaults of the resilience package and configure them
before enabling it. See [Dependency injection](dependency-injection.md#enrich-the-pipeline).

## Cancellation

Pass the `CancellationToken` you receive to every call. Cancelling it throws
`OperationCanceledException`, which is not a `MyJdException`, so it is not caught
by a `catch (MyJdException)` block.
