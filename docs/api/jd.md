# Jd

The `jd` namespace exposes read-only information about the JDownloader application running on the device (uptime, version, core revision) and one maintenance action (refreshing the plugins). Use it to identify the build a device runs and to see how long it has been up.

**Coverage:** 4/6 actions of `/jd` — not yet typed: `doSomethingCool`, `sum` (test endpoints, not planned). See [API coverage](../api-coverage.md).

Reach it as `device.Jd`, which returns a `JdApi`. The C# namespace is `MyJdownloader.Jd`, and the device API path is `/jd`. Restarting JDownloader is not part of this namespace: it is `/system/restartJD`, exposed by [System](system.md).

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

This namespace has no model types: every member returns a primitive or a `TimeSpan`.

## JdApi

`public sealed class JdApi`

Typed facade over the `/jd` namespace of one device (uptime, version, core revision and plugin refresh).

| Member | Signature | Description |
|---|---|---|
| `GetUptimeAsync` | `Task<TimeSpan> GetUptimeAsync(CancellationToken cancellationToken = default)` | Returns the time elapsed since the device started (`/jd/uptime`). |
| `GetVersionAsync` | `Task<long> GetVersionAsync(CancellationToken cancellationToken = default)` | Returns the version reported by the device (`/jd/version`). The raw value is returned as is; its meaning is not documented by the API. |
| `GetCoreRevisionAsync` | `Task<int> GetCoreRevisionAsync(CancellationToken cancellationToken = default)` | Returns the core revision number of the device (`/jd/getCoreRevision`). |
| `RefreshPluginsAsync` | `Task<bool> RefreshPluginsAsync(CancellationToken cancellationToken = default)` | Asks the device to refresh its plugins (`/jd/refreshPlugins`). Returns `true` when the device accepted the request. A timeout may still have triggered the refresh. |

### Example

```csharp
using MyJdownloader.Jd;

TimeSpan uptime = await device.Jd.GetUptimeAsync();
int revision = await device.Jd.GetCoreRevisionAsync();
long version = await device.Jd.GetVersionAsync();

Console.WriteLine($"Core revision {revision}, version {version}, up for {uptime}.");
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
