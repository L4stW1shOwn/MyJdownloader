# ADR-003: Injected `HttpClient` in the core, named client in the DI package

- **Status**: Accepted
- **Date**: 2026-11-02
- **Amended**: 2026-11-06 (T-007)
- **Amended**: 2026-11-06 (T-008)
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§5, §8)

## Context

`MyJDownloaderClient` is thread-safe and designed to be a singleton (it holds the
session and the single-flight reconnection). It must send HTTP requests to the
MyJD relay. Two classic pitfalls:

- a long-lived `HttpClient` without connection rotation does not follow DNS
  changes;
- a **typed** client (`AddHttpClient<T>`) captured in a singleton keeps its
  initial `HttpMessageHandler` and no longer benefits from the handler rotation
  of `IHttpClientFactory`.

The core must depend only on the BCL (ADR-002).

## Decision

**Core**:

- The constructor takes an `HttpClient`:
  `MyJDownloaderClient(HttpClient httpClient, MyJDownloaderOptions? options = null, TimeProvider? timeProvider = null)`.
- The client does not own the `HttpClient` and is not `IDisposable`.
- The XML doc recommends a long-lived `HttpClient` with
  `SocketsHttpHandler.PooledConnectionLifetime` (e.g. 2 min).
- For an `HttpClient` built by the caller (without the DI package), use a
  long-lived `SocketsHttpHandler` with `PooledConnectionLifetime` (e.g. 2 min),
  `AllowAutoRedirect = false` and no HTTP logging, because device-call URL paths
  carry the session token and the device id.
- No convenience constructor creating its own `HttpClient` in v1.

**DI package** (`AddMyJDownloader(o => ...)`):

- Registers a **named client**:
  `AddHttpClient(name).UseSocketsHttpHandler((h, _) => h.PooledConnectionLifetime = TimeSpan.FromMinutes(2)).SetHandlerLifetime(Timeout.InfiniteTimeSpan)`.
  DNS tracking is ensured by `PooledConnectionLifetime`; the handler is never
  recycled by the factory, so its capture in a singleton is safe.
- The handler also sets `AllowAutoRedirect = false` and the builder calls
  `RemoveAllLoggers()`, so no default HTTP logger records token-bearing paths.
- Registers `MyJDownloaderClient` as a **singleton**, built with
  `sp.GetRequiredService<IHttpClientFactory>().CreateClient(name)`,
  `IOptions<MyJDownloaderOptions>` and `TimeProvider` (resolved if registered,
  otherwise `TimeProvider.System`), registered with `TryAddSingleton` and an
  optional `TimeProvider` (`sp.GetService<TimeProvider>()`).
- Returns `IHttpClientBuilder` so the caller can add its own resilience
  (`AddStandardResilienceHandler`, custom handlers).

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| `IHttpClientFactory` in the core's constructor | Would impose `Microsoft.Extensions.Http` on the core (contrary to ADR-002). |
| Convenience constructor creating an internal `HttpClient` | Ambiguous ownership and lifetime (dispose, handler configuration); dropped for v1. |
| Typed client (`AddHttpClient<MyJDownloaderClient>`) | A typed client captured in a singleton no longer follows handler rotation. |
| Named client with the default handler lifetime | The singleton would capture a handler that the factory considers expired; `SetHandlerLifetime(InfiniteTimeSpan)` + `PooledConnectionLifetime` is the consistent combination. |

## Consequences

Positive:

- Core testable with a fake `HttpMessageHandler`, without external dependency.
- Safe DI usage with respect to DNS and singleton capture.
- The caller keeps control over resilience and the HTTP pipeline.

Negative:

- In standalone use, correct configuration of the `HttpClient` is the caller's
  responsibility (mitigated by the XML doc and the README).
- Handler rotation of the factory is intentionally disabled for this client:
  only `PooledConnectionLifetime` ensures DNS tracking.
- Device calls carry the session token and the device id in the URL path, which
  `HttpClient` logging/tracing does not redact (only the query string is). The
  named client registered by T-008 must remove the default HTTP loggers
  (`RemoveAllLoggers`) or redact the path; the effect on tracing (no built-in
  request logs) must be documented.
- `System.Net.Http` EventSource (`pathAndQuery`) and the `Activity` tag
  `url.full` redact only the query: the session token and device id in the path
  can leak through tracing; callers must exclude this client's traces or add a
  redacting enricher.
- `AddLogger`/`AddDefaultLogger`/extended HTTP logging on the returned builder
  re-enable the logging of token-bearing paths.
- A `MyJDownloaderClient` registered before `AddMyJDownloader` is kept
  (`TryAdd`) and bypasses these protections.
- `HttpClientName` is reserved for the library.
- Replacing or reconfiguring the primary handler through the returned builder
  (`ConfigurePrimaryHttpMessageHandler`, `UseSocketsHttpHandler`, another
  `AddHttpClient(HttpClientName)`) must keep `AllowAutoRedirect = false`. A
  non-`SocketsHttpHandler` primary handler set by `ConfigureHttpClientDefaults`
  is replaced by the library's `SocketsHttpHandler` (caller proxy/certificate
  settings on it are lost).
