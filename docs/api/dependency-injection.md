# Dependency Injection

> Package `MyJdownloader.Extensions.DependencyInjection` · namespace `Microsoft.Extensions.DependencyInjection`

The DI package registers the `MyJDownloaderClient` as a singleton, together with its options and a dedicated named `HttpClient` configured for the relay. It is the recommended setup for ASP.NET Core and generic host applications. The core package `MyJdownloader` has no dependency on the DI abstractions; see [client.md](client.md) for constructing the client by hand.

## MyJDownloaderServiceCollectionExtensions

`public static class MyJDownloaderServiceCollectionExtensions`

Dependency injection extensions for the MyJDownloader client.

| Member | Signature | Description |
|---|---|---|
| `HttpClientName` | `public const string HttpClientName = "MyJdownloader"` | The name of the `HttpClient` registered for MyJDownloader. Reserved for this library: do not register another client under this name. |
| `AddMyJDownloader` | `static IHttpClientBuilder AddMyJDownloader(this IServiceCollection services, Action<MyJDownloaderOptions> configure)` | Registers the options, the named `HttpClient` and the `MyJDownloaderClient` singleton. Returns the builder of the named client for further configuration. |

**What is registered**

- `MyJDownloaderOptions`, bound through `configure` (`IOptions<MyJDownloaderOptions>`).
- The named `HttpClient` `HttpClientName`, with:
  - `PooledConnectionLifetime` of **2 minutes**, so DNS changes are picked up without rotating the handler (ADR-003);
  - `AllowAutoRedirect` set to `false`;
  - no default HTTP loggers, because device-call URL paths carry the session token and the device id;
  - an infinite handler lifetime.
- `MyJDownloaderClient` as a **singleton**, added with `TryAdd`: a registration made earlier by the application is kept. The client uses the registered `TimeProvider` when there is one.

**Remarks**

- **Named client, not typed client.** A typed client captured by a singleton no longer follows handler rotation, so the library uses a named client.
- **Keep the protections.** When you configure the returned `IHttpClientBuilder`, do not re-enable redirects on the primary handler, and do not call `AddLogger`, `AddDefaultLogger` or extended HTTP logging: they expose device-call paths. Non-`SocketsHttpHandler` primary handlers set by `ConfigureHttpClientDefaults` are replaced.
- **Bypasses.** A `MyJDownloaderClient` registered before `AddMyJDownloader` is kept and bypasses these protections. So does a typed client, `AddHttpClient<MyJDownloaderClient>`, whatever the call order: it registers its own transient client that does not use the `HttpClientName` configuration.
- **Validation.** Invalid options throw `ArgumentException` at the first resolution of `MyJDownloaderClient`, not at registration.
- **Session state.** The singleton holds the session in memory. A process restart starts disconnected: restore a session saved with `MyJdSession.ToJson()` (see [client.md](client.md#myjdsession)).

### Example

Registration in the application startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;

services.AddMyJDownloader(options =>
{
    options.AppKey = "MyApp";
    options.ApiVersion = 1;
})
// Optional: safe settings on the named client. Keep redirects disabled.
.ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(30));

services.AddSingleton<DeviceMonitor>();
```

Consumption through constructor injection:

```csharp
using MyJdownloader;

public sealed class DeviceMonitor(MyJDownloaderClient client)
{
    public async Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        if (!client.IsConnected)
        {
            // Connect here, or restore a session saved earlier with MyJdSession.FromJson.
            throw new InvalidOperationException("The MyJDownloader client is not connected.");
        }

        return await client.GetDevicesAsync(cancellationToken);
    }
}
```

## See also

- [Client API](client.md): the client, the session and the options registered by this extension
- [Common types](common-types.md)
- [Errors](errors.md)
- [Getting started](../guides/getting-started.md)
- [Dependency injection](../guides/dependency-injection.md)
- [ADR-003: injected HttpClient and named DI client](../adr/ADR-003-injected-httpclient-and-named-di-client.md)
