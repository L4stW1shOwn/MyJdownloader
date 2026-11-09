# Polling

The polling namespace returns a snapshot of the state of the device: download progress, download state, link grabber state, waiting captchas and aggregated numbers. One call answers once with the requested events; it creates no subscription.

**Coverage:** 1/1 actions of `/polling` (complete).

Reach it as `device.Polling`, which returns a `PollingApi`. The C# namespace is `MyJdownloader.Polling`, and the device API path is `/polling`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Event payloads are untyped on the device, so the `data` of each event is exposed as `System.Text.Json.JsonElement` ([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)). All content comes from the device and is untrusted.

## PollingApi

`public sealed class PollingApi`

Typed facade over the `/polling` namespace of one device (state snapshot events).

| Member | Signature | Description |
|---|---|---|
| `PollAsync` | `Task<IReadOnlyList<PollingResult>> PollAsync(PollingQuery? query = null, CancellationToken cancellationToken = default)` | Polls the requested state events (`/polling/poll`). A `null` query requests all five events. Returns one entry per requested event, in the order `downloadProgress`, `jdState`, `linkGrabberState`, `captchasWaiting`, `aggregatedNumbers`; an empty list when the device returns none. Throws `ArgumentException` when no event is enabled. |

### Events

The events are returned in `PollingResult.EventName`. Their `data` is the payload under the `data` property of `PollingResult.EventData`.

| `EventName` | `PollingQuery` flag | `data` |
|---|---|---|
| `downloadProgress` | `DownloadProgress` | The running packages with their speed and their running links. Links are listed only for the packages in `DownloadProgressPackageUuids`, or for every running package when it is `null`; the other running packages are listed without links. The fields are not typed by this library. |
| `jdState` | `JdState` | The download state label, for example `RUNNING`, `IDLE`, `PAUSE` or `STOPPING`. |
| `linkGrabberState` | `LinkGrabberState` | The link checker state: `RUNNING` or `IDLE`. |
| `captchasWaiting` | `CaptchasWaiting` | The waiting captcha jobs, as returned by `/captcha/list`. The job fields are described in [captcha.md](captcha.md). |
| `aggregatedNumbers` | `AggregatedNumbers` | The aggregated download and crawler numbers. The fields are not typed by this library. |

The exact layout of the `data` payloads is not documented by the device API and is to be confirmed by live tests.

### Wire format

`PollAsync` sends one argument to `/polling/poll`: a JSON object that holds only the requested keys. A flag set to `false` is omitted. `downloadProgress` is sent as `null` when `DownloadProgressPackageUuids` is `null`, or as the array of UUIDs otherwise. `DownloadProgressPackageUuids` is not sent when `DownloadProgress` is `false`.

| `PollingQuery` | Argument sent |
|---|---|
| Defaults (`new PollingQuery()`) | `{"downloadProgress":null,"jdState":true,"linkGrabberState":true,"captchasWaiting":true,"aggregatedNumbers":true}` |
| `new PollingQuery { LinkGrabberState = false, CaptchasWaiting = false, AggregatedNumbers = false, DownloadProgressPackageUuids = [1234567890123, 2345678901234] }` | `{"downloadProgress":[1234567890123,2345678901234],"jdState":true}` |
| `new PollingQuery { DownloadProgress = false, LinkGrabberState = false, CaptchasWaiting = false, AggregatedNumbers = false }` | `{"jdState":true}` |

### Example

```csharp
using System.Text.Json;
using MyJdownloader.Polling;

// Only the jdState event: the other four flags are turned off.
IReadOnlyList<PollingResult> results = await device.Polling.PollAsync(
    new PollingQuery
    {
        DownloadProgress = false,
        LinkGrabberState = false,
        CaptchasWaiting = false,
        AggregatedNumbers = false,
    },
    cancellationToken);

foreach (PollingResult result in results)
{
    if (result.EventName == "jdState" && result.EventData is { } eventData)
    {
        // GetProperty throws KeyNotFoundException when the device omits "data".
        JsonElement data = eventData.GetProperty("data");
        if (data.ValueKind == JsonValueKind.String && data.ValueEquals("RUNNING"))
        {
            Console.WriteLine("Downloads are running.");
        }
    }
}
```

### Remarks

- `PollAsync` throws `ArgumentException` when every flag of the query is `false`: at least one event must be requested.
- A `null` query is the same as `new PollingQuery()`: every event is requested, with the links of every running package.
- Large responses are a concern: with all events and many running packages, the response can approach the 1 MiB limit on the encrypted body. Restrict the query with the flags or with `DownloadProgressPackageUuids`. Above the limit, the call fails with `MyJdProtocolException`.
- The device returns no data: the result is an empty list.
- All text and values are untrusted. Encode them before you display them. Do not log whole `EventData` values: log the event name at most.
- `PollingResult.ToString()` prints only the JSON kind of `EventData`, never its content.

## PollingQuery

`public sealed record PollingQuery`

Selects the events returned by `PollAsync`. Every event is requested by default, and at least one must stay enabled.

| Property | Type | Default | Description |
|---|---|---|---|
| `DownloadProgress` | `bool` | `true` | Requests the `downloadProgress` event. |
| `JdState` | `bool` | `true` | Requests the `jdState` event. |
| `LinkGrabberState` | `bool` | `true` | Requests the `linkGrabberState` event. |
| `CaptchasWaiting` | `bool` | `true` | Requests the `captchasWaiting` event. |
| `AggregatedNumbers` | `bool` | `true` | Requests the `aggregatedNumbers` event. |
| `DownloadProgressPackageUuids` | `IReadOnlyList<long>?` | `null` | UUIDs of the packages whose running links are included in `downloadProgress`; `null` includes the links of every running package. Only used when `DownloadProgress` is `true`. |

## PollingResult

`public sealed record PollingResult`

One event returned by `PollAsync`. The payload is untyped.

`EventData` comes from the device and is untrusted: do not log it. `ToString()` prints only the JSON kind of `EventData`.

| Property | Type | Description |
|---|---|---|
| `EventName` | `string?` | Name of the event: `downloadProgress`, `jdState`, `linkGrabberState`, `captchasWaiting` or `aggregatedNumbers`. |
| `EventData` | `JsonElement?` | The raw event object. The payload is under its `data` property. `null` when the device returns none. |

## Polling or events

- `PollAsync` answers once with the current state. It creates no subscription, so there is nothing to unsubscribe, and it does not report changes that happen between two calls.
- The [`/events` facade](events.md) creates a subscription and returns the events queued since the last listen. Use it to follow changes over time. It needs the subscription lifecycle described in the [Events guide](../guides/events.md).

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [captcha.md](captcha.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Events guide](../guides/events.md)
