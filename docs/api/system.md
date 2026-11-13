# System

The system namespace reads system and storage information, and it controls the device itself: it restarts or exits JDownloader, and it shuts down, puts on standby or hibernates the operating system. These are the most disruptive actions the library exposes, so review them before calling.

**Coverage:** 7/7 actions of `/system` (complete).

Reach it as `device.System`, which returns a `SystemApi`. The C# namespace is `MyJdownloader.SystemControl`, and the device API path is `/system`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## SystemApi

`public sealed class SystemApi`

Typed facade over the `/system` namespace of one device (system and storage information; restart or exit JDownloader, shut down, standby or hibernate the operating system).

| Member | Signature | Description |
|---|---|---|
| `RestartAsync` | `Task RestartAsync(CancellationToken cancellationToken = default)` | Restarts JDownloader on the device (`/system/restartJD`). |
| `ExitAsync` | `Task ExitAsync(CancellationToken cancellationToken = default)` | Exits JDownloader on the device (`/system/exitJD`). |
| `ShutdownOsAsync` | `Task ShutdownOsAsync(bool force, CancellationToken cancellationToken = default)` | Shuts down the operating system of the device (`/system/shutdownOS`). `force` (`true`) forces the shutdown even if applications block it; unsaved data may be lost. |
| `StandbyOsAsync` | `Task StandbyOsAsync(CancellationToken cancellationToken = default)` | Puts the operating system of the device on standby (`/system/standbyOS`). |
| `HibernateOsAsync` | `Task HibernateOsAsync(CancellationToken cancellationToken = default)` | Hibernates the operating system of the device (`/system/hibernateOS`). |
| `GetSystemInfoAsync` | `Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default)` | Returns the system and JVM information of the device (`/system/getSystemInfos`). Read-only. The content comes from the device and is untrusted. |
| `GetStorageInfosAsync` | `Task<IReadOnlyList<StorageInfo>> GetStorageInfosAsync(string? path = null, CancellationToken cancellationToken = default)` | Returns the storage information of the device (`/system/getStorageInfos`), one entry per storage root; an empty list when the device returns none. A `null` path lists every storage root. A path is rejected with `ArgumentException` when it is blank, longer than 32,767 characters or contains a control character; the check runs before any request. Read-only. The content comes from the device and is untrusted. |

Remarks:

- These actions are destructive, and the library asks for no confirmation. Confirm with the user before calling them.
- The device usually goes offline before answering. A `TimeoutException`, an `HttpRequestException` or a `MyJdDeviceOfflineException` therefore does not mean the action failed; the action may have been executed.
- A storage root that failed on the device stays in the list, with its message in `StorageInfo.Error`. Check `Error` for each root.

### Information example

```csharp
using MyJdownloader.SystemControl;

var system = await device.System.GetSystemInfoAsync();
Console.WriteLine($"JVM {system.JavaVersionString} on {system.OsString}");

foreach (var root in await device.System.GetStorageInfosAsync())
{
    Console.WriteLine(root.Error is null
        ? $"{root.Path}: {root.Free} of {root.Size} bytes free"
        : $"{root.Path}: {root.Error}");
}
```

### Example

```csharp
using MyJdownloader.SystemControl;

// Destructive: restarts JDownloader on the device without confirmation.
// A timeout here does not mean the restart failed.
await device.System.RestartAsync();

// Destructive: forces an OS shutdown; unsaved work on the device may be lost.
// await device.System.ShutdownOsAsync(force: false);
```

## SystemInfo

`public sealed record SystemInfo`

System and JVM information of the device, as returned by `GetSystemInfoAsync`.

| Property | Type | Description |
|---|---|---|
| `ArchFamily` | `string?` | CPU architecture family, for example `X86`. |
| `ArchString` | `string?` | CPU architecture as reported by the JVM, for example `amd64`. |
| `Hardware` | `string?` | Hardware description of the device. |
| `JavaName` | `string?` | Name of the Java virtual machine. |
| `JavaVendor` | `string?` | Vendor of the Java virtual machine. |
| `JavaVersionString` | `string?` | Java version as text, for example `11.0.25+11`. Use it for display. |
| `OperatingSystem` | `string?` | Operating system identifier, for example `LINUX`. |
| `OsFamily` | `string?` | Operating system family, for example `LINUX`. |
| `OsString` | `string?` | Operating system name and version as text. |
| `HeapCommitted` | `long` | Committed JVM heap size, in bytes. |
| `HeapMax` | `long` | Maximum JVM heap size, in bytes. |
| `HeapUsed` | `long` | Used JVM heap size, in bytes. |
| `JavaVersion` | `long` | Encoded Java version, for example `11000025000011` for `11.0.25+11`. Use `JavaVersionString` for display. |
| `StartupTime` | `DateTimeOffset?` | Time JDownloader was started. `null` when the device sends zero or less. |
| `Arch64Bit` | `bool` | `true` when the CPU architecture is 64-bit. |
| `Docker` | `bool` | `true` when the device runs inside a Docker container. |
| `Headless` | `bool` | `true` when the JVM runs without a graphical interface. |
| `Jvm64Bit` | `bool` | `true` when the JVM is 64-bit. |
| `Os64Bit` | `bool` | `true` when the operating system is 64-bit. |
| `Snap` | `bool` | `true` when JDownloader runs as a Snap package. |

**Remarks**

- The values come from the device and are untrusted. Show them as plain text.
- The startup time is sent as epoch milliseconds; the library converts it to a `DateTimeOffset`.

## StorageInfo

`public sealed record StorageInfo`

Storage information of one root of the device, as returned by `GetStorageInfosAsync`.

| Property | Type | Description |
|---|---|---|
| `Path` | `string?` | Path of the storage root. |
| `Size` | `long` | Total size, in bytes. |
| `Free` | `long` | Free space, in bytes. |
| `Error` | `string?` | Error message the device reported for this root. `null` when there is none. |

**Remarks**

- The values come from the device and are untrusted.
- A root that failed on the device is still returned, with its message in `Error`. Check `Error` before using `Size` and `Free` for that root.

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
