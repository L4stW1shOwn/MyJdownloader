# Architecture Decision Records

Structural decisions of MyJdownloader.

| No. | Title | Status | Link |
|---|---|---|---|
| 001 | Hand-written typed facade over a public generic core | Accepted | [ADR-001](ADR-001-typed-facade-over-generic-core.md) |
| 002 | BCL-only, AOT/trimming-compatible core with a separate DI package | Accepted | [ADR-002](ADR-002-bcl-only-aot-core-and-separate-di-package.md) |
| 003 | `HttpClient` injected into the core, named client in the DI package | Accepted | [ADR-003](ADR-003-injected-httpclient-and-named-di-client.md) |
| 004 | Exportable immutable session and single-flight automatic reconnection | Accepted | [ADR-004](ADR-004-immutable-session-and-automatic-reconnection.md) |
| 005 | Error handling through a `MyJdException` hierarchy, without built-in retry | Accepted | [ADR-005](ADR-005-error-handling.md) |
| 006 | Relay-only transport and the protocol's cryptographic constraints | Accepted | [ADR-006](ADR-006-relay-transport-and-cryptographic-constraints.md) |
| 007 | Namespace facade conventions | Accepted | [ADR-007](ADR-007-namespace-facade-conventions.md) |
| 008 | Untyped API values as `JsonElement` | Accepted | [ADR-008](ADR-008-untyped-values-as-jsonelement.md) |
| 009 | Events long polling without a built-in loop | Accepted | [ADR-009](ADR-009-events-long-polling-without-built-in-loop.md) |

Format: `ADR-NNN-title-in-kebab-case.md` (NNN on 3 digits).
