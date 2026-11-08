# Config

The config namespace reads and changes JDownloader's advanced configuration entries, grouped by configuration interface. Use it to inspect and tune entries that no other typed facade covers.

**Coverage:** 6/7 actions of `/config` — not yet typed: `list`. See [API coverage](../api-coverage.md).

Reach it as `device.Config`, which returns a `ConfigApi`. The C# namespace is `MyJdownloader.Config`, and the device API path is `/config`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Values are untyped on the device, so they are exposed as `System.Text.Json.JsonElement` ([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)). Many entries hold secrets, such as proxy passwords and API keys: never log values.

## ConfigApi

`public sealed class ConfigApi`

Typed facade over the `/config` namespace of one device (advanced configuration entries).

| Member | Signature | Description |
|---|---|---|
| `GetAsync` | `Task<JsonElement> GetAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)` | Returns the current value of an entry (`/config/get`). Returns an element of kind `JsonValueKind.Null` when the device returns none. The value may hold a secret. |
| `GetDefaultAsync` | `Task<JsonElement> GetDefaultAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)` | Returns the default value of an entry (`/config/getDefault`). Same `Null` convention as `GetAsync`. |
| `SetAsync` | `Task<bool> SetAsync(string interfaceName, string? storage, string key, JsonElement value, CancellationToken cancellationToken = default)` | Sets the value of an entry (`/config/set`) and returns `true` when the device accepted it. The change is applied immediately, without confirmation. An undefined (`default`) element throws `ArgumentException`. |
| `ResetAsync` | `Task<bool> ResetAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)` | Resets an entry to its default value (`/config/reset`) and returns `true` when the device reset it. Applied immediately, without confirmation. |
| `QueryAsync` | `Task<IReadOnlyList<ConfigEntry>> QueryAsync(ConfigQuery? query = null, CancellationToken cancellationToken = default)` | Lists entries (`/config/query`). A `null` query returns all entries without their current values. Responses above 1 MiB fail with `MyJdProtocolException`: restrict the query with `ConfigInterface` or `Pattern`. |
| `ListEnumAsync` | `Task<IReadOnlyList<ConfigEnumOption>> ListEnumAsync(string type, CancellationToken cancellationToken = default)` | Lists the options of an enumeration type (`/config/listEnum`). `type` is the Java type name reported in `ConfigEntry.Type`. |

Remarks:

- `SetAsync` sends the value as its JSON text: strings are quoted. This wire behaviour is still to be confirmed by live tests ([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)).
- `ConfigQuery.Values` is `false` by default because current values may hold secrets. When you request them, never log whole `ConfigEntry` values.
- `ConfigQuery.Pattern` is a regular expression evaluated on the device, not by this library.
- `ResetAsync` and `SetAsync` change the device configuration without confirmation. A timeout means the action may have been executed.

### Example

```csharp
using System.Text.Json;
using MyJdownloader.Config;

var entries = await device.Config.QueryAsync(new ConfigQuery { Pattern = "maxsimultanedownloads", Values = true });
var entry = entries[0];

// Values may hold secrets: do not log them.
JsonElement current = await device.Config.GetAsync(entry.InterfaceName!, entry.Storage, entry.Key!);

await device.Config.SetAsync(entry.InterfaceName!, entry.Storage, entry.Key!, JsonSerializer.SerializeToElement(3));
```

## ConfigEntry

`public sealed record ConfigEntry`

An advanced configuration entry. Fields that the query did not request are `null`.

`Value` and `DefaultValue` may hold secrets: do not log them. `ToString()` prints only the JSON kind of each value, never its content.

| Property | Type | Description |
|---|---|---|
| `InterfaceName` | `string?` | Name of the configuration interface that holds the entry. |
| `Storage` | `string?` | Storage the entry belongs to, or `null` for the default storage. |
| `Key` | `string?` | Entry key. |
| `Type` | `string?` | Java type name of the entry. Pass it to `ListEnumAsync` for enumeration types. |
| `AbstractType` | `ConfigValueType?` | Abstract type of the entry. |
| `Docs` | `string?` | Documentation text. |
| `EnumLabel` | `string?` | Label of the enumeration type, when the entry is an enumeration. |
| `Value` | `JsonElement?` | Current value, when requested. A JSON `null` is also read as `null`. May hold a secret. |
| `DefaultValue` | `JsonElement?` | Default value, when requested. May hold a secret. |
| `EnumOptions` | `IReadOnlyList<ConfigEnumOption>?` | Options of an enumeration entry, or `null` when not applicable or not requested. |

## ConfigQuery

`public sealed record ConfigQuery`

Selects the entries and fields returned by `QueryAsync`. The documentation, default value, enumeration and extension flags are `true` by default; current values are not.

| Property | Type | Default | Description |
|---|---|---|---|
| `ConfigInterface` | `string?` | `null` | Restricts the query to one configuration interface. `null` means all. |
| `Pattern` | `string?` | `null` | Regular expression, evaluated on the device, that entries must match. `null` means all. |
| `Description` | `bool` | `true` | Returns the documentation text. |
| `DefaultValues` | `bool` | `true` | Returns the default values. |
| `EnumInfo` | `bool` | `true` | Returns the enumeration options. |
| `IncludeExtensions` | `bool` | `true` | Includes the entries of extensions. |
| `Values` | `bool` | `false` | Returns the current values. They may hold secrets. |

## ConfigEnumOption

`public sealed record ConfigEnumOption`

An option of an enumeration-typed configuration entry.

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Option name, as sent to the device. |
| `Label` | `string?` | Human-readable label, or `null` when none is provided. |

## ConfigValueType

`public enum ConfigValueType`

Abstract type of a configuration entry, as used by the JDownloader API. Unrecognized values, including the API's own `UNKNOWN`, are read as `Unknown`.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The value is missing or not recognized by this library. |
| `Boolean` | `1` | A boolean (`BOOLEAN`). |
| `Int` | `2` | A 32-bit integer (`INT`). |
| `Long` | `3` | A 64-bit integer (`LONG`). |
| `String` | `4` | A string (`STRING`). |
| `Object` | `5` | An arbitrary object (`OBJECT`). |
| `ObjectList` | `6` | A list of objects (`OBJECT_LIST`). |
| `StringList` | `7` | A list of strings (`STRING_LIST`). |
| `Enum` | `8` | An enumeration value (`ENUM`). |
| `Byte` | `9` | A byte (`BYTE`). |
| `Char` | `10` | A character (`CHAR`). |
| `Double` | `11` | A double-precision number (`DOUBLE`). |
| `Float` | `12` | A single-precision number (`FLOAT`). |
| `Short` | `13` | A 16-bit integer (`SHORT`). |
| `BooleanList` | `14` | A list of booleans (`BOOLEAN_LIST`). |
| `ByteList` | `15` | A list of bytes (`BYTE_LIST`). |
| `ShortList` | `16` | A list of 16-bit integers (`SHORT_LIST`). |
| `LongList` | `17` | A list of 64-bit integers (`LONG_LIST`). |
| `IntList` | `18` | A list of 32-bit integers (`INT_LIST`). |
| `FloatList` | `19` | A list of single-precision numbers (`FLOAT_LIST`). |
| `EnumList` | `20` | A list of enumeration values (`ENUM_LIST`). |
| `DoubleList` | `21` | A list of double-precision numbers (`DOUBLE_LIST`). |
| `CharList` | `22` | A list of characters (`CHAR_LIST`). |
| `HexColor` | `23` | A hexadecimal color (`HEX_COLOR`). |
| `HexColorList` | `24` | A list of hexadecimal colors (`HEX_COLOR_LIST`). |
| `Action` | `25` | An action, triggered rather than read (`ACTION`). |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
