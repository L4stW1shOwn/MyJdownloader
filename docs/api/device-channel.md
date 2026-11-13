# Device Channel

> Package `MyJdownloader` · namespace `MyJdownloader`

`IDeviceChannel` is the single public primitive through which every typed facade reaches a JDownloader device. You normally use the facades exposed by `JDownloaderDevice`; use the channel directly to call a device action that has no typed facade yet. The channel reads the client's current session on every call, so it survives session renewals. The call is encrypted and signed by the library before it leaves through the MyJDownloader relay (ADR-006). Any `IDeviceChannel` can also back a typed facade, which is how you unit-test code that uses the library (see [Testing your code](../guides/testing-your-code.md)).

## IDeviceChannel

`public interface IDeviceChannel`

An encrypted channel to one device, reached through the MyJDownloader relay. Obtain it from `JDownloaderDevice.Channel`. The library never retries a call on its own: a call is sent a second time only after the relay answers `TOKEN_INVALID`, in which case the session is renewed and the call is replayed once.

| Member | Signature | Description |
|---|---|---|
| `CallAsync<T>` | `Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)` | Calls a device action and deserializes its `data` payload with the given type metadata. Returns `default` when the payload is absent or `null`. |
| `CallAsync` | `Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)` | Calls a device action and ignores its `data` payload. The task completes when the device has answered. |

**Parameters**

- `path`: the action path, starting with `/`, for example `/jd/version`. Each segment is made of ASCII letters, digits and underscore. The whole path is limited to 256 characters. An invalid path throws `ArgumentException` synchronously.
- `responseType`: the source-generated `JsonTypeInfo<T>` used to deserialize `data`. See "Serialization and AOT" below.
- `args`: the action arguments. Each entry is sent as one string parameter: a JSON string contributes its text, any other value contributes its raw JSON text. The arguments are copied before the method returns, so the `JsonDocument` that owns them may be disposed right after the call.
- `cancellationToken`: cancels the call.

**Exceptions**

| Exception | When |
|---|---|
| `ArgumentException`, `ArgumentNullException` | An argument is invalid or `null`. Thrown synchronously. |
| `InvalidOperationException` | The client is not connected. |
| [`MyJdException`](errors.md) | The relay or the device rejected the request, or the response is invalid. |
| `OperationCanceledException` | The operation was cancelled. |
| `TimeoutException` | The request timed out. The action may already have been executed. |
| `HttpRequestException` | A network error occurred. The action may already have been executed. |
| `IOException` | The response body could not be read. The action may already have been executed. |

**Remarks**

- **Not retried.** On `TimeoutException`, `HttpRequestException` or `IOException`, the device may have executed the action. The library never retries it; the caller decides whether replaying is safe. Only idempotent actions should be retried automatically.
- **Size limit.** There may be at most 1 024 arguments, and they may total at most 1 048 576 UTF-16 code units (`string.Length`). A non-string argument counts its JSON text (escaped form), and each argument adds 3 for the quotes and separator. The cap bounds the request but is not the HTTP body size: escaping and encryption expand it. A larger set throws `ArgumentException` on `args`, synchronously, before encryption and any HTTP request. See [Argument validation](../guides/error-handling.md#argument-validation).
- **Duration.** A call may last about three times `HttpClient.Timeout` (the call, the session renewal and the replay).
- **Misconfigured response type.** An `InvalidOperationException` raised by a misconfigured `responseType` is reported as a `MyJdProtocolException`, meaning the device answer could not be read.
- **Logging.** The session token and the device id travel in the URL path, and `HttpClient` logging does not redact them. Do not log full request URIs.

### Serialization and AOT

The library is compiled with `IsAotCompatible`. The generic overload takes a `JsonTypeInfo<T>` instead of relying on reflection, so it works under Native AOT and trimming, provided the type metadata comes from a source-generated `JsonSerializerContext`. Declare a context in your own project for every response type you read:

- use a `[JsonSerializable(typeof(...))]` attribute for each type, with the nullable form (`long?`) when the device may return `null`;
- pass the matching `Default` property (for example `NullableInt64` for `long?`) as `responseType`;
- avoid `JsonSerializerOptions` built at run time and reflection-based serialization in the call path.

The internal contexts of the library are not public; declare your own.

### Example

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using MyJdownloader;

// Source-generated metadata: no reflection, compatible with Native AOT.
[JsonSerializable(typeof(long?))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}

// Typed call: the response is deserialized with the generated metadata.
JDownloaderDevice device = client.GetDevice(deviceId);
long? version = await device.Channel.CallAsync(
    "/jd/version",
    AppJsonContext.Default.NullableInt64,
    [],
    cancellationToken);

// Void call: the response payload is ignored.
await device.Channel.CallAsync("/jd/refreshPlugins", [], cancellationToken);
```

## See also

- [Client API](client.md): obtaining a `JDownloaderDevice` and its `Channel`
- [Errors](errors.md): the `MyJdException` hierarchy thrown by the channel
- [Common types](common-types.md)
- [Getting started](../guides/getting-started.md)
- [Error handling](../guides/error-handling.md)
- [ADR-008: untyped values as JsonElement](../adr/ADR-008-untyped-values-as-jsonelement.md)
