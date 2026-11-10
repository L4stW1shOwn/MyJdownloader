# Events

A device publishes events (download progress, state changes, and so on) that
you can receive through an event subscription. The subscription is consumed by
long polling: `ListenAsync` waits on the device until events are queued or the
poll timeout elapses, then returns them.

The easiest way to follow events is `StreamAsync`, an opt-in helper that owns the
subscription lifecycle and exposes the events as an `IAsyncEnumerable<DeviceEvent>`
(see [ADR-010](../adr/ADR-010-opt-in-event-stream-helper.md)). The low-level calls
remain available when you need full control: there is no listen loop in them, no
automatic resubscription and no retry (see
[ADR-009](../adr/ADR-009-events-long-polling-without-built-in-loop.md)).

## Event stream

```csharp
using MyJdownloader.Events;

var options = new EventStreamOptions
{
    Subscriptions = ["downloads.*"],
};

await foreach (DeviceEvent evt in device.Events.StreamAsync(options, cancellationToken))
{
    // Publisher and EventId are device-controlled: encode them before logging. Data may hold secrets.
    Console.WriteLine($"{evt.Publisher}/{evt.EventId}");
}
```

Nothing happens until the sequence is enumerated. On the first `MoveNextAsync`
the stream subscribes and sets the subscription timeouts, then long-polls and
yields the events in the order received. When the enumeration ends for any reason
(`break`, cancellation, exception, `DisposeAsync`), the stream unsubscribes with
its own token, cancelled after 10 seconds and independent of yours, so a
cancelled caller still releases the subscription without blocking for up to
`HttpClient.Timeout`. The cleanup always returns within 10 s: if the
unsubscribe is not done by then it is abandoned (a session reconnect already in
flight may finish in the background on the client) and the device drops the
subscription after `MaxKeepalive`.
Nothing runs between two `MoveNextAsync` calls: no background task, no timer.

### Options

| Property         | Default | Meaning                                                                 |
|------------------|---------|-------------------------------------------------------------------------|
| `Subscriptions`  | required | Event patterns (device-side regular expressions); may be empty.        |
| `Exclusions`     | none    | Event patterns to exclude (device-side regular expressions).            |
| `PollTimeout`    | 25 s    | How long the device holds each listen call. At least 1 ms, strictly below `MaxKeepalive`. |
| `MaxKeepalive`   | 2 min   | How long the device keeps the subscription without a listen call. At least 1 ms. |
| `EmptyPollDelay` | 1 s     | Pause after a listen call that returned no event; zero for no pause.    |

The options are validated, and the lists copied, when `StreamAsync` is called:
invalid arguments throw at the call, not at the first `MoveNextAsync`, and later
changes to the options have no effect on a stream already created.

### Behaviour to know

- **One subscription per enumeration.** Enumerating the same sequence twice
  creates two device subscriptions.
- **Lost events.** Events published between a subscription expiry and the
  resubscription are lost, and the caller is not told about the resubscription.
- **Undisposed enumerator.** `await foreach` disposes the enumerator for you. If
  you call `GetAsyncEnumerator` yourself and never dispose it, the device
  subscription lives until `MaxKeepalive`.
- **Cancellation during subscribe.** If you cancel while the subscribe call is in
  flight, the device may already have created a subscription the stream does not
  know about yet. It expires after `MaxKeepalive`.
- **Best-effort cleanup.** A failure of the final unsubscribe is ignored so it
  never masks the exception that ended the stream; the device drops the
  subscription after `MaxKeepalive`.
- **`PollTimeout` versus `HttpClient.Timeout`.** Each listen call is an ordinary
  call bounded by `HttpClient.Timeout` (100 s by default). Keep `PollTimeout`
  well below it (25 s by default). The library cannot read that timeout, so this
  is documented, not enforced.

### Failure policy

| Situation                                              | Result                                                        |
|--------------------------------------------------------|---------------------------------------------------------------|
| 404 on listen (`MyJdApiException`, subscription expired or unknown) | The stream resubscribes silently with the same options and keeps listening. |
| 404 on the first listen after a resubscription         | The stream ends with that `MyJdApiException` (no endless resubscribe loop). |
| Failure while setting the timeouts at start            | The new subscription is unsubscribed; the exception propagates. |
| Anything else (`TimeoutException`, `HttpRequestException`, `MyJdRateLimitException`, `MyJdDeviceOfflineException`, `OperationCanceledException`, ...) | Propagates unchanged and ends the stream. There is no retry. |

Add your own back-off and restart policy around the `await foreach` if you want
to survive transient failures. See [Error handling](error-handling.md).

## Advanced: manual loop

Use the low-level calls when you need the subscription id, a custom resubscription
policy or full control. There is no built-in loop in them: you own the loop, its
cancellation and its error policy.

### The calls

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

### Caller-owned loop

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

## Push interval of the download events

`device.DownloadEvents` sets how often the `downloads.*` interval events are
pushed to a subscription. The subscription is the one you create with the calls
above: `StreamAsync` owns its subscription and does not expose its identifier,
so use the manual calls for this.

```csharp
using System;
using MyJdownloader.Events;

EventSubscription subscription = await device.Events.SubscribeAsync(
    ["downloads.*"],
    cancellationToken: cancellationToken);

// Push the downloads.* interval events every 2 seconds (the device default is 1 second).
bool applied = await device.DownloadEvents.SetStatusEventIntervalAsync(
    subscription.SubscriptionId,
    TimeSpan.FromSeconds(2),
    cancellationToken);
```

- `false` means the subscription is unknown or closed: subscribe again before you
  listen.
- The interval is sent as whole milliseconds, and a fraction is truncated. An
  interval below 1 ms is rejected with `ArgumentOutOfRangeException`.
- The interval applies until the subscription ends.
- The page is [DownloadEvents](../api/downloadevents.md).

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

## Current state without a subscription

If you need the state at one moment, not a stream of changes, `device.Polling`
answers once with the requested events and creates no subscription, so there is
nothing to clean up:

```csharp
using MyJdownloader.Polling;

IReadOnlyList<PollingResult> results = await device.Polling.PollAsync(cancellationToken: cancellationToken);
```

A poll does not report the changes that happen between two calls. The payloads
of each event are described in [Polling](../api/polling.md).
