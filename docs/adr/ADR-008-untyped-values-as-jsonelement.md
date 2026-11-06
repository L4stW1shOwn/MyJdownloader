# ADR-008: Untyped API values as `JsonElement`

- **Status**: Accepted
- **Date**: 2026-11-06
- **Reference**: `docs/superpowers/specs/2026-11-06-t015-config-design.md` (§3.1, §3.3, §4)

## Context

T-015 (`/config`) is the first facade whose API values are untyped: the
advanced configuration API declares setting values as `Object`
(`/config/get`, `/config/getDefault`, `/config/set`, and the `value` /
`defaultValue` members of `AdvancedConfigAPIEntry`). A value may be a boolean,
a number, a string, a list, an object or `null`, and its shape is only hinted
at by the entry's `abstractType`, which itself evolves with JDownloader.
Later facades may meet the same `Object` type.

Constraints:

- the core is `IsAotCompatible` (ADR-002): no reflection-based serialization,
  so `object` cannot be (de)serialized polymorphically;
- values must round-trip losslessly (number precision, nested structures,
  types the library does not know yet);
- configuration values may hold secrets (proxy passwords, API keys): they must
  not leak through `ToString()`, the debugger or logs (ADR-005, ADR-006,
  ADR-007);
- `DeviceChannel` (T-007) sends each argument as one string parameter: **a JSON
  string contributes its raw text, any other JSON value its raw JSON text**
  (`src/MyJdownloader/IDeviceChannel.cs` remarks). A string value `"true"`
  passed as a JSON string would therefore reach the device as `true` and be
  read as a boolean; `"42"` as a number. For `Object` parameters, the string
  encoding of the channel is ambiguous.

## Decision

- **Untyped API values (`Object`) are exposed as `System.Text.Json.JsonElement`**,
  never as `object` nor as a CLR type guessed from `abstractType`. Results use
  `JsonElement` (e.g. `GetAsync`), optional members use `JsonElement?`
  (e.g. `ConfigEntry.Value`). An absent or JSON `null` single-value result is
  returned as a `JsonElement` of kind `Null` (a null setting is a legitimate
  value). Returned elements are detached (`Clone()`), never tied to a pooled
  buffer.
- **Values written to `Object` parameters are sent as their JSON text**: the
  facade passes `JsonSerializer.SerializeToElement(value.GetRawText())`, so the
  wire parameter is `"foo"` (quoted) for a string and `true`, `42`, `["a"]`,
  `{"k":1}` otherwise. This defeats the raw-string encoding of the channel; it
  relies on the device parsing `Object` parameters as JSON. A default
  `JsonElement` (`ValueKind == Undefined`) is rejected with `ArgumentException`
  before any call.
- **`ToString()` and the debugger never show untyped values**: records holding
  them override `PrintMembers` to print only the JSON kind (`<String>`,
  `<Number>`, ... or null), and the members carry
  `[DebuggerBrowsable(DebuggerBrowsableState.Never)]`. XML doc warns that values
  may hold secrets and must not be logged.
- **Queries fetch untyped values only on request**: the corresponding query flag
  defaults to `false` (e.g. `ConfigQuery.Values = false`), in line with the
  ADR-007 rule for secrets; descriptive flags keep their default `true`.
- **Pending confirmation**: the quoted-string wire behaviour must be confirmed
  against a real device by the T-031 live tests (`[Category("Live")]`). If the
  device does not parse `Object` parameters as JSON, this ADR is amended
  (status kept, amendment recorded) and the encoding adjusted in a patch
  release.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Typed overloads (`SetAsync(..., bool)`, `(..., int)`, `(..., string)`, ...) | Larger public surface to stabilize under PublicApiAnalyzers; still needs a raw path for lists, objects and types the library does not know; reading would still require an untyped result. |
| `object` (boxed CLR values, polymorphic serialization) | Relies on reflection-based serialization, not AOT/trimming-safe (ADR-002); the CLR type would be guessed from JSON, losing precision and structure. |
| Raw `string` values (caller supplies the wire text) | Ambiguous on the wire: the channel sends strings raw, so `"true"` and `true` are indistinguishable; pushes JSON encoding knowledge to every caller. |
| `JsonNode` | Mutable and heavier; `JsonElement` is the type already used for channel arguments (T-007) and is immutable, matching record semantics. |

## Consequences

Positive:

- Lossless, AOT-safe representation of any value, including types added by
  future JDownloader versions.
- A string value set by the caller stays a string on the wire.
- Secret-safe by default: values are neither fetched nor printed unless the
  caller explicitly asks.
- One consistent rule for every facade meeting an API `Object` type.

Negative:

- Callers must read values through the `JsonElement` API (`ValueKind`,
  `GetBoolean()`, `GetString()`, ...) and build values with
  `JsonSerializer.SerializeToElement` or `JsonDocument`; no compile-time typing.
- `JsonElement` becomes part of the public contract (System.Text.Json, already
  in the BCL, so no new dependency).
- Record equality on `JsonElement` members is not structural (struct wrapping a
  document reference): two entries with the same value text may compare unequal.
- The quoted-string encoding is unconfirmed against a real device until T-031;
  a mismatch would require a behavioural fix.
- `ToString()` diagnostics show only the value kind, never the content.
