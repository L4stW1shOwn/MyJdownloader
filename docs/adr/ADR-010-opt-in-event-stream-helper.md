# ADR-010: Opt-in event stream helper

- **Status**: Accepted
- **Date**: 2026-11-08

## Context

[ADR-009](ADR-009-events-long-polling-without-built-in-loop.md) exposes
`/events` as a low-level facade only: `EventsApi.ListenAsync` returns one batch
of `DeviceEvent`, and every caller writes the same loop: subscribe, set the
subscription timeouts, listen repeatedly, resubscribe when the subscription
expires (device-side 404), unsubscribe in a `finally` block. Forgetting the
`finally` leaks device subscriptions until their keep-alive expires. ADR-009
deferred a high-level `IAsyncEnumerable<DeviceEvent>` helper to T-045, on the
condition that its lifecycle and failure policy are defined explicitly.

Constraints the helper must respect:

- **[ADR-005](ADR-005-error-handling.md)**: no built-in network retry or
  back-off; resilience belongs to the caller.
- **[ADR-003](ADR-003-injected-httpclient-and-named-di-client.md)**: a single
  injected `HttpClient`; no separate client and no longer timeout for listen.
- **[ADR-007](ADR-007-namespace-facade-conventions.md)**: stateless typed
  facade per namespace.
- **[ADR-008](ADR-008-untyped-values-as-jsonelement.md)**: event payloads stay
  `JsonElement`.
- **ADR-009**: every call, listen included, is an ordinary channel call bounded
  by `HttpClient.Timeout`; the poll timeout must stay well below it.

## Decision

Add an **opt-in** helper to the existing facade, in namespace
`MyJdownloader.Events`; the low-level methods of ADR-009 are unchanged.

Public API (backward-compatible addition, minor version):

- `EventsApi.StreamAsync(EventStreamOptions options, CancellationToken
  cancellationToken = default)` returning `IAsyncEnumerable<DeviceEvent>`.
- `EventStreamOptions`, a sealed record: `Subscriptions` (required),
  `Exclusions` (optional), `PollTimeout` (default 25 s), `MaxKeepalive`
  (default 2 min), `EmptyPollDelay` (default 1 s).
- `StreamAsync` is a non-iterator method that **validates synchronously**, then
  returns a private `async` iterator: invalid arguments throw at the call, not
  at the first `MoveNextAsync`. Validation reuses the rules of `SubscribeAsync`
  and `ChangeSubscriptionTimeoutsAsync`, requires `PollTimeout` strictly below
  `MaxKeepalive`, and rejects a negative `EmptyPollDelay`. The library cannot
  read `HttpClient.Timeout`: keeping `PollTimeout` well below it stays a
  documented rule, not an enforced one (ADR-009).
- The helper only calls the existing public facade methods (`SubscribeAsync`,
  `ChangeSubscriptionTimeoutsAsync`, `ListenAsync`, `UnsubscribeAsync`): the
  wire behaviour stays in one place, no new JSON type, AOT-compatible.

Lifecycle:

- **One subscription per enumeration.** Each `GetAsyncEnumerator` runs its own
  lifecycle and holds at most one device subscription at a time; enumerating
  the same sequence twice creates two subscriptions (documented).
- **Start** (first `MoveNextAsync`): subscribe, then change the subscription
  timeouts to `PollTimeout` / `MaxKeepalive`.
- **Loop**: listen and yield each event in the received order. After an empty
  batch, wait `EmptyPollDelay` (skipped when zero) to avoid a hot loop.
- **End** (completion for any reason: `break`, cancellation, exception,
  explicit `DisposeAsync`): when a subscription is held, unsubscribe in a
  `finally` block with **`CancellationToken.None`**, so a cancelled caller
  still releases the subscription; the call remains bounded by
  `HttpClient.Timeout`.
- The caller's token (combined with the one from `WithCancellation` through
  `[EnumeratorCancellation]`) flows into every call and delay. **Nothing runs
  between two `MoveNextAsync` calls**: no background task, no timer.

Failure policy (ADR-005):

- **404 on listen** (`MyJdApiException` with `StatusCode == 404`, subscription
  expired or unknown): **resubscribe silently** with the same options (start
  sequence), then keep listening. The old subscription is not unsubscribed
  (the device no longer knows it). Events published between expiry and the new
  subscription are **lost** (documented). Only the status code is checked, not
  the error source, consistent with ADR-009 and the `ListenAsync` XML doc.
- **Second-404 guard**: a 404 on the first listen after a resubscription (no
  successful listen in between) ends the stream with that `MyJdApiException`,
  preventing an endless resubscribe loop.
- **Failure while changing the timeouts at start**: the just-created
  subscription is held, so the `finally` unsubscribes it; the exception
  propagates.
- **Everything else propagates** unchanged and ends the stream, with no retry:
  `TimeoutException`, `HttpRequestException`, `IOException`,
  `MyJdRateLimitException`, `MyJdDeviceOfflineException`,
  `MyJdAuthenticationException`, `MyJdProtocolException`, any other
  `MyJdApiException`, `InvalidOperationException` when disconnected,
  `OperationCanceledException`. Automatic session renewal keeps working as for
  any channel call.
- **Best-effort unsubscribe**: in the `finally`, a failure of the unsubscribe
  call with `MyJdException`, `TimeoutException`, `HttpRequestException`,
  `IOException`, `OperationCanceledException` or `InvalidOperationException`
  is ignored, so it never masks the exception that ended the stream; the device
  drops the subscription after `MaxKeepalive`. Other exception types propagate.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| `DeviceEventStream` class implementing `IAsyncEnumerable<DeviceEvent>` and `IAsyncDisposable`, exposing the `SubscriptionId` | Larger public surface; two lifecycles to document and test (the enumeration and the object's disposal), with ambiguous ownership of the subscription. Callers needing the id keep the low-level facade. |
| Extension method on `EventsApi` in a separate namespace | Less discoverable (requires an extra `using`) for no gain: the helper is part of the events facade and adds no dependency. |
| End the stream on 404 instead of resubscribing | Subscription expiry is the normal, expected failure of long polling; callers would have to rewrite the restart loop the helper exists to remove. |
| Resubscribe and signal it, through a marker item in the sequence or a callback in the options | Larger API (a sum type or a delegate to version and document) for a need not yet expressed; can be added later as a backward-compatible option. Deferred until a real need. |

## Consequences

Positive:

- The usual events loop shrinks to an `await foreach`; the subscription is
  always released without caller code, whatever ends the enumeration.
- Consistent with ADR-003, ADR-005, ADR-007, ADR-008 and ADR-009: no new HTTP,
  retry or threading machinery; the only automatic recovery is the documented
  resubscription on expiry, bounded by the second-404 guard.
- Explicit, testable failure policy (no network needed: the helper is built on
  the substitutable `IDeviceChannel` through the facade).
- Small addition (one method, one options record); the low-level facade remains
  available for callers needing full control.

Negative:

- Events published between a subscription expiry and the resubscription are
  lost, and the caller is not notified of the resubscription.
- Enumerating the same sequence twice creates two device subscriptions; callers
  must be aware of it.
- The best-effort unsubscribe can leave a subscription on the device until
  `MaxKeepalive` when the call fails; the failure is silent.
- The `PollTimeout` below `HttpClient.Timeout` rule is still documented, not
  enforced.
- No retry on transient failures: a network error ends the stream, and the
  caller restarts the enumeration with its own policy (ADR-005).
