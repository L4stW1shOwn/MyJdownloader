# Update

The update namespace checks whether a JDownloader update is available on the device and installs it. Installing an update requires a restart of JDownloader, which the device performs without confirmation.

Reach it as `device.Update`, which returns an `UpdateApi`. The C# namespace is `MyJdownloader.Update`, and the device API path is `/update`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

This namespace has no model types.

## UpdateApi

`public sealed class UpdateApi`

Typed facade over the `/update` namespace of one device (check for and install JDownloader updates).

| Member | Signature | Description |
|---|---|---|
| `IsUpdateAvailableAsync` | `Task<bool> IsUpdateAvailableAsync(CancellationToken cancellationToken = default)` | Returns `true` when an update is available on the device (`/update/isUpdateAvailable`). Read the outcome of a check started with `RunUpdateCheckAsync` through this method. |
| `RunUpdateCheckAsync` | `Task RunUpdateCheckAsync(CancellationToken cancellationToken = default)` | Starts an update check on the device (`/update/runUpdateCheck`). The check runs on the device and may return before it finishes, so call `IsUpdateAvailableAsync` afterwards to read the result. |
| `RestartAndUpdateAsync` | `Task RestartAndUpdateAsync(CancellationToken cancellationToken = default)` | Restarts JDownloader on the device to install the pending updates (`/update/restartAndUpdate`). This is destructive: the restart happens immediately and without confirmation. |

Remarks:

- `RestartAndUpdateAsync` restarts JDownloader immediately and without confirmation. The device usually goes offline before answering, so a `TimeoutException`, an `HttpRequestException` or a `MyJdDeviceOfflineException` does not mean the action failed.
- `RunUpdateCheckAsync` returns before the check finishes on the device. Do not assume the result is available right after the call.

### Example

```csharp
using MyJdownloader.Update;

await device.Update.RunUpdateCheckAsync();

// The check may still be running on the device: wait before reading the result in a real flow.
if (await device.Update.IsUpdateAvailableAsync())
{
    // Destructive: JDownloader restarts on the device without confirmation. Confirm with the user first.
    await device.Update.RestartAndUpdateAsync();
}
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
