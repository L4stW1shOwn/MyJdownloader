# ADR-005: Error handling through a `MyJdException` hierarchy, without built-in retry

- **Status**: Accepted
- **Date**: 2026-11-02
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§1, §6, §8)

## Context

The success criterion requires explicit errors. The MyJD API returns structured
errors (type as a string, server or device source), and the protocol can fail
locally (decryption, JSON, inconsistent `rid`). The transport can also fail
(network, cancellation). Messages must never disclose any secret.

## Decision

Public exception hierarchy, fed by a single internal mapper
(`MyJdErrorMapper`, API code → exception mapping):

| Exception | Cases |
|---|---|
| `MyJdException` (base, abstract) | `Source` (`Server`/`Device`), `ErrorType` (raw API string), `StatusCode` |
| `MyJdAuthenticationException` | `AUTH_FAILED`, `EMAIL_INVALID`, `EMAIL_FORBIDDEN`, reconnect failure |
| `MyJdDeviceOfflineException` | `OFFLINE` |
| `MyJdRateLimitException` | `TOO_MANY_REQUESTS`, `OVERLOAD`, `MAINTENANCE` (+ `RetryAfter?`) |
| `MyJdApiException` | Other structured error (server or device action) |
| `MyJdProtocolException` | Decryption, invalid JSON, inconsistent `rid` |

Rules:

- Network errors (`HttpRequestException`, etc.) and `OperationCanceledException`
  are propagated **without wrapping**.
- **No built-in network retry**: resilience is left to the caller
  (`AddStandardResilienceHandler`, Polly), through the `IHttpClientBuilder`
  returned by the DI package.
- `TOKEN_INVALID` is never exposed if the reconnect succeeds (ADR-004).
- No exception message or log contains a token, secret, encrypted body, or full
  email address.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| `Result<T>` instead of exceptions | Not idiomatic for a .NET client library; weighs down every facade call. |
| A single exception with a code | Forces the caller to compare strings; the hierarchy allows a targeted `catch` on actionable cases (auth, offline, rate limit). |
| Wrapping network errors and cancellation in `MyJdException` | Breaks .NET conventions and standard resilience policies that filter on these types. |
| Built-in retry | Duplicates and conflicts with the caller's resilience; the retry policy is a choice specific to each context. |

## Consequences

Positive:

- Targeted `catch` on actionable cases; raw `ErrorType` kept for unmapped cases.
- Natural interoperability with standard resilience handlers.
- Centralized, testable mapping.

Negative:

- The caller must configure its own resilience.
- Adding a new public exception type is an API evolution tracked by
  PublicApiAnalyzers; changing the mapping of an existing code may be breaking
  for callers' `catch` blocks.
- Messages deliberately poor in context (no secret, truncated email), which
  limits diagnostics.
