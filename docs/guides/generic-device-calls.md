# Generic device calls

Every device has a `Channel` (`IDeviceChannel`) that sends any device action
through the same encrypted transport as the typed facades. Use it for a
namespace or action that has no typed facade yet. The typed facades are
preferred whenever they exist: they check their arguments and map the
responses to types.

The facades are not tied to the channel of a device: each one can be built over any `IDeviceChannel`, typically a test double. See [Testing your code](testing-your-code.md).

The channel design is described in [ADR-001](../adr/ADR-001-typed-facade-over-generic-core.md)
and [ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md).

`IDeviceChannel` only carries JSON answers. Binary actions, such as the icons of
`device.ContentV2`, are not reachable through it: their PNG answers are not
encrypted, and the channel cannot return them. Use the typed facade when it
exists. A caller that needs another binary action must wait for a public API
([ADR-011](../adr/ADR-011-unauthenticated-binary-device-responses.md)).

## Two overloads

```csharp
// 1. Deserializes the "data" payload of the answer.
Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken ct = default);

// 2. Ignores the "data" payload; completes when the device has answered.
Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken ct = default);
```

- `path` is the action, for example `/jd/version`. Each segment is made of ASCII
  letters, digits and underscores; the whole path is limited to 256 characters.
- `responseType` is the source-generated metadata used to deserialize the
  response. The library is AOT compatible, so the type must be declared in a
  `JsonSerializerContext` of your own (see below).
- `args` are the action arguments. Each entry is sent as one string parameter: a
  JSON string contributes its text, any other value contributes its raw JSON text.
- The arguments are copied before the method returns, so the `JsonDocument` that
  produced them can be disposed right after the call.

## Declare the JSON context

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(long?))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
```

Add one `[JsonSerializable(...)]` attribute per response type you deserialize.
The generated metadata is then available as `AppJsonContext.Default.<Name>`
(for example `NullableInt64` for `long?`).

## Call an action

```csharp
using System.Text.Json;
using MyJdownloader;

IReadOnlyList<JsonElement> noArgs = [];

// /jd/version is also covered by device.Jd: the typed facade is preferred.
long? version = await device.Channel.CallAsync(
    "/jd/version",
    AppJsonContext.Default.NullableInt64,
    noArgs,
    cancellationToken);
```

For an action with arguments, build one `JsonElement` per argument, in the order
the action expects. The argument list is defined by the action itself: copy the
path and the order from the typed facade that wraps the same action, for example
[`ConfigApi`](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/src/MyJdownloader/Config/ConfigApi.cs).

```csharp
using System.Text.Json;

IReadOnlyList<JsonElement> args =
[
    JsonSerializer.SerializeToElement("my-interface"), // a string: sent as its text
    JsonSerializer.SerializeToElement(3),              // a number: sent as its JSON text "3"
];
```

## Errors

The channel throws the same exceptions as the facades:

- `ArgumentException` and `ArgumentNullException` for an invalid path or null
  argument, thrown synchronously.
- `InvalidOperationException` when the client is not connected.
- `MyJdException` (see [Error handling](error-handling.md)) when the relay or the
  device rejects the request or the answer is invalid.
- `TimeoutException`, `HttpRequestException` and `IOException`: the action may
  already have been executed by the device. The library never retries it, so
  decide yourself whether replaying it is safe.

A `TOKEN_INVALID` answer triggers one automatic session renewal and one replay
(see [Sessions and authentication](sessions-and-authentication.md)). Only that
case is replayed by the library.

## Security

- Paths and arguments are actions on a remote device. Never build a path or an
  argument from end-user input without an allow-list.
- Do not log the arguments or the response: they can contain secrets (passwords,
  tokens). See [Logging and security](logging-and-security.md).
- Responses of unknown namespaces are untrusted text: encode them before
  displaying them.
