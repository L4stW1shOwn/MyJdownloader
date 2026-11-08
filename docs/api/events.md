# Events

The events namespace subscribes to JDownloader's event publishers (listed by `ListPublishersAsync`) and receives the queued events through a long poll. Use it when a service needs to react to changes on the device without polling every model.

**Coverage:** 10/10 actions of `/events` (complete).

Reach it as `device.Events`, which returns an `EventsApi`. The C# namespace is `MyJdownloader.Events`, and the device API path is `/events`.

The low-level methods have no built-in listen loop ([ADR-009](../adr/ADR-009-events-long-polling-without-built-in-loop.md)): the caller calls `ListenAsync` repeatedly, handles cancellation, and re-subscribes when the subscription expires. `StreamAsync` is an opt-in helper that does this for you ([ADR-010](../adr/ADR-010-opt-in-event-stream-helper.md)).

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. When a subscription is expired or unknown, the device usually answers `404`, which surfaces as a `MyJdApiException`. The error model is described in [errors.md](errors.md).

## EventsApi

`public sealed class EventsApi`

Typed facade over the `/events` namespace of one device (event subscriptions and long polling).

| Member | Signature | Description |
|---|---|---|
| `SubscribeAsync` | `Task<EventSubscription> SubscribeAsync(IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)` | Creates an event subscription (`/events/subscribe`) for the given patterns. `subscriptions` may be empty. `exclusions` defaults to none, sent as an empty array. |
| `ListenAsync` | `Task<IReadOnlyList<DeviceEvent>> ListenAsync(long subscriptionId, CancellationToken cancellationToken = default)` | Waits for and returns the queued events of a subscription (`/events/listen`). This is a long poll; see the remarks. Returns an empty list when no event arrived. |
| `GetSubscriptionAsync` | `Task<EventSubscription> GetSubscriptionAsync(long subscriptionId, CancellationToken cancellationToken = default)` | Returns a subscription (`/events/getsubscription`). |
| `GetSubscriptionStatusAsync` | `Task<EventSubscriptionStatus> GetSubscriptionStatusAsync(long subscriptionId, CancellationToken cancellationToken = default)` | Returns the status of a subscription (`/events/getsubscriptionstatus`), including the number of queued events. |
| `AddSubscriptionAsync` | `Task<EventSubscription> AddSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)` | Adds patterns to a subscription (`/events/addsubscription`). |
| `RemoveSubscriptionAsync` | `Task<EventSubscription> RemoveSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)` | Removes patterns from a subscription (`/events/removesubscription`). |
| `SetSubscriptionAsync` | `Task<EventSubscription> SetSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)` | Replaces the patterns of a subscription (`/events/setsubscription`). |
| `ChangeSubscriptionTimeoutsAsync` | `Task<EventSubscription> ChangeSubscriptionTimeoutsAsync(long subscriptionId, TimeSpan pollTimeout, TimeSpan maxKeepalive, CancellationToken cancellationToken = default)` | Changes the poll timeout and keep-alive duration of a subscription (`/events/changesubscriptiontimeouts`). Both values must be at least 1 ms, otherwise `ArgumentOutOfRangeException` is thrown. Sent as whole milliseconds. |
| `UnsubscribeAsync` | `Task<EventSubscription> UnsubscribeAsync(long subscriptionId, CancellationToken cancellationToken = default)` | Cancels a subscription (`/events/unsubscribe`). JDownloader does not document the returned subscription, so some properties may be absent. |
| `ListPublishersAsync` | `Task<IReadOnlyList<EventPublisher>> ListPublishersAsync(CancellationToken cancellationToken = default)` | Lists the event publishers and the events they emit (`/events/listpublisher`). |
| `StreamAsync` | `IAsyncEnumerable<DeviceEvent> StreamAsync(EventStreamOptions options, CancellationToken cancellationToken = default)` | Follows the events as an asynchronous stream: subscribes, resubscribes on expiry, unsubscribes at the end. Lazy; see the stream remarks. |

Remarks:

- Subscription patterns and exclusions are regular expressions evaluated on the device, not by this library. Each item must be non-null, non-blank, and free of control characters and lone surrogates; otherwise `ArgumentException` is thrown.
- `ListenAsync` is a long poll: the device holds the request until events are queued or the subscription poll timeout elapses, then answers with the queued events, possibly none.
- The call is bounded by `HttpClient.Timeout`, which covers the response body. The library neither raises nor bypasses it, so keep the poll timeout well below it. For example, use 25 s with the default 100 s client timeout, through `ChangeSubscriptionTimeoutsAsync`.
- Responses are capped at 1 MiB. The shape of the events is undocumented; it is an assumption to confirm by live tests.
- `SubscribeAsync`, `UnsubscribeAsync` and the other write methods act on the device immediately. A timeout means the action may have been executed.

`StreamAsync` remarks:

- `options` is validated and its lists are copied at the call, so invalid arguments throw at the call, not at the first `MoveNextAsync`: `ArgumentNullException` (null `options` or `Subscriptions`), `ArgumentException` (invalid pattern, or `PollTimeout` not strictly below `MaxKeepalive`), `ArgumentOutOfRangeException` (`PollTimeout` or `MaxKeepalive` below 1 ms, negative `EmptyPollDelay`). No call is made until the stream is enumerated.
- Each enumeration owns one device subscription: enumerating the same stream twice creates two. The subscription is removed when the enumeration ends for any reason, with a token of its own cancelled after 10 s and independent of the caller's token, so a cancelled consumer never waits for `HttpClient.Timeout`; a failure or timeout of that cleanup is ignored, and the device drops the subscription after `MaxKeepalive`. An enumerator obtained manually and never disposed leaks the subscription until `MaxKeepalive`.
- If the caller cancels while the subscribe call is in flight, the device may already have created a subscription the stream does not know about yet; it expires after `MaxKeepalive`.
- A 404 on listen (`MyJdApiException`) triggers a silent resubscription with the same options; events published between the expiry and the resubscription are lost. A 404 on the first listen after a resubscription ends the stream with that exception. Every other exception propagates and ends the stream; there is no network retry.
- Keep `PollTimeout` well below `HttpClient.Timeout` (25 s against the default 100 s); this is documented, not enforced.

### Example

```csharp
using MyJdownloader.Events;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

var subscription = await device.Events.SubscribeAsync(["downloads.*"], cancellationToken: cts.Token);
await device.Events.ChangeSubscriptionTimeoutsAsync(
    subscription.SubscriptionId, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(120), cts.Token);

try
{
    while (true)
    {
        IReadOnlyList<DeviceEvent> events = await device.Events.ListenAsync(subscription.SubscriptionId, cts.Token);
        foreach (DeviceEvent deviceEvent in events)
        {
            // Publisher and EventId are device-controlled: encode them before logging. Data may hold secrets.
            Console.WriteLine($"{deviceEvent.Publisher} / {deviceEvent.EventId}");
        }
    }
}
finally
{
    await device.Events.UnsubscribeAsync(subscription.SubscriptionId, CancellationToken.None);
}
```

Stream example:

```csharp
using MyJdownloader.Events;

var options = new EventStreamOptions { Subscriptions = ["downloads.*"] };

await foreach (DeviceEvent deviceEvent in device.Events.StreamAsync(options, cts.Token))
{
    Console.WriteLine($"{deviceEvent.Publisher} / {deviceEvent.EventId}");
}
```

## EventStreamOptions

`public sealed record EventStreamOptions`

Options of `StreamAsync`: which events to follow and how the device-side subscription is polled. Validated, and the lists copied, when `StreamAsync` is called.

| Property | Type | Description |
|---|---|---|
| `Subscriptions` | `IReadOnlyList<string>` | Required. Event patterns (device-side regular expressions); may be empty. Items follow the rules of `SubscribeAsync`. |
| `Exclusions` | `IReadOnlyList<string>?` | Patterns to exclude; `null` (default) for none. |
| `PollTimeout` | `TimeSpan` | How long the device holds each listen call. At least 1 ms and strictly below `MaxKeepalive`; default 25 s. Keep it well below `HttpClient.Timeout`. |
| `MaxKeepalive` | `TimeSpan` | How long the device keeps the subscription without a listen call. At least 1 ms and strictly above `PollTimeout`; default 2 min. Also the time after which the device drops a subscription never released. |
| `EmptyPollDelay` | `TimeSpan` | Pause after a listen call that returned no event. Zero or more; default 1 s. |

## DeviceEvent

`public sealed record DeviceEvent`

An event received from the device by listening to a subscription.

The shape of the `listen` items is undocumented; it is an assumption to confirm by live tests. Property names are read case-insensitively. `Data` is untyped and may hold anything, including secrets: do not log it. `Publisher` and `EventId` are device-controlled strings: sanitize or encode them before logging or rendering. `ToString()` prints only the JSON kind of `Data`, never its content.

| Property | Type | Description |
|---|---|---|
| `Publisher` | `string?` | Name of the publisher that emitted the event. |
| `EventId` | `string?` | Event identifier (JSON `eventid`). |
| `Data` | `JsonElement?` | Event payload (JSON `eventData`), or `null` when absent or a JSON `null`. May hold a secret. |

## EventPublisher

`public sealed record EventPublisher`

An event publisher and the identifiers of the events it can emit.

| Property | Type | Description |
|---|---|---|
| `Publisher` | `string?` | Publisher name. |
| `EventIds` | `IReadOnlyList<string>?` | Identifiers of the events the publisher emits (JSON `eventids`). |

## EventSubscription

`public sealed record EventSubscription`

An event subscription as reported by the device.

| Property | Type | Description |
|---|---|---|
| `SubscriptionId` | `long` | Opaque identifier of the subscription, to pass to the other methods (JSON `subscriptionid`). |
| `Subscribed` | `bool` | Whether the subscription is active. |
| `Subscriptions` | `IReadOnlyList<string>?` | Subscribed event patterns, each one a regular expression evaluated on the device. |
| `Exclusions` | `IReadOnlyList<string>?` | Excluded event patterns, each one a regular expression evaluated on the device. |
| `MaxPollTimeout` | `TimeSpan?` | Maximum poll timeout, or `null` when unknown (JSON `maxPolltimeout`). Milliseconds on the wire; assumption to confirm by live tests. |
| `MaxKeepalive` | `TimeSpan?` | Maximum keep-alive duration, or `null` when unknown. Milliseconds on the wire; assumption to confirm by live tests. |

## EventSubscriptionStatus

`public sealed record EventSubscriptionStatus`

The status of an event subscription.

| Property | Type | Description |
|---|---|---|
| `SubscriptionId` | `long` | Opaque identifier of the subscription (JSON `subscriptionid`). |
| `Subscribed` | `bool` | Whether the subscription is active. |
| `QueueSize` | `int` | Number of events waiting to be listened to. |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Events guide](../guides/events.md)
