# Reconnect

The reconnect namespace asks the device to reconnect its internet connection (a router reconnect, as JDownloader does it).

**Coverage:** 1/1 actions of `/reconnect` (complete).

Reach it as `device.Reconnect`, which returns a `ReconnectApi`. The C# namespace is `MyJdownloader.Reconnect`, and the device API path is `/reconnect`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning.** `DoReconnectAsync` drops the internet connection of the device at once. It acts without any confirmation, running downloads are interrupted, and the device is unreachable for a while. Ask the user before you call it.

## ReconnectApi

`public sealed class ReconnectApi`

Typed facade over the `/reconnect` namespace of one device (internet connection reconnect).

| Member | Signature | Description |
|---|---|---|
| `DoReconnectAsync` | `Task DoReconnectAsync(CancellationToken cancellationToken = default)` | Asks the device to reconnect its internet connection (`/reconnect/doReconnect`). The task completes when the device has answered; the answer carries no value. |

Remarks:

- The device requests the reconnect immediately, without confirmation.
- The call, or the calls that follow it, may fail with `MyJdDeviceOfflineException`, a `TimeoutException` or an `HttpRequestException` even though the reconnect happened: the device may be briefly unreachable while its connection comes back. A `TimeoutException` or `HttpRequestException` means the action may have been executed.
- Do not replay `DoReconnectAsync` after a failure without checking the device first. Each call requests another reconnect.
- `MyJDownloaderClient.ReconnectAsync` renews the MyJD session and is unrelated to this action. `device.Reconnect` reconnects the internet connection of the device.

### Example

```csharp
using MyJdownloader;
using MyJdownloader.Errors;

// Ask the user first: the connection drops at once and downloads are interrupted.
if (AskUser("Reconnect the internet connection of the device now?"))
{
    try
    {
        await device.Reconnect.DoReconnectAsync(cancellationToken);
    }
    catch (MyJdDeviceOfflineException)
    {
        // Expected while the connection comes back: the reconnect may already have happened.
        // Wait with a delay of your own, then check the device before any further action.
    }
}
```

`AskUser` stands for your own user prompt. After a failure, check the device with [`device.Device.PingAsync`](device.md) before you send another request.

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [Device administration](../guides/device-administration.md)
