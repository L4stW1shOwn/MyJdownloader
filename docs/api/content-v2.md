# ContentV2

The contentV2 namespace returns the favicon of a hoster, the icon of a file type and icons by key, as PNG images, and describes the icon keys of the device.

**Coverage:** 4/4 actions of `/contentV2` (complete).

Reach it as `device.ContentV2`, which returns a `ContentV2Api`. The C# namespace is `MyJdownloader.ContentV2`, and the device API path is `/contentV2`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning: unauthenticated bytes.** The three icon methods return PNG images that the device sends outside the encrypted envelope. Nothing authenticates the bytes or binds them to your request: only TLS to the relay protects them, and the relay, or anything able to alter its traffic, could substitute them (ADR-011). The library checks the HTTP status, the `image/png` content type, the 1 MiB limit and the PNG signature, and nothing more. Decode the bytes with a robust, sandboxed image decoder, never serve them under another content type, and never base a security decision on them.

## ContentV2Api

`public sealed class ContentV2Api`

Typed facade over the `/contentV2` namespace of one device (hoster favicons, file-type icons, icons by key and icon descriptions).

| Member | Signature | Description |
|---|---|---|
| `GetFavIconAsync` | `Task<byte[]> GetFavIconAsync(string hosterName, CancellationToken cancellationToken = default)` | Gets the favicon of a hoster, for example `rapidgator.net` (`/contentV2/getFavIcon`). Returns the PNG bytes. |
| `GetFileIconAsync` | `Task<byte[]> GetFileIconAsync(string fileName, CancellationToken cancellationToken = default)` | Gets the icon of a file type, chosen from the extension of `fileName` (`/contentV2/getFileIcon`). Returns a 16×16 PNG. |
| `GetIconAsync` | `Task<byte[]> GetIconAsync(string key, int size, CancellationToken cancellationToken = default)` | Gets an icon by key at `size` pixels, from 1 to 128 (`/contentV2/getIcon`). Returns the PNG bytes. |
| `GetIconDescriptionAsync` | `Task<IconDescriptor?> GetIconDescriptionAsync(string key, CancellationToken cancellationToken = default)` | Describes an icon key (`/contentV2/getIconDescription`). Returns `null` when the device sends no description, which is the case for a key it does not know. |

Common remarks:

- The string arguments are validated before any request: `null` throws `ArgumentNullException`, a blank value, one with control characters, or one longer than its allowed length (see [Argument validation](../guides/error-handling.md#argument-validation)) throws `ArgumentException`. The validation messages never repeat the value.
- The arguments are sent as they are, without escaping or normalization.
- A successful icon answer that is not a PNG of at most 1 MiB, or whose `Content-Type` is not `image/png`, raises `MyJdProtocolException`. The device errors (for example `FILE_NOT_FOUND`) raise `MyJdApiException`, whose `ErrorType` holds the code.
- The icon methods throw `NotSupportedException` when the channel of the device does not support binary answers. This cannot happen with a device obtained from `MyJDownloaderClient`.

### GetFavIconAsync

Remarks:

- A hoster without favicon gives `404 FILE_NOT_FOUND`, which surfaces as a `MyJdApiException` with `ErrorType` `FILE_NOT_FOUND`. Treat it as "no icon", not as a failure of the device.

### GetFileIconAsync

Remarks:

- The icon depends only on the extension of the name: the file itself is not read by the device.

### GetIconAsync

Remarks:

- `size` is checked before any request: a value outside 1 to 128 throws `ArgumentOutOfRangeException`. The device would answer `400 BAD_PARAMETERS`.
- `key` accepts a theme icon name, `myjdownloader` (the logo), a `kc.` key that the device registers when it reports icons, or a JSON icon descriptor text starting with `{`. An unknown `kc.` key gives `404 FILE_NOT_FOUND`.

### GetIconDescriptionAsync

Remarks:

- The result is an `IconDescriptor` built from device data: it is untrusted. The properties are described in the IconDescriptor section below.

### Example

```csharp
using System.IO;
using MyJdownloader;
using MyJdownloader.ContentV2;

// 1. Fetch the favicon of a hoster. The bytes are untrusted and unauthenticated (ADR-011).
byte[]? png = null;
try
{
    png = await device.ContentV2.GetFavIconAsync("rapidgator.net", cancellationToken);
}
catch (MyJdApiException ex) when (ex.ErrorType == "FILE_NOT_FOUND")
{
    // The hoster has no favicon: keep the placeholder of your application.
}

// 2. Store the bytes as data in a file of your own. Never serve them under another content type.
if (png is not null)
{
    await File.WriteAllBytesAsync(Path.Combine(iconDirectory, "rapidgator.net.png"), png, cancellationToken);
}

// 3. Describe an icon key. The result is untrusted, and null when the device does not know the key.
IconDescriptor? descriptor = await device.ContentV2.GetIconDescriptionAsync(key, cancellationToken);
```

`device`, `iconDirectory`, `key` and `cancellationToken` stand for your own device, output folder, icon key and cancellation token.

## IconDescriptor

`public sealed record IconDescriptor`

A device-side description of an icon, as returned by `GetIconDescriptionAsync`. Absent fields are `null`.

| Property | Type | Description |
|---|---|---|
| `Cls` | `string? Cls { get; init; }` | The icon class name. |
| `Key` | `string? Key { get; init; }` | The icon key. |
| `Prps` | `JsonElement? Prps { get; init; }` | The icon properties, as untyped JSON. `ToString()` shows only its JSON kind. |
| `Rsc` | `IReadOnlyList<IconDescriptor>? Rsc { get; init; }` | The nested icon resources, recursively. |

**Remarks**

- Every value comes from the device and is untrusted: encode text before you display it, and do not log `Prps`.
- `Rsc` is recursive and bounded by the response depth limit of the library; the device may still send a deep tree, so do not recurse over it without a limit of your own.
- Unknown JSON fields are ignored.

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [Generic device calls](../guides/generic-device-calls.md): binary actions are not reachable through `IDeviceChannel` (ADR-011).
- [ADR-011: unauthenticated binary device responses](../adr/ADR-011-unauthenticated-binary-device-responses.md)
