# Dialogs

The dialogs namespace exposes the dialogs JDownloader is waiting on. Use it to list the open dialogs, read one together with the description of its type, and answer it.

**Coverage:** 4/4 actions of `/dialogs` (complete).

Reach it as `device.Dialogs`, which returns a `DialogsApi`. The C# namespace is `MyJdownloader.Dialogs`, and the device API path is `/dialogs`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Text returned by the device (`DialogInfo`, `DialogTypeInfo`) is untrusted: it can contain file names, URLs or messages. Encode it before displaying it.

## DialogsApi

`public sealed class DialogsApi`

Typed facade over the `/dialogs` namespace of one device (dialogs JDownloader is waiting on).

| Member | Signature | Description |
|---|---|---|
| `ListAsync` | `Task<IReadOnlyList<long>> ListAsync(CancellationToken cancellationToken = default)` | Lists the identifiers of the dialogs currently open (`/dialogs/list`). Empty when the device returns no data. |
| `GetAsync` | `Task<DialogInfo> GetAsync(long id, bool includeIcon = false, bool includeProperties = true, CancellationToken cancellationToken = default)` | Gets an open dialog (`/dialogs/get`). `includeIcon` is `false` by default because icons are large. `includeProperties` is `true` by default. When the id is unknown, the device answers `400 "Invalid ID"`, which surfaces as a `MyJdApiException`. |
| `GetTypeInfoAsync` | `Task<DialogTypeInfo> GetTypeInfoAsync(string dialogType, CancellationToken cancellationToken = default)` | Describes the properties of a dialog type (`/dialogs/getTypeInfo`). Pass the value of `DialogInfo.Type`. A null `dialogType` throws `ArgumentNullException`; a blank one, or one with control characters or lone surrogates, throws `ArgumentException`. When the type is unknown, the device answers `400 "Requested Type not found!"`, which surfaces as a `MyJdApiException`. |
| `AnswerAsync` | `Task AnswerAsync(long id, IReadOnlyDictionary<string, JsonElement> data, CancellationToken cancellationToken = default)` | Answers an open dialog (`/dialogs/answer`). `data` holds the answer properties (see `DialogTypeInfo.Out`) and is sent as one JSON object. A null `data` throws `ArgumentNullException`; a blank key, a key with control characters or lone surrogates, or a `default` value throws `ArgumentException`. The device result is ignored. |

Remarks:

- `AnswerAsync` acts on the device immediately and without confirmation, and cannot be undone: the dialog closes and JDownloader continues with the values you supplied. A `TimeoutException` or `HttpRequestException` means the answer may have been applied.
- When the id is unknown, `AnswerAsync` fails with `400 "Invalid ID"`. When the dialog needs another answer first, it fails with `400 "Answer Dialog N first"`. Both surface as a `MyJdApiException`.
- The values of `data` are untyped `JsonElement`s, as decided in [ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md). Build them from the types described by `DialogTypeInfo.Out`.
- The text returned by the device is untrusted: `DialogInfo.Type` and every key and value of `Properties`, as well as `DialogTypeInfo`, are raw strings.

### Example

```csharp
using System.Text.Json;
using MyJdownloader.Dialogs;

IReadOnlyList<long> ids = await device.Dialogs.ListAsync();
if (ids.Count > 0)
{
    DialogInfo dialog = await device.Dialogs.GetAsync(ids[0]);

    // Device text is untrusted: encode it before displaying it.
    if (dialog.Type is not null)
    {
        DialogTypeInfo typeInfo = await device.Dialogs.GetTypeInfoAsync(dialog.Type);
    }

    // Acts on the device without confirmation. Ask the user first, and use the keys from typeInfo.Out.
    var answer = new Dictionary<string, JsonElement>
    {
        ["ok"] = JsonSerializer.SerializeToElement(true),
    };
    await device.Dialogs.AnswerAsync(ids[0], answer);
}
```

## DialogInfo

`public sealed record DialogInfo`

A dialog currently open on the device. Fields the device does not return are `null`.

`Type` and every key and value of `Properties` are raw strings supplied by the device: encode them before displaying them.

| Property | Type | Description |
|---|---|---|
| `Type` | `string?` | Dialog type reported by the device. Its set of values is not documented. |
| `Properties` | `IReadOnlyDictionary<string, string>?` | Dialog properties (content, title, flags), or `null` when not returned or not requested. |

## DialogTypeInfo

`public sealed record DialogTypeInfo`

Describes the properties of a dialog type: what the device shows (`In`) and what an answer may contain (`Out`). Every key and value is a raw string supplied by the device: encode it before displaying it.

| Property | Type | Description |
|---|---|---|
| `In` | `IReadOnlyDictionary<string, string>?` | Properties the device sets on a dialog of this type (name to type description), or `null` when not returned. |
| `Out` | `IReadOnlyDictionary<string, string>?` | Properties an answer may carry (name to type description), or `null` when not returned. |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
