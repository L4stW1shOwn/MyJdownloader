# Events

The events namespace subscribes to JDownloader's event publishers (listed by `ListPublishersAsync`) and receives the queued events through a long poll. Use it when a service needs to react to changes on the device without polling every model.

Reach it as `device.Events`, which returns an `EventsApi`. The C# namespace is `MyJdownloader.Events`, and the device API path is `/events`.

There is no built-in listen loop ([ADR-009](../adr/ADR-009-events-long-polling-without-built-in-loop.md)): the caller calls `ListenAsync` repeatedly, handles cancellation, and re-subscribes when the subscription expires.

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
| `UnsubscribeAsync` | `Task<EventSubscription> UnsubscribeAsync(long subscriptionId, CancellationToken cancellationToken = default)` | Cancels a subscription (`/events/unsubscribe`). The content of the returned subscription is unconfirmed until live tests. |
| `ListPublishersAsync` | `Task<IReadOnlyList<EventPublisher>> ListPublishersAsync(CancellationToken cancellationToken = default)` | Lists the event publishers and the events they emit (`/events/listpublisher`). |

Remarks:

- Subscription patterns and exclusions are regular expressions evaluated on the device, not by this library. Each item must be non-null, non-blank, and free of control characters and lone surrogates; otherwise `ArgumentException` is thrown.
- `ListenAsync` is a long poll: the device holds the request until events are queued or the subscription poll timeout elapses, then answers with the queued events, possibly none.
- The call is bounded by `HttpClient.Timeout`, which covers the response body. The library neither raises nor bypasses it, so keep the poll timeout well below it. For example, use 25 s with the default 100 s client timeout, through `ChangeSubscriptionTimeoutsAsync`.
- Responses are capped at 1 MiB. The shape of the events is undocumented; it is an assumption to confirm by live tests.
- `SubscribeAsync`, `UnsubscribeAsync` and the other write methods act on the device immediately. A timeout means the action may have been executed.

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
