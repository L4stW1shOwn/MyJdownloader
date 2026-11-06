# ADR-001: Hand-written typed facade over a public generic core

- **Status**: Accepted
- **Date**: 2026-11-02
- **Amended**: 2026-11-06 (T-007)
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§1, §3, §7)

## Context

The MyJDownloader device API exposes many namespaces (`/linkgrabberv2`,
`/downloadsV2`, `/config`, `/captcha`, `/accounts`, `/extensions`, `/events`,
`/update`, `/system`, `/jd`, etc.). The library aims for full coverage over
time, delivered namespace by namespace. The success criterion requires that any
consumer can call **any** device action in a typed way, including those not yet
covered by the facade.

The library is public (nuget.org): the API surface must remain minimal, stable,
readable, and compatible with AOT/trimming.

## Decision

Two levels of public API:

1. **Public generic core**: `IDeviceChannel.CallAsync<T>(string path,
   JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken)`
   (`object?` arguments would need reflection, incompatible with
   `IsAotCompatible` / ADR-002). A result-less `CallAsync` overload (no
   `responseType`) is added up front, because adding interface members later is
   a breaking change.
   It allows calling any device action, with typed deserialization that is
   AOT-compatible via `JsonTypeInfo<T>`. The `DeviceChannel` implementation
   remains `internal`.
2. **Hand-written typed facade**: `JDownloaderDevice` exposes a lazy property
   per namespace (`LinkGrabber`, `Downloads`, ...). Each namespace is a `sealed`
   class with an internal constructor, built on top of `IDeviceChannel`.

Facade conventions:

- `async` methods with `CancellationToken`;
- requests/responses as `record`, request options as `bool?` as in the API;
- .NET naming (`AddLinksAsync`), original path (`/linkgrabberv2/addLinks`)
  cited in XML doc;
- the skeleton implements no namespace: one namespace = one backlog entry = one
  `/feature` cycle.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Source generator producing the facade from an API description | No official and reliable machine-readable description of the API; disproportionate tooling complexity; finer control over naming, types and XML doc is harder; generated public surface is harder to stabilize with PublicApiAnalyzers. |
| Generic core alone + extension methods on `IDeviceChannel` | No grouping by namespace (poor IntelliSense discoverability on a flat surface); extensions on a public interface freeze more surface and complicate evolution without breaking changes. |

## Consequences

Positive:

- Any device action is callable from v1 via `CallAsync<T>`, even before the
  facade covers its namespace.
- Idiomatic, documented .NET facade, discoverable by namespace.
- Namespaces are tested in isolation by substituting `IDeviceChannel`
  (NSubstitute): path and arguments verified without network.
- Incremental delivery, one namespace per cycle.

Negative:

- Facade code is written and maintained by hand for each action.
- `IDeviceChannel` becomes a public contract: any signature change is a breaking
  change (major, strict semver).
- Risk of divergence between the facade and the real API, mitigated by the
  original path reference in XML doc and the per-namespace tests.
