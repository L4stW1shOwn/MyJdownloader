# ADR-007: Namespace facade conventions

- **Status**: Accepted
- **Date**: 2026-11-06
- **Reference**: `docs/superpowers/specs/2026-11-06-t010-linkgrabber-design.md` (§2, §3, §5)

## Context

ADR-001 decided on a hand-written typed facade, one device namespace per
backlog entry, built on the public `IDeviceChannel.CallAsync<T>` core. T-010
(`/linkgrabberv2`) is the first facade to be delivered; T-011..T-019 follow the
same shape. Without shared conventions, each facade would make its own choices
about C# namespaces, JSON serialization, enum handling, query defaults, null
responses, validation and secret exposure, producing an inconsistent public
surface that is hard to stabilize under PublicApiAnalyzers.

Constraints that apply to every facade:

- the core is `IsAotCompatible` (ADR-002): no reflection-based serialization;
- the device API evolves independently of the library: new enum values and new
  response fields must not break reading;
- query endpoints only return the fields explicitly requested, and some fields
  are secrets (download/archive passwords);
- arguments reach the channel as `IReadOnlyList<JsonElement>` (T-007): strings
  are sent raw, objects and arrays as their JSON text, as in the reference
  client;
- no exception message, log or `ToString()` may disclose a secret (ADR-005,
  ADR-006).

## Decision

Every namespace facade follows these conventions.

Structure:

- **One C# namespace per facade**: `MyJdownloader.<Facade>` (e.g.
  `MyJdownloader.LinkGrabber`) holds the facade class and its request/response
  records.
- **One `sealed` class `<Facade>Api`** with an `internal` constructor taking
  `IDeviceChannel`; it is stateless.
- **Lazy property on `JDownloaderDevice`** (`device.LinkGrabber`), created once
  per device on first access.
- **Cross-facade enums** (e.g. `Priority`, `AvailableLinkState`) live in the root
  `MyJdownloader` namespace; enums specific to one facade stay in its namespace.
- Records are `sealed record` with `init` properties.

Serialization:

- **One internal source-generated `JsonSerializerContext` per facade** (e.g.
  `LinkGrabberJsonContext`): camelCase, `UnmappedMemberHandling.Skip`,
  `DefaultIgnoreCondition = WhenWritingNull`. Explicit `[JsonPropertyName]`
  where the API name differs from camelCase (`uuid`, `packageUUID`,
  `packageUUIDs`, `jobUUIDs`, `variantID`, `assignJobID`, ...).
  List members (`Links`, `Hosts`, `PackageUuids`) compare by reference, so the
  record equality of the records holding them is reference-based for those
  members.
- **Tolerant enums**: every API enum has `Unknown = 0`. An internal generic
  `TolerantEnumConverter<TEnum>` (AOT-safe: `Enum.GetValues<TEnum>()` and
  `JsonNamingPolicy.SnakeCaseUpper`) reads any unrecognized string as
  `Unknown`; writing `Unknown` throws `ArgumentException` (the facade rejects it
  earlier, see validation). Converters are registered in each internal context
  (`JsonSourceGenerationOptions.Converters`) and never attached to the public
  enums with `[JsonConverter]`, so callers' own serialization of these enums is
  unaffected.
- **Arguments** are built with `JsonSerializer.SerializeToElement(value,
  context.TypeInfo)`: strings and enums become JSON strings (sent raw by the
  channel), objects and arrays their JSON text. Parameter order matches the
  reference client.
- **Query records include every field by default**: non-nullable `bool` flags
  defaulting to `true`, **except secrets**, which default to `false` and are
  fetched only on explicit request (e.g. `LinkQuery.Password = false`). Paging
  defaults follow the API (`MaxResults = -1`, `StartAt = 0`). Other request
  records keep nullable members (`bool?`, `string?`), omitted from the JSON
  when null.

Responses (`data` absent or `null`):

| Result kind | Behaviour |
|---|---|
| List | Empty list |
| Scalar (`bool`, `int`, ...) or required object | `MyJdProtocolException` (device response unreadable) |
| Result-less method | Ignored |

Validation and secrets:

- **Synchronous validation before any call**: null arguments
  (`ArgumentNullException`), blank strings and control characters (incl.
  CR/LF), ranges, `Unknown` enum values (`ArgumentException` /
  `ArgumentOutOfRangeException`). Messages name the parameter but never carry
  its value. Facade methods are not `async`: they validate, build the
  arguments, then return the channel task (or a small continuation to shape the
  result), so invalid input fails before any I/O.
- **Secret masking**: records holding a secret override `PrintMembers` so that
  `ToString()` shows `***` when the secret is set. Value equality is unchanged.
  Some API responses return secrets without an opt-out flag (for example a
  package `downloadPassword`): the XML doc of such members and queries must say so.

Documentation, tests and delivery:

- Each public method's XML doc cites the original API path
  (e.g. `/linkgrabberv2/addLinks`); every public type and member is documented.
- Facade tests substitute `IDeviceChannel` (NSubstitute) and assert the exact
  path, the exact raw JSON of every argument (including defaults) and the
  returned value; plus **one end-to-end test per facade** through the fake
  relay. No network.
- One backlog entry per facade. A facade may ship a subset of its namespace
  actions, provided a follow-up ticket lists the remaining ones (still reachable
  through `device.Channel.CallAsync`).

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Single shared `JsonSerializerContext` for all facades | One ever-growing type touched by every feature (merge conflicts, no isolation); a facade's serialization options could not diverge if an API namespace requires it; per-facade contexts keep trimming granular. |
| Strict enums (unknown value throws) | Any new value added by JDownloader would break reading of a whole list response for existing library versions. |
| Enums exposed as `string` | Loses typing and discoverability; pushes magic strings to every caller; values cannot be validated before the call. |
| Query flags as `bool?` (API-faithful, omitted when null) | The API returns only requested fields, so a default query would return near-empty records; the caller would have to know and set every flag. Non-nullable flags defaulting to `true` give complete results by default while keeping secrets opt-in. |
| All facade types in the root `MyJdownloader` namespace | Name collisions across namespaces (`CrawledLink` vs download links, `...Query` types); flat, noisy IntelliSense; no clear ownership per feature. |
| Source generator producing facades | Already rejected in ADR-001 (no reliable machine-readable API description, harder to stabilize the public surface). |

## Consequences

Positive:

- Uniform, discoverable public surface across T-010..T-019; each new facade is a
  repeatable recipe.
- Forward compatibility: unknown enum values and fields do not break reading.
- AOT/trimming compatibility preserved (source generation only, no reflection).
- Secrets are opt-in on queries and never exposed through `ToString()`.
- Invalid input fails fast, without I/O and without leaking values.
- Facades are testable in isolation, with one end-to-end guard per facade.

Negative:

- More code per facade (one JSON context, converters registration, `PrintMembers`
  overrides, validation) written by hand.
- `Unknown = 0` enum members and default-`true` query flags become part of the
  public contract: changing a default later is a behavioural breaking change.
- Default-`true` queries fetch more data than strictly needed; callers wanting
  lean responses must turn flags off.
- `Unknown` hides which new value the device actually sent (diagnostics limited
  to the raw `CallAsync` path).
- Subset facades leave some actions only reachable through the generic core
  until their follow-up ticket is delivered.
