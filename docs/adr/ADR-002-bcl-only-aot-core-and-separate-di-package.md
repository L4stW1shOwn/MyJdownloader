# ADR-002: Core with no non-BCL dependency, AOT/trimming-compatible, separate DI integration

- **Status**: Accepted
- **Date**: 2026-11-02
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§1, §2, §8, §10, §11)

## Context

The package is public and will be consumed in varied contexts: console
applications, hosted services with a DI container, applications published with
Native AOT or trimmed. Every transitive dependency imposed on the consumer is a
cost (version conflicts, size, AOT compatibility). The MyJD protocol only
requires JSON and standard cryptographic primitives (SHA-256, HMAC-SHA256,
AES), all available in the BCL.

## Decision

- **Two packages**:
  - `MyJdownloader` (core): no dependency outside the BCL
    (`System.Text.Json`, `System.Security.Cryptography`). Only
    `Microsoft.CodeAnalysis.PublicApiAnalyzers` is referenced, as an analyzer
    (`PrivateAssets=all`).
  - `MyJdownloader.Extensions.DependencyInjection`: `AddMyJDownloader()`,
    depends on `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options`.
- **AOT/trimming**: `IsAotCompatible=true` on both packages. JSON through source
  generation (`MyJdJsonContext`, internal `JsonSerializerContext`); the public
  generic API receives a `JsonTypeInfo<T>` instead of relying on reflection.
- TFM `net10.0`; `TreatWarningsAsErrors`, `AnalysisMode=All`; package
  validation; public API tracked by `PublicAPI.Shipped.txt` /
  `PublicAPI.Unshipped.txt`.

Allowed inter-project dependencies:

```
MyJdownloader.Extensions.DependencyInjection --> MyJdownloader
MyJdownloader.Tests                          --> MyJdownloader
MyJdownloader.Extensions.DependencyInjection.Tests --> MyJdownloader.Extensions.DependencyInjection
```

Forbidden: `MyJdownloader` references no other project nor any runtime package.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Single package including the DI integration | Would impose `Microsoft.Extensions.*` on all consumers, including those without a DI container. |
| JSON serialization through reflection | Incompatible with trimming/Native AOT (warnings, runtime behaviors). |
| Third-party libraries (Newtonsoft.Json, BouncyCastle, etc.) | Unnecessary: the BCL covers the need; transitive dependencies and AOT incompatibility. |

## Consequences

Positive:

- Minimal footprint for the consumer; no version conflict induced by the core.
- Usable with Native AOT and trimming without warnings.
- Optional DI integration, evolving independently of the core.

Negative:

- Each serialized type (including those of the facade) must be declared in the
  source-generation context; omission = compile-time or runtime error.
- Callers of `CallAsync<T>` with their own types must provide their own
  `JsonTypeInfo<T>` (and therefore their own `JsonSerializerContext`).
- Two packages to version and publish.
