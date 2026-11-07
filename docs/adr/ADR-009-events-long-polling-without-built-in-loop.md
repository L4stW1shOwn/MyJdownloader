# ADR-009: Events long polling without a built-in loop

- **Status**: Accepted
- **Date**: 2026-11-07
- **Reference**: `docs/superpowers/specs/2026-11-07-t019-events-design.md`

## Context

T-019 delivers the `/events` device namespace. Unlike the other facades, it is
a **subscription + long-polling** API: the caller subscribes to publishers
(`subscribe`, `changesubscription`, `addsubscription`, ...), receives a
subscription id, then repeatedly calls `listen(subscriptionId)`, which blocks
on the device until events arrive or the subscription poll timeout expires. A
subscription expires on the device when it is not polled within its keep-alive
delay.

This shape invites a high-level "event stream" abstraction (background loop,
automatic resubscription, retry on transient failures), which collides with
existing decisions:

- **ADR-005**: no built-in network retry; resilience belongs to the caller.
- **ADR-003**: the core receives a single injected `HttpClient`; the library
  never creates or reconfigures clients.
- **ADR-007**: one stateless, typed facade per namespace, built on
  `IDeviceChannel.CallAsync<T>`.
- **ADR-008**: untyped API values are exposed as `JsonElement`.
- **Transport** (`RelayTransport`): every call, body included, is bounded by
  `HttpClient.Timeout` (exceeding it raises `TimeoutException`), and response
  bodies are capped at 1 MiB.

The `listen` response shape and the units of the subscription durations are not
documented by the MyJD API and cannot be verified without a real device.

## Decision

Low-level typed facade only:

- `/events` is exposed as **`EventsApi`** (ADR-007 conventions), with 10
  methods mapping the namespace actions, including
  **`ListenAsync(subscriptionId)`**, which returns **one batch** of
  `DeviceEvent`.
- **No built-in listen loop, no automatic resubscription, no retry, no
  background task.** The caller owns the loop and the subscription lifetime,
  and is expected to unsubscribe in a `finally` block. This is consistent with
  ADR-005.

Timeouts:

- A `ListenAsync` call is an **ordinary channel call**: bounded by
  `HttpClient.Timeout` and by the 1 MiB response cap. The library neither
  raises nor bypasses `HttpClient.Timeout` for listen.
- The subscription poll timeout must stay **well below** `HttpClient.Timeout`
  (guidance: e.g. 25 s against the default 100 s); XML doc on `ListenAsync`
  and the subscription records states this rule.
- An **expired subscription** surfaces as a `MyJdApiException` (device-side
  404); the caller handles it by resubscribing.

Payloads and wire assumptions:

- Event payloads are **untyped** and exposed as `JsonElement` (ADR-008).
- The `listen` response is read as a list of `{publisher, eventid, eventData}`
  objects with **case-insensitive property names**, since the shape and casing
  are undocumented.
- Durations (`polltimeout`, `maxkeepalive`, `maxPolltimeout`, `maxKeepalive`)
  are assumed to be **milliseconds on the wire** and are exposed as `TimeSpan`.
  This deviates from the ADR-007 default (durations in seconds) for this
  namespace only.
- **Pending confirmation**: the response shape and the duration units must be
  confirmed by the T-031 live tests (`[Category("Live")]`). A mismatch is fixed
  in a patch release and recorded as an amendment to this ADR.

Follow-up:

- A high-level `IAsyncEnumerable<DeviceEvent>` stream helper is **deferred to
  T-045**; it will have to define its lifecycle and failure policy explicitly,
  in line with ADR-005.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Built-in `IAsyncEnumerable` event stream in v1 | Forces lifecycle and retry policy choices (resubscribe on expiry, back-off on network errors, behaviour on device offline or rate limit) that ADR-005 leaves to the caller; a hidden loop is harder to cancel, test and reason about. Deferred to T-045 as an opt-in helper over the low-level facade. |
| Dedicated longer timeout or separate `HttpClient` for listen | Breaks ADR-003 (single injected `HttpClient`, configured by the caller); introduces hidden, call-specific behaviour; bypassing `HttpClient.Timeout` would leave a hung listen unbounded. Keeping the poll timeout below `HttpClient.Timeout` achieves the same result with the caller's own configuration. |
| Raw `JsonElement` for the whole listen response | Loses the typing of the stable parts (publisher, event id); every caller would re-parse the envelope. Only the event payload is untyped. |
| Strict (case-sensitive) property-name matching | The response shape is undocumented; an unexpected casing would silently yield empty events or break reading for existing library versions. |

## Consequences

Positive:

- Consistent with ADR-003, ADR-005, ADR-007 and ADR-008: no new HTTP, retry or
  threading machinery in the core; `ListenAsync` behaves like any other call
  (cancellation, timeouts, exceptions).
- The caller keeps full control over the loop, cancellation, resilience and
  subscription lifetime; nothing runs in the background.
- Small, stable public surface; a stream helper can be added later (T-045) as a
  backward-compatible minor addition built on the same facade.
- Tolerant reading of an undocumented response.

Negative:

- Caller boilerplate: every consumer writes its own subscribe / listen loop /
  resubscribe-on-404 / unsubscribe-in-`finally` code, with the risk of leaking
  device subscriptions if the `finally` is forgotten.
- No ready-to-use event stream until T-045.
- Misconfiguration risk: a poll timeout close to or above `HttpClient.Timeout`
  turns idle polls into `TimeoutException`s; the library only documents the
  rule, it does not enforce it.
- An expired subscription is reported as a generic `MyJdApiException` (404),
  not a dedicated exception type; callers must filter on it.
- The response shape and millisecond units are assumptions until T-031; a
  mismatch requires a behavioural fix.
- Case-insensitive reading of `DeviceEvent` diverges from the camelCase-only
  convention of the other facade contexts.
- Event payloads are `JsonElement`, with the ADR-008 drawbacks (no
  compile-time typing, no structural equality).
