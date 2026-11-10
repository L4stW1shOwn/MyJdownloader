# Toolbar

The toolbar namespace is the action set of the browser-extension toolbar of JDownloader. Use it to read the toolbar status, start and stop the downloads, flip the settings of the toolbar and trigger an update check.

**Coverage:** 11/15 actions of `/toolbar`. Not exposed: `addLinksFromDOM`, `checkLinksFromDOM`, `pollCheckedLinksFromDOM` and `specialURLHandling` (see [Not exposed](#not-exposed)).

Reach it as `device.Toolbar`, which returns a `ToolbarApi`. The C# namespace is `MyJdownloader.Toolbar`, and the device API path is `/toolbar`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning: side effects.** The start, stop and toggle methods act on the device at once, without confirmation. They change what the device downloads and how fast: a speed limit, a pause, premium accounts, the clipboard monitoring, the automatic reconnect and the stop after the current download.
>
> The toggles flip the setting, so they are not idempotent. A `TimeoutException`, `OperationCanceledException` or `HttpRequestException` means the change may have been applied, and a replay flips it back. Read the state with `GetStatusAsync` before you call a toggle again.

## ToolbarApi

`public sealed class ToolbarApi`

Typed facade over the `/toolbar` namespace of one device (the actions of the browser-extension toolbar). All content returned by the device is untrusted.

| Member | Signature | Description |
|---|---|---|
| `GetStatusAsync` | `Task<ToolbarStatus> GetStatusAsync(CancellationToken cancellationToken = default)` | Reads the state of the toolbar (`/toolbar/getStatus`). Throws `MyJdProtocolException` when the response holds no status. |
| `IsAvailableAsync` | `Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)` | Checks that the toolbar API answers (`/toolbar/isAvailable`). Returns `true` on current versions, so it is a liveness probe only. Throws `MyJdProtocolException` when the response holds no value. |
| `StartDownloadsAsync` | `Task<bool> StartDownloadsAsync(CancellationToken cancellationToken = default)` | Starts the downloads (`/toolbar/startDownloads`). Returns `true` on current versions. |
| `StopDownloadsAsync` | `Task<bool> StopDownloadsAsync(CancellationToken cancellationToken = default)` | Stops the downloads (`/toolbar/stopDownloads`). Returns `true` on current versions. |
| `TriggerUpdateAsync` | `Task<bool> TriggerUpdateAsync(CancellationToken cancellationToken = default)` | Starts an update check in the background, without a dialog (`/toolbar/triggerUpdate`). Returns `true` as soon as the check is started; the call does not wait for it. |
| `ToggleDownloadSpeedLimitAsync` | `Task<bool> ToggleDownloadSpeedLimitAsync(CancellationToken cancellationToken = default)` | Flips the download speed limit (`/toolbar/toggleDownloadSpeedLimit`). Returns the new state: `true` when the limit is now on. |
| `TogglePauseDownloadsAsync` | `Task<bool> TogglePauseDownloadsAsync(CancellationToken cancellationToken = default)` | Flips the pause of the downloads (`/toolbar/togglePauseDownloads`). Returns the new state: `true` when paused. |
| `TogglePremiumAsync` | `Task<bool> TogglePremiumAsync(CancellationToken cancellationToken = default)` | Flips the use of premium accounts (`/toolbar/togglePremium`). Returns the new state: `true` when premium accounts are used. |
| `ToggleClipboardMonitoringAsync` | `Task<bool> ToggleClipboardMonitoringAsync(CancellationToken cancellationToken = default)` | Flips the clipboard monitoring (`/toolbar/toggleClipboardMonitoring`). Returns the new state: `true` when the clipboard is monitored. |
| `ToggleAutomaticReconnectAsync` | `Task<bool> ToggleAutomaticReconnectAsync(CancellationToken cancellationToken = default)` | Flips the automatic reconnect (`/toolbar/toggleAutomaticReconnect`). Returns the new state: `true` when it is on. |
| `ToggleStopAfterCurrentDownloadAsync` | `Task<bool> ToggleStopAfterCurrentDownloadAsync(CancellationToken cancellationToken = default)` | Flips the stop after the current download (`/toolbar/toggleStopAfterCurrentDownload`). Returns the new state: `true` when it is on. |

Every method that returns `bool` throws `MyJdProtocolException` when the response holds no value.

Remarks:

- A toggle returns the state after the change. Keep that value rather than computing the new state from the one you read before the call.

### Overlap with other facades

Several actions are also exposed by other facades, with more detail or with a target state:

- `StartDownloadsAsync` and `StopDownloadsAsync` do the same as `device.DownloadController.StartAsync` and `StopAsync`. Use the [download controller](download-controller.md) when you need to confirm the state afterwards with `GetCurrentStateAsync`.
- `TogglePauseDownloadsAsync` flips the pause. `device.DownloadController.PauseAsync(paused)` takes the target state, so a replay of that call is harmless. Prefer it when you know the state you want.
- `TriggerUpdateAsync` starts an update check. `device.Update.RunUpdateCheckAsync` also starts an update check, and `IsUpdateAvailableAsync` tells whether an update is available. See the [Update page](update.md).

## ToolbarStatus

`public sealed record ToolbarStatus`

The state of the toolbar, as returned by `GetStatusAsync`. A member absent from the response keeps its default value: `false`, `0` or `null`.

| Property | Type | Wire name | Unit | Description |
|---|---|---|---|---|
| `State` | `string?` | `state` | — | Label of the download watchdog of the device. Free text from the device: untrusted. `null` when not set. |
| `Running` | `bool` | `running` | — | Whether downloads are running. |
| `SpeedLimitEnabled` | `bool` | `limit` | — | Whether the download speed limit is on. |
| `SpeedLimit` | `long` | `limitspeed` | bytes per second | The download speed limit. `0` when the limit is off. |
| `AutoReconnectEnabled` | `bool` | `reconnect` | — | Whether the automatic reconnect is on. |
| `ClipboardMonitored` | `bool` | `clipboard` | — | Whether the clipboard is monitored for links. |
| `StopAfterCurrentDownload` | `bool` | `stopafter` | — | Whether the downloads stop after the current download. |
| `PremiumEnabled` | `bool` | `premium` | — | Whether premium accounts are used. |
| `Speed` | `long` | `speed` | bytes per second | The current download speed. `0` when idle. |
| `Paused` | `bool` | `pause` | — | Whether the downloads are paused. |
| `BytesLoaded` | `long` | `download_current` | bytes | Bytes loaded so far. |
| `BytesTotal` | `long` | `download_complete` | bytes | Estimated total size of the enabled links. An estimate, not a guarantee. |

**Remarks**

- `State` is a label of the device and is untrusted. Show it as plain text.
- The values are a snapshot. Read them again rather than keep an earlier status.

## Not exposed

Four of the 15 actions have no typed method.

| Action | Why it is not exposed |
|---|---|
| `addLinksFromDOM` | Part of the browser-extension DOM protocol: it reads named HTTP parameters (such as `url`, `data`, `index`, `sessionid` and `lastchunk`) and receives the page in chunks. `IDeviceChannel` sends positional parameters only, so the action cannot be called correctly through it either. It also opens a dialog on the device. Use [LinkGrabber](link-grabber.md) to add links. |
| `checkLinksFromDOM` | Same protocol as `addLinksFromDOM`, with the same named parameters. Its result is read with `pollCheckedLinksFromDOM`. |
| `pollCheckedLinksFromDOM` | Reads the result of a `checkLinksFromDOM` call. It is meaningless without the identifier of that call, so it is excluded with it. |
| `specialURLHandling` | Returns JavaScript for the browser extension to evaluate. A .NET client has no use for it, and forwarding that script to a browser would be unsafe. |

## Example

Read the status, then flip the pause. The toggle is not replayed blindly after a timeout.

```csharp
using System;
using MyJdownloader.Toolbar;

ToolbarStatus status = await device.Toolbar.GetStatusAsync(cancellationToken);
Console.WriteLine($"Running: {status.Running}, speed: {status.Speed} B/s, " +
    $"loaded {status.BytesLoaded} of about {status.BytesTotal} bytes.");

// Toggles flip the setting: check the state first, and do not replay a toggle blindly.
if (!status.Paused && AskUser("Pause the downloads on the device?"))
{
    try
    {
        bool pausedNow = await device.Toolbar.TogglePauseDownloadsAsync(cancellationToken);
        Console.WriteLine($"Paused: {pausedNow}");
    }
    catch (TimeoutException)
    {
        // The toggle may have run. Read the state again before any further action.
        ToolbarStatus current = await device.Toolbar.GetStatusAsync(cancellationToken);
        Console.WriteLine($"Paused after the timeout: {current.Paused}");
    }
}
```

`AskUser` stands for your own user prompt.

See also:

- [client.md](client.md)
- [download-controller.md](download-controller.md)
- [update.md](update.md)
- [errors.md](errors.md)
- [Managing downloads](../guides/managing-downloads.md)
