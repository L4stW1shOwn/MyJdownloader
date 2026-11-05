# ADR-004: Immutable exportable session and single-flight automatic reconnection

- **Status**: Accepted
- **Date**: 2026-11-02
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

- `MyJdSession` is a public, immutable and serializable `record`: email,
  loginSecret, deviceSecret, sessionToken, regainToken, serverEncryptionToken,
  deviceEncryptionToken.
- The client atomically replaces the current reference.
- `ExportSession()` returns the current instance (or `null`); `RestoreSession()`
  replaces the state without any network call.
- `SessionChanged` is raised after each successful connection/reconnection so
  that the caller can persist the new state.
- No built-in store: persistence is left to the caller.
- `ConnectAsync` does not keep the password after derivation; only the derived
  keys are kept. The doc states that the session contains keys derived from the
  password and must be stored in a protected way.

**Reconnection**:

- On `TOKEN_INVALID`: `/my/reconnect`, token update, **a single** replay of the
  original call. `TOKEN_INVALID` is never exposed if the reconnect succeeds.
- Reconnect failure ⇒ `MyJdAuthenticationException`; the caller must call
  `ConnectAsync` again.
- Single-flight: `SemaphoreSlim(1,1)`; after acquisition, if the session token
  has already changed since the failure, replay without reconnecting.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Built-in session store (file, `ISessionStore` abstraction) | Storage and protection choices belong to each caller; additional API surface; out of scope for v1. |
| Keep the password to reconnect via `connect` | Retaining a secret in memory is unacceptable; the regain token covers the need. |
| Mutable session | Risk of inconsistent reads under concurrency; immutability + atomic replacement is simpler to reason about. |
| Reconnection without coordination | Multiple concurrent reconnections, cross-invalidation of tokens. |
| Multiple replays | Risk of loops; a single replay is enough, beyond that the failure is surfaced. |

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
