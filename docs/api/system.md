# System

The system namespace controls the device itself: it restarts or exits JDownloader, and it shuts down, puts on standby or hibernates the operating system. These are the most disruptive actions the library exposes, so review them before calling.

**Coverage:** 5/7 actions of `/system` — not yet typed: `getStorageInfos`, `getSystemInfos`. See [API coverage](../api-coverage.md).

Reach it as `device.System`, which returns a `SystemApi`. The C# namespace is `MyJdownloader.SystemControl`, and the device API path is `/system`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

This namespace has no model types.

## SystemApi

`public sealed class SystemApi`

Typed facade over the `/system` namespace of one device (restart or exit JDownloader, shut down, standby or hibernate the operating system).

| Member | Signature | Description |
|---|---|---|
| `RestartAsync` | `Task RestartAsync(CancellationToken cancellationToken = default)` | Restarts JDownloader on the device (`/system/restartJD`). |
| `ExitAsync` | `Task ExitAsync(CancellationToken cancellationToken = default)` | Exits JDownloader on the device (`/system/exitJD`). |
| `ShutdownOsAsync` | `Task ShutdownOsAsync(bool force, CancellationToken cancellationToken = default)` | Shuts down the operating system of the device (`/system/shutdownOS`). `force` (`true`) forces the shutdown even if applications block it; unsaved data may be lost. |
| `StandbyOsAsync` | `Task StandbyOsAsync(CancellationToken cancellationToken = default)` | Puts the operating system of the device on standby (`/system/standbyOS`). |
| `HibernateOsAsync` | `Task HibernateOsAsync(CancellationToken cancellationToken = default)` | Hibernates the operating system of the device (`/system/hibernateOS`). |

Remarks:

- These actions are destructive, and the library asks for no confirmation. Confirm with the user before calling them.
- The device usually goes offline before answering. A `TimeoutException`, an `HttpRequestException` or a `MyJdDeviceOfflineException` therefore does not mean the action failed; the action may have been executed.
- System and storage information (`getSystemInfos`, `getStorageInfos`) is not typed yet. Use the raw channel through `device.Channel`, described in [device-channel.md](device-channel.md).

### Example

```csharp
using MyJdownloader.SystemControl;

// Destructive: restarts JDownloader on the device without confirmation.
// A timeout here does not mean the restart failed.
await device.System.RestartAsync();

// Destructive: forces an OS shutdown; unsaved work on the device may be lost.
// await device.System.ShutdownOsAsync(force: false);
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
