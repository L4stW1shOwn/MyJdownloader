# Extensions

The extensions namespace manages JDownloader extensions on the device: the add-ons that extend the JDownloader core. Use it to list the available extensions, check whether one is installed or enabled, install one, and enable or disable it.

**Coverage:** 5/5 actions of `/extensions` (complete).

Reach it as `device.Extensions`, which returns an `ExtensionsApi`. The C# namespace is `MyJdownloader.DeviceExtensions`, and the device API path is `/extensions`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Text returned by the device is untrusted: encode it before displaying it.

## ExtensionsApi

`public sealed class ExtensionsApi`

Typed facade over the `/extensions` namespace of one device (JDownloader extensions).

| Member | Signature | Description |
|---|---|---|
| `ListAsync` | `Task<IReadOnlyList<Extension>> ListAsync(ExtensionQuery? query = null, CancellationToken cancellationToken = default)` | Lists the extensions (`/extensions/list`). A `null` query returns all extensions with all fields. `ExtensionQuery.Pattern` is a regular expression evaluated on the device. |
| `InstallAsync` | `Task<bool> InstallAsync(string id, CancellationToken cancellationToken = default)` | Installs an extension (`/extensions/install`) and returns `true` when the device accepted the installation. The device downloads it from its own JDownloader update source, not from the caller. A timeout means the action may have been executed. |
| `IsInstalledAsync` | `Task<bool> IsInstalledAsync(string id, CancellationToken cancellationToken = default)` | Returns `true` when the extension is installed (`/extensions/isInstalled`). |
| `IsEnabledAsync` | `Task<bool> IsEnabledAsync(string className, CancellationToken cancellationToken = default)` | Returns `true` when the extension is enabled (`/extensions/isEnabled`). `className` is the extension class name, the value of `Extension.Id`. |
| `SetEnabledAsync` | `Task<bool> SetEnabledAsync(string className, bool enabled, CancellationToken cancellationToken = default)` | Enables or disables an extension (`/extensions/setEnabled`) and returns `true` when the device applied the change. Applied immediately, without confirmation. |

Remarks:

- `InstallAsync` installs the extension on the device immediately and without confirmation. Take `id` from `Extension.Id` or from an allow-list, never from end-user input.
- `InstallAsync` and `SetEnabledAsync` do not treat a timeout as a failure: the action may have been executed.
- `ExtensionQuery.Pattern` is evaluated on the device. Escape literal text (for example with `\Q...\E`) so that untrusted input cannot cause costly or overly broad matching.

### Example

```csharp
using MyJdownloader.DeviceExtensions;

var extensions = await device.Extensions.ListAsync(new ExtensionQuery { Pattern = @"\QExtraction\E" });

string? className = extensions.FirstOrDefault()?.Id;
if (className is not null && !await device.Extensions.IsEnabledAsync(className))
{
    await device.Extensions.SetEnabledAsync(className, true);
}
```

## Extension

`public sealed record Extension`

A JDownloader extension. Fields that the query did not request are `null`.

Text fields are supplied by the device and are untrusted: encode them before displaying them.

| Property | Type | Description |
|---|---|---|
| `Id` | `string?` | Extension identifier, which is the class name used by the other actions. |
| `Name` | `string?` | Display name. |
| `Description` | `string?` | Description. |
| `ConfigInterface` | `string?` | Name of the configuration interface of the extension. Use it with [Config](config.md). |
| `IconKey` | `string?` | Icon key. |
| `Enabled` | `bool?` | Whether the extension is enabled, or `null` when not requested. |
| `Installed` | `bool?` | Whether the extension is installed, or `null` when not requested. |

## ExtensionQuery

`public sealed record ExtensionQuery`

Selects the extensions and fields returned by `ListAsync`. Every field flag is `true` by default.

| Property | Type | Default | Description |
|---|---|---|---|
| `ConfigInterface` | `bool` | `true` | Returns the configuration interface name. |
| `Description` | `bool` | `true` | Returns the description. |
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `IconKey` | `bool` | `true` | Returns the icon key. |
| `Installed` | `bool` | `true` | Returns the installed flag. |
| `Name` | `bool` | `true` | Returns the name. |
| `Pattern` | `string?` | `null` | Regular expression, evaluated on the device, that extensions must match. `null` means all. Do not pass untrusted input. |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
