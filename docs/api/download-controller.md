# Download controller

The download controller is the queue-level switch of JDownloader: it starts, stops and pauses all downloads at once, and reports whether the queue is running and how fast it is moving. Use it to drive the whole queue; per-link and per-package control lives in [Downloads](downloads.md).

Reach it as `device.DownloadController`, which returns a `DownloadControllerApi`. The C# namespace is `MyJdownloader.DownloadController`, and the device API path is `/downloadcontroller`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## DownloadControllerApi

`public sealed class DownloadControllerApi`

Typed facade over the `/downloadcontroller` namespace of one device (start, stop, pause and state of the downloads).

| Member | Signature | Description |
|---|---|---|
| `StartAsync` | `Task<bool> StartAsync(CancellationToken cancellationToken = default)` | Starts the downloads (`/downloadcontroller/start`). Returns `true` when the device accepted the request. A timeout may still have started them. |
| `StopAsync` | `Task<bool> StopAsync(CancellationToken cancellationToken = default)` | Stops the downloads (`/downloadcontroller/stop`). Returns `true` when the device accepted the request. A timeout may still have stopped them. |
| `PauseAsync` | `Task<bool> PauseAsync(bool paused, CancellationToken cancellationToken = default)` | Pauses (`true`) or resumes (`false`) the downloads (`/downloadcontroller/pause`). Returns `true` when the device accepted the request. |
| `GetCurrentStateAsync` | `Task<DownloadState> GetCurrentStateAsync(CancellationToken cancellationToken = default)` | Returns the current `DownloadState` (`/downloadcontroller/getCurrentState`). A state this library does not recognize is returned as `DownloadState.Unknown`. |
| `GetSpeedAsync` | `Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)` | Returns the current download speed in bytes per second (`/downloadcontroller/getSpeedInBps`). |

Remarks:

- `StartAsync`, `StopAsync` and `PauseAsync` change the queue immediately and without confirmation. A timeout means the action may have been executed.
- The `bool` results report whether the device accepted the request; they do not confirm the resulting state. Read `GetCurrentStateAsync` to check it.

### Example

```csharp
using MyJdownloader.DownloadController;

await device.DownloadController.StartAsync();

var state = await device.DownloadController.GetCurrentStateAsync();
if (state == DownloadState.Running)
{
    await device.DownloadController.PauseAsync(true);
}
```

## DownloadState

`public enum DownloadState`

State of the device download controller, as reported by the JDownloader download watchdog. Unrecognized values returned by a device are read as `Unknown`, and `Unknown` cannot be sent.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The state is missing or not recognized by this library. |
| `Idle` | `1` | No download is running (`IDLE`). |
| `Running` | `2` | Downloads are running (`RUNNING`). |
| `Pause` | `3` | Downloads are paused (`PAUSE`). |
| `Stopping` | `4` | Downloads are stopping (`STOPPING`). |
| `Stopped` | `5` | Downloads are stopped (`STOPPED`). |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
