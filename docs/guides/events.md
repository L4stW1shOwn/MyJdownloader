# Events

A device publishes events (download progress, state changes, and so on) that
you can receive through an event subscription. The subscription is consumed by
long polling: `ListenAsync` waits on the device until events are queued or the
poll timeout elapses, then returns them.

The library provides the calls, not the loop. There is no built-in listen loop,
no automatic resubscription and no retry (see
[ADR-009](../adr/ADR-009-events-long-polling-without-built-in-loop.md)). You own
the loop, its cancellation and its error policy.

## The calls

| Method                              | Purpose                                                        |
|-------------------------------------|----------------------------------------------------------------|
| `ListPublishersAsync()`             | Discover the publishers and the event ids they emit.           |
| `SubscribeAsync(subscriptions, exclusions?)` | Create a subscription from event patterns.            |
| `ChangeSubscriptionTimeoutsAsync(id, pollTimeout, maxKeepalive)` | Set how long a poll is held and how long the subscription lives without polls. |
| `ListenAsync(id)`                   | Long poll: returns the queued events (possibly none).          |
| `GetSubscriptionStatusAsync(id)`    | Check whether the subscription is still active and its queue size. |
| `AddSubscriptionAsync`, `RemoveSubscriptionAsync`, `SetSubscriptionAsync` | Change the patterns of an existing subscription. |
| `UnsubscribeAsync(id)`              | Cancel the subscription on the device.                         |

Subscription and exclusion patterns are regular expressions evaluated on the
device, not by this library.

## Caller-owned loop

```csharp
using System.Net;
using MyJdownloader;
using MyJdownloader.Events;

EventSubscription subscription = await device.Events.SubscribeAsync(
    ["downloads.*"],
    cancellationToken: cancellationToken);

// The device holds each poll for up to 25 s. HttpClient.Timeout (100 s by default)
// must stay well above that: the library neither raises nor bypasses it.
await device.Events.ChangeSubscriptionTimeoutsAsync(
    subscription.SubscriptionId,
    TimeSpan.FromSeconds(25),
    TimeSpan.FromSeconds(120),
    cancellationToken);

try
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            IReadOnlyList<DeviceEvent> events = await device.Events.ListenAsync(
                subscription.SubscriptionId,
                cancellationToken);

            if (events.Count == 0)
            {
                // The device can answer at once with nothing: avoid a hot loop.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            foreach (DeviceEvent evt in events)
            {
                Console.WriteLine($"{evt.Publisher}/{evt.EventId}");
            }
        }
        catch (MyJdApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // The subscription expired or is unknown to the device: create a new one.
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            subscription = await device.Events.SubscribeAsync(
                ["downloads.*"],
                cancellationToken: cancellationToken);
            await device.Events.ChangeSubscriptionTimeoutsAsync(
                subscription.SubscriptionId,
                TimeSpan.FromSeconds(25),
                TimeSpan.FromSeconds(120),
                cancellationToken);
        }
    }
}
finally
{
    try
    {
        await device.Events.UnsubscribeAsync(subscription.SubscriptionId, CancellationToken.None);
    }
    catch (MyJdException)
    {
        // Best effort: the subscription may already have expired.
    }
}
```

Points to note in this loop:

- **Cancellation.** The loop stops when `cancellationToken` is cancelled. The
  `ListenAsync` call throws `OperationCanceledException`, which is not caught
  here. The `finally` block unsubscribes with `CancellationToken.None`, so the
  cleanup runs even when the loop was cancelled.
- **Hot loop.** An answer with no events returns at once in some conditions. The
  delay avoids a busy loop.
- **Expired subscriptions.** The device usually answers `404`, which surfaces as
  a `MyJdApiException` with `StatusCode` set to `HttpStatusCode.NotFound`. The loop
  resubscribes. Every other error propagates, so your own policy decides what to
  do with it. See [Error handling](error-handling.md).
- **Network errors.** A `TimeoutException` or `HttpRequestException` ends the
  loop, unless you catch and handle it. Add your own back-off and resubscription
  policy if you want the loop to survive transient failures.
- **Timeouts.** The poll timeout (25 s above) must be well below the HTTP client
  timeout. If you change `HttpClient.Timeout` for the named client, keep it above
  the poll timeout. See [Dependency injection](dependency-injection.md).

## Event payloads

- `DeviceEvent.Publisher` and `DeviceEvent.EventId` are strings sent by the device.
  Sanitize or encode them before you log or render them.
- `DeviceEvent.Data` is untyped (`JsonElement?`) and can contain secrets. Do not log it.
- The shape of the events is not documented by the device. It is to be confirmed
  against a live device. The millisecond durations in the subscription are
  assumptions to confirm too.
- `ListenAsync` responses are capped at 1 MiB.

## Discover the event names

```csharp
IReadOnlyList<EventPublisher> publishers = await device.Events.ListPublishersAsync(cancellationToken);

foreach (EventPublisher publisher in publishers)
{
    Console.WriteLine($"{publisher.Publisher}: {string.Join(", ", publisher.EventIds ?? [])}");
}
```

## Stop the subscription

`UnsubscribeAsync(id)` cancels the subscription on the device. Call it when the
loop ends, as in the `finally` block above. A subscription that is not cancelled
expires on its own after its keep-alive time.
