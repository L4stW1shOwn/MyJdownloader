# ADR-004: Immutable exportable session and single-flight automatic reconnection

- **Status**: Accepted
- **Date**: 2026-11-02
- **Amended**: 2026-11-05 (T-003)
- **Amended**: 2026-11-05 (T-005)
- **Amended**: 2026-11-06 (T-006)
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§1, §3, §5, §6)

## Context

The MyJD protocol relies on a session (session token, regain token, derived
encryption tokens). Consumers want to avoid asking for the password at every
startup, but persistence (file, vault, database) depends on their context. The
session token expires: the server answers `TOKEN_INVALID` and the protocol
provides `/my/reconnect` with the regain token. Since the client is a shared
singleton, several concurrent calls may fail at the same time.

## Decision

**Session**:

- `MyJdSession` is a public, immutable `sealed class` (not a `record`: a record
  would expose its members through the generated `ToString()`, `Deconstruct` and
  `with`, and compare `byte[]` by reference). It holds the email, loginSecret,
  deviceSecret, sessionToken, regainToken, serverEncryptionToken and
  deviceEncryptionToken; only `Email` is public, secrets and tokens are internal
  and hidden from the debugger.
- Serialization goes through `ToJson()` / `FromJson()` with a versioned
  (`version: 1`), library-owned JSON format produced by the internal
  source-generated `MyJdJsonContext` (AOT). `FromJson` rejects any invalid,
  unknown or duplicate content with a single data-free `FormatException`;
  `ToString()` masks the email and exposes no secret.
- The client atomically replaces the current reference.
- `ExportSession()` returns the current instance (or `null`); `RestoreSession()`
  replaces the state without any network call.
- `SessionChanged` (`EventHandler<MyJdSessionChangedEventArgs>`, nullable
  `Session`) is raised after connect, reconnect and disconnect (with `null`),
  outside the lock; a version counter drops notifications of superseded
  changes, so callers persist `ExportSession()`; every handler is invoked even
  if one throws (one failure rethrown, several as `AggregateException`).
- Session changes are serialized by a `SemaphoreSlim(1,1)` that T-006's
  single-flight reconnection reuses; reads are lock-free.
- No built-in store: persistence is left to the caller.
- `ConnectAsync` does not keep the password after derivation; only the derived
  keys are kept. The doc states that the session contains keys derived from the
  password and must be stored in a protected way.

**Reconnection**:

- Trigger: a server-sourced `TOKEN_INVALID` only. The device-sourced case is
  decided by T-007, with an explicit bound against forced session rotation.
- `/my/reconnect`, token update, **a single** replay of the original call.
  `TOKEN_INVALID` is never exposed if the reconnect succeeds. The renewal is not
  cancellable by the caller (bounded by `HttpClient.Timeout`); cancellation is
  checked before the replay.
- Rejected renewal (`MyJdAuthenticationException`): the session is cleared only
  if it is still the failed one (compare-and-exchange), `SessionChanged(null)` is
  raised and the error is rethrown; the caller must call `ConnectAsync` again.
  Other renewal failures (server error, rate limit, network) keep the session.
- Handler failures do not prevent the replay; they are rethrown after a
  successful replay.
- Single-flight: `SemaphoreSlim(1,1)`; under the gate, "session already
  changed" is a reference comparison with the failed session; if so, replay
  without reconnecting.
- No cross-account replay: if the restored session belongs to another account,
  the original `TOKEN_INVALID` is rethrown.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Built-in session store (file, `ISessionStore` abstraction) | Storage and protection choices belong to each caller; additional API surface; out of scope for v1. |
| Keep the password to reconnect via `connect` | Retaining a secret in memory is unacceptable; the regain token covers the need. |
| Mutable session | Risk of inconsistent reads under concurrency; immutability + atomic replacement is simpler to reason about. |
| Reconnection without coordination | Multiple concurrent reconnections, cross-invalidation of tokens. |
| Multiple replays | Risk of loops; a single replay is enough, beyond that the failure is surfaced. |
| Public serializable record (caller serializes) | Not AOT-safe for the caller (reflection), format not versioned, secrets exposed as public members. |

## Consequences

Positive:

- Restart without password via `RestoreSession()`; free persistence.
- Transparent token expiration for the caller.
- A single reconnection per expiration, even under heavy concurrency.

Negative:

- The exported session contains keys derived from the password: protecting it
  at rest is the caller's responsibility.
- Calls replayed after reconnection must be safe to reissue once.
- Concurrency complexity to be covered by dedicated tests (single concurrent
  reconnection).
