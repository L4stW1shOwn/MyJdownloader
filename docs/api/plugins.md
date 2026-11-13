# Plugins

The plugins namespace exposes the settings of JDownloader's hoster and crawler plugins and the URL patterns that decide which plugin handles a link. Use it to list plugins, read, change or reset a plugin setting, and find which plugin matches a URL.

**Coverage:** 7/7 actions of `/plugins` (complete).

Reach it as `device.Plugins`, which returns a `PluginsApi`. The C# namespace is `MyJdownloader.Plugins`, and the device API path is `/plugins`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Values are untyped on the device, so they are exposed as `System.Text.Json.JsonElement` ([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)). Plugin settings often hold secrets, such as hoster credentials and tokens: never log values. `PluginInfo` and `PluginConfigEntry` print only the JSON kind of their values in `ToString()`.

`ConfigQuery`, `ConfigValueType` and `ConfigEnumOption` are reused from the config namespace (`MyJdownloader.Config`) and are described in [config.md](config.md).

## PluginsApi

`public sealed class PluginsApi`

Typed facade over the `/plugins` namespace of one device (plugin settings and URL patterns).

| Member | Signature | Description |
|---|---|---|
| `GetAsync` | `Task<JsonElement> GetAsync(string interfaceName, string displayName, string key, CancellationToken cancellationToken = default)` | Returns the current value of a plugin setting (`/plugins/get`). Returns an element of kind `JsonValueKind.Null` when the device returns none. The value may hold a secret. |
| `SetAsync` | `Task<bool> SetAsync(string interfaceName, string displayName, string key, JsonElement value, CancellationToken cancellationToken = default)` | Sets the value of a plugin setting (`/plugins/set`) and returns `true` when the device accepted it. Applied immediately, without confirmation. An undefined (`default`) element throws `ArgumentException`. |
| `ResetAsync` | `Task<bool> ResetAsync(string interfaceName, string displayName, string key, CancellationToken cancellationToken = default)` | Resets a plugin setting to its default value (`/plugins/reset`) and returns `true` when the device reset it. Applied immediately, without confirmation. |
| `ListAsync` | `Task<IReadOnlyList<PluginInfo>> ListAsync(PluginsQuery? query = null, CancellationToken cancellationToken = default)` | Lists the plugins and their settings (`/plugins/list`). A `null` query returns the pattern and the version. Empty when the device returns no data. |
| `QueryAsync` | `Task<IReadOnlyList<PluginConfigEntry>> QueryAsync(ConfigQuery? query = null, CancellationToken cancellationToken = default)` | Lists plugin configuration entries (`/plugins/query`). A `null` query returns all entries without their current values. |
| `GetAllPluginRegexAsync` | `Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllPluginRegexAsync(CancellationToken cancellationToken = default)` | Returns the URL patterns of all plugins, keyed by plugin (`/plugins/getAllPluginRegex`). Empty when the device returns no data. |
| `GetPluginRegexAsync` | `Task<IReadOnlyList<string>> GetPluginRegexAsync(string url, CancellationToken cancellationToken = default)` | Returns the URL patterns of the plugins matching a URL (`/plugins/getPluginRegex`). Empty when the device returns no data. |

The identifiers `interfaceName`, `displayName`, `key` and `url` throw `ArgumentNullException` when null, and `ArgumentException` when blank, when they contain control characters or lone surrogates, or when they are longer than its allowed length (see [Argument validation](../guides/error-handling.md#argument-validation)). For `QueryAsync`, the same `ArgumentException` applies to `ConfigQuery.ConfigInterface` and `ConfigQuery.Pattern` when they are set.

Remarks:

- `SetAsync` and `ResetAsync` change the device configuration without confirmation. A `TimeoutException` or `HttpRequestException` means the action may have been executed: read the value back before you replay it.
- `SetAsync` sends the value as its JSON text: strings are quoted. This wire behaviour is still to be confirmed by live tests ([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)).
- Values may hold secrets. `ListAsync` returns current values, and `QueryAsync` returns them only when `ConfigQuery.Values` is `true`. Never log whole entries or values.
- `PluginInfo.Pattern`, `ConfigQuery.Pattern` and the results of `GetAllPluginRegexAsync` and `GetPluginRegexAsync` are regular expressions defined or evaluated on the device. Do not compile device patterns without a match timeout.
- `GetPluginRegexAsync` sends the URL to the device as is. The URL is not required to be a valid absolute URI.
- Responses above `MyJDownloaderOptions.MaxResponseBodyLength` (1 MiB of encrypted body by default, about 768 KiB of JSON) fail with `MyJdProtocolException`. The list of all plugins can be large: restrict `QueryAsync` with `ConfigQuery.ConfigInterface` or `ConfigQuery.Pattern`.
- The device rejects unknown identifiers and invalid values with `400 BAD_PARAMETERS` or `400 INVALID_VALUE`, which surface as a `MyJdApiException`.
- All text returned by the device (names, documentation, patterns, enumeration labels) is untrusted: encode it before displaying it.

### Example

```csharp
using System.Text.Json;
using MyJdownloader.Plugins;

// List the plugins. Skip the pattern; the version is still returned.
IReadOnlyList<PluginInfo> plugins = await device.Plugins.ListAsync(new PluginsQuery { Pattern = false });
if (plugins.Count > 0)
{
    PluginInfo plugin = plugins[0];

    // Device text is untrusted: encode it before displaying it. Never log Value or DefaultValue.
    Console.WriteLine($"{Encode(plugin.DisplayName ?? string.Empty)}: {Encode(plugin.Key ?? string.Empty)}");

    if (plugin.InterfaceName is not null && plugin.DisplayName is not null && plugin.Key is not null)
    {
        JsonElement current = await device.Plugins.GetAsync(plugin.InterfaceName, plugin.DisplayName, plugin.Key);

        // Changing a setting applies immediately and without confirmation: ask the user first.
        if (current.ValueKind == JsonValueKind.True && AskUser("Disable this plugin setting?"))
        {
            await device.Plugins.SetAsync(
                plugin.InterfaceName,
                plugin.DisplayName,
                plugin.Key,
                JsonSerializer.SerializeToElement(false));
        }
    }
}

// Which plugin patterns match a URL? The patterns are device-side regexes: do not compile them without a timeout.
IReadOnlyList<string> patterns = await device.Plugins.GetPluginRegexAsync("https://example.com/file/abc");
Console.WriteLine($"{patterns.Count} pattern(s) match.");
```

`Encode` and `AskUser` stand for your own output encoding and user prompt.

## PluginsQuery

`public sealed record PluginsQuery`

Selects the fields returned by `ListAsync`; both are returned by default.

| Property | Type | Default | Description |
|---|---|---|---|
| `Pattern` | `bool` | `true` | Returns the URL pattern of each plugin. |
| `Version` | `bool` | `true` | Returns the version of each plugin. |

## PluginInfo

`public sealed record PluginInfo`

A plugin and one of its settings, as listed by `ListAsync`. Fields that the device does not return are `null`.

`Value` and `DefaultValue` may hold secrets: do not log them. `ToString()` prints only the JSON kind of each value, never its content. All text comes from the device and is untrusted.

| Property | Type | Description |
|---|---|---|
| `InterfaceName` | `string?` | Name of the configuration interface of the plugin. |
| `DisplayName` | `string?` | Display name of the plugin. |
| `Key` | `string?` | Setting key. |
| `ClassName` | `string?` | Java class name of the plugin. |
| `Storage` | `string?` | Storage the setting belongs to. |
| `Type` | `string?` | Java type name of the setting. |
| `AbstractType` | `ConfigValueType?` | Abstract type of the setting (see [config.md](config.md)). |
| `Docs` | `string?` | Documentation text. |
| `EnumLabel` | `string?` | Label of the enumeration type, when the setting is an enumeration. |
| `Pattern` | `string?` | URL pattern of the plugin, a regular expression defined on the device. `null` when not requested. |
| `Version` | `string?` | Version of the plugin, or `null` when not requested. |
| `Value` | `JsonElement?` | Current value. A JSON `null` is also read as `null`. May hold a secret. |
| `DefaultValue` | `JsonElement?` | Default value. May hold a secret. |
| `EnumOptions` | `IReadOnlyList<ConfigEnumOption>?` | Options of an enumeration setting, or `null` when not applicable. |

## PluginConfigEntry

`public sealed record PluginConfigEntry`

A plugin configuration entry, as returned by `QueryAsync`. Fields that the query did not request are `null`.

`Value` and `DefaultValue` may hold secrets: do not log them. `ToString()` prints only the JSON kind of each value, never its content. All text comes from the device and is untrusted.

| Property | Type | Description |
|---|---|---|
| `InterfaceName` | `string?` | Name of the configuration interface that holds the entry. |
| `Storage` | `string?` | Storage the entry belongs to. |
| `Key` | `string?` | Entry key. |
| `Type` | `string?` | Java type name of the entry. |
| `AbstractType` | `ConfigValueType?` | Abstract type of the entry (see [config.md](config.md)). |
| `Docs` | `string?` | Documentation text. |
| `EnumLabel` | `string?` | Label of the enumeration type, when the entry is an enumeration. |
| `Value` | `JsonElement?` | Current value, when requested. A JSON `null` is also read as `null`. May hold a secret. |
| `DefaultValue` | `JsonElement?` | Default value, when requested. May hold a secret. |
| `EnumOptions` | `IReadOnlyList<ConfigEnumOption>?` | Options of an enumeration entry, or `null` when not applicable or not requested. |

See also:

- [client.md](client.md)
- [config.md](config.md) for `ConfigQuery`, `ConfigValueType` and `ConfigEnumOption`
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
