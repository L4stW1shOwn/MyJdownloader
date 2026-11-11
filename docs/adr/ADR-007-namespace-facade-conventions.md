# ADR-007: Namespace facade conventions

- **Status**: Accepted
- **Date**: 2026-11-06
- **Amended**: 2026-11-06 (T-011)
- **Amended**: 2026-11-11 (T-060, length caps on caller strings and device-call arguments)
- **Amended**: 2026-11-11 (T-066, no implicit "everything" on empty selections)

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
- **Shared validation/result helpers** live in the internal
  `MyJdownloader.Facades` namespace, so facades do not duplicate them.

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
- **API values typed `Object`** (untyped) follow ADR-008: exposed as
  `JsonElement`, written as their JSON text, never printed by `ToString()`.

Dates and durations (amendment, T-011):

- **Epoch milliseconds** returned by the API are exposed as `DateTimeOffset?`.
- **Durations expressed in seconds** are exposed as `TimeSpan?`.
- The conversions are done by **internal converters** in the facade's JSON
  context; the public records never carry raw numeric timestamps or durations.
- **Sentinel values on read**: for dates, a value `<= 0` (or JSON `null`) reads
  as `null`; for durations, a negative value (or JSON `null`) reads as `null`,
  while `0` is a valid `TimeSpan.Zero`.
- **Writing uses the same units**: milliseconds since the Unix epoch for dates,
  whole seconds for durations.

Responses (`data` absent or `null`):

| Result kind | Behaviour |
|---|---|
| List | Empty list |
| Scalar (`bool`, `int`, ...) or required object | `MyJdProtocolException` (device response unreadable) |
| Result-less method | Ignored |

Validation and secrets:

- **Synchronous validation before any call**: null arguments
  (`ArgumentNullException`), blank strings, over-long strings and control
  characters (incl. CR/LF), ranges, `Unknown` enum values (`ArgumentException`
  / `ArgumentOutOfRangeException`). Messages name the parameter but never carry
  its value. Facade methods are not `async`: they validate, build the
  arguments, then return the channel task (or a small continuation to shape the
  result), so invalid input fails before any I/O.
- **Secret masking**: records holding a secret override `PrintMembers` so that
  `ToString()` shows `***` when the secret is set. Value equality is unchanged.
  Some API responses return secrets without an opt-out flag (for example a
  package `downloadPassword`): the XML doc of such members and queries must say so.

Length caps (amendment, T-060):

- **Caller strings have a maximum length per category**, counted in UTF-16
  code units (`string.Length`):

  | Category | Max | Covers |
  |---|---|---|
  | Identifier | 1 024 | identifiers, keys, types, names (file, package, hoster, user), formats, host names, site keys |
  | Path | 32 767 | file system paths (Windows extended-length maximum; covers Linux `PATH_MAX` 4 096) |
  | Long text | 65 536 | URLs, captcha results and tokens, passwords, regex and event subscription patterns, free text |

- The length check runs **after the null check and before the character
  scan** (a huge value is rejected without being scanned). Over the cap:
  `ArgumentException` naming the argument, thrown synchronously before any
  channel call, like every other check.
- **`maxLength` is required**: `FacadeGuard.ValidateText`,
  `ValidateOptionalText` and `ValidateUrlSafeToken` take a mandatory
  `maxLength` (no default); `ValidateHostName` uses the identifier cap. Every
  new facade call site must pick a category explicitly.
- **Device calls are also capped as a whole**: the arguments of one
  `IDeviceChannel.CallAsync` call must not exceed 1 024 in number nor 1 048 576
  UTF-16 code units in total (a non-string argument counts its escaped JSON
  text, plus 3 per argument for quotes and separator). The cap bounds the
  request, not the HTTP body size (escaping and encryption expand it); otherwise `ArgumentException` on `args`, before serialization,
  encryption and any HTTP call. This mirrors the 1 MiB response cap and covers
  lists and direct `CallAsync` callers that bypass the facades.

Empty selections on destructive actions (amendment, 2026-11-11, T-066):

- **An empty selection never silently means "everything"** on a
  selection-based destructive action (T-042 audit, finding L2, CWE-1188).
  With `CleanupMode.RemoveLinksAndDeleteFiles`, `Unselected` with no
  identifier deletes every file on the device, and the library adds no
  confirmation step: an accidental empty selection must fail fast.
- `CleanupAsync` (Downloads and LinkGrabber facades) rejects
  `SelectionType.Unselected` when both `linkIds` and `packageIds` are empty:
  `ArgumentException` on `selectionType`, fixed message "Unselected with no
  identifier targets every item; use SelectionType.All explicitly.", thrown
  synchronously before any channel call. Targeting every item requires
  `SelectionType.All` explicitly.
- Nothing is published yet, so the behaviour change is free; it is listed in
  `CHANGELOG.md` under `[Unreleased]` / Changed. Every future selection-based
  destructive action follows the same rule.

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
| Raw `long` timestamps and durations | Loses typing; every caller must convert epoch milliseconds or seconds and handle sentinel values themselves. |
| Single 256 cap on caller strings (T-060) | Rejects legitimate values: captcha tokens, file system paths and URLs routinely exceed it. |
| Single 65 536 cap on caller strings (T-060) | Too weak a bound: a 60 000-character config key or identifier would still pass. |
| Transport cap on request arguments alone (T-060) | Late, generic error that does not name the faulty argument; kept only as a complement. |
| Optional `maxLength` with a default (T-060) | New call sites would silently inherit a possibly wrong cap; a required parameter forces the decision. |

## Consequences

Positive:

- Uniform, discoverable public surface across T-010..T-019; each new facade is a
  repeatable recipe.
- Forward compatibility: unknown enum values and fields do not break reading.
- AOT/trimming compatibility preserved (source generation only, no reflection).
- Secrets are opt-in on queries and never exposed through `ToString()`.
- Invalid input fails fast, without I/O and without leaking values.
- Facades are testable in isolation, with one end-to-end guard per facade.
- Dates and durations are typed consistently across facades, with sentinels
  normalized to `null`.
- Absurdly long caller strings and oversized device calls are rejected early
  with a precise `ArgumentException`, bounding request memory and CPU use
  (T-051 audit, CWE-1284), without rejecting real-world values.

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
- Enums used by the `cleanup` action of several namespaces (cross-facade enums)
  live in the root `MyJdownloader` namespace rather than with their facade.
- The length caps (per category and the 1 048 576 request cap) are part of the
  public contract: lowering one later is a behavioural breaking change. The
  per-call-site classification table lives in the T-060 spec
  (`docs/superpowers/specs/2026-11-11-t060-text-length-caps-design.md`), not
  in this ADR; each new call site must be classified the same way.
