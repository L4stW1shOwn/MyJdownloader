# DownloadEvents

The downloadevents namespace sets how often the `downloads.*` interval events are pushed to an event subscription. You create the subscription with the [Events](events.md) facade, then set its interval here.

**Coverage:** 1/2 actions of `/downloadevents`. Not exposed: `queryLinks` (see [Not exposed](#not-exposed)).

Reach it as `device.DownloadEvents`, which returns a `DownloadEventsApi`. The C# namespace is `MyJdownloader.DownloadEvents`, and the device API path is `/downloadevents`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Note.** The call only changes how often events are pushed to a subscription that already exists. It does not create or end a subscription, and it does not read events.

## DownloadEventsApi

`public sealed class DownloadEventsApi`

Typed facade over the `/downloadevents` namespace of one device (the push interval of the download events).

| Member | Signature | Description |
|---|---|---|
| `SetStatusEventIntervalAsync` | `Task<bool> SetStatusEventIntervalAsync(long subscriptionId, TimeSpan interval, CancellationToken cancellationToken = default)` | Sets the push interval of the `downloads.*` interval events for one event subscription (`/downloadevents/setStatusEventInterval`). Returns `true` when the interval is set, and `false` when the subscription is unknown or its channel is closed. Throws `ArgumentOutOfRangeException` when `interval` is shorter than 1 millisecond, before any request. Throws `MyJdProtocolException` when the response holds no value. |

Remarks:

- `subscriptionId` is the `SubscriptionId` of the `EventSubscription` returned by `device.Events.SubscribeAsync`.
- The interval is sent as whole milliseconds: the fraction of a millisecond is truncated. An interval of 1.5 ms is sent as 1 ms, and an interval below 1 ms, such as 0.999 ms, is rejected.
- The device default is 1000 ms.
- The interval applies until the subscription ends.
- The call sets a value rather than toggling one. Retrying it after a timeout is harmless, but a `false` result means the subscription is gone: subscribe again before you listen.

### Example

Subscribe to the download events, then push the interval events every 2 seconds instead of the default 1 second.

```csharp
using System;
using MyJdownloader.Events;

EventSubscription subscription = await device.Events.SubscribeAsync(
    ["downloads.*"],
    cancellationToken: cancellationToken);

bool applied = await device.DownloadEvents.SetStatusEventIntervalAsync(
    subscription.SubscriptionId,
    TimeSpan.FromSeconds(2),
    cancellationToken);

if (!applied)
{
    // The subscription is unknown or closed: subscribe again before you set the interval.
}
```

After this call, listen to the subscription as described in the [Events guide](../guides/events.md). The interval events arrive at the new rate.

## Not exposed

`queryLinks` is not exposed: the device always fails it with `Not Implemented`, so a typed method would always fail.

See also:

- [Events](events.md)
- [client.md](client.md)
- [errors.md](errors.md)
- [Events guide](../guides/events.md)
