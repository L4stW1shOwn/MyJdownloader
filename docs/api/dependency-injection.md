# Dependency Injection

> Package `MyJdownloader.Extensions.DependencyInjection` · namespace `Microsoft.Extensions.DependencyInjection`

The DI package registers two clients as singletons: the `MyJDownloaderClient` for the relay and the `LocalJDownloaderClient` for the local API. Each comes with its options and a dedicated named `HttpClient` configured for its transport. It is the recommended setup for ASP.NET Core and generic host applications. The core package `MyJdownloader` has no dependency on the DI abstractions; see [client.md](client.md) for constructing the client by hand.

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

## LocalJDownloaderServiceCollectionExtensions

`public static class LocalJDownloaderServiceCollectionExtensions`

Dependency injection extensions for the local API client.

| Member | Signature | Description |
|---|---|---|
| `HttpClientName` | `public const string HttpClientName = "MyJdownloader.Local"` | The name of the `HttpClient` registered for the local API client. Reserved for this library: do not register another client under this name. |
| `AddLocalJDownloader` | `static IHttpClientBuilder AddLocalJDownloader(this IServiceCollection services, Action<LocalJDownloaderOptions> configure)` | Registers the options, the named `HttpClient` and the `LocalJDownloaderClient` singleton. `configure` is required: pass `_ => { }` for the defaults (`http://127.0.0.1:3128`). Returns the builder of the named client for further configuration. |

**What is registered**

- `LocalJDownloaderOptions`, bound through `configure` (`IOptions<LocalJDownloaderOptions>`).
- The named `HttpClient` `HttpClientName`, with:
  - `PooledConnectionLifetime` of **2 minutes**, so DNS changes are picked up without rotating the handler (ADR-003);
  - `AllowAutoRedirect` set to `false`, enforced after every handler configuration (see the remarks);
  - no default HTTP loggers: they log the device address and, at trace level, the request headers, which hold the credentials of a reverse proxy when you add them. Request bodies carry the arguments of device calls, some of them passwords: never log them from a custom handler;
  - an infinite handler lifetime.
- `LocalJDownloaderClient` as a **singleton**, added with `TryAdd`: a registration made earlier by the application is kept. The local client does not use `TimeProvider`.
- `JDownloaderDevice` is **not** registered. Use `client.Device`: the relay client also produces `JDownloaderDevice` instances, so a registration would be ambiguous when both clients are in the container.

**Remarks**

- **Why redirects are disabled.** The local API has no authentication and no encryption. An `HttpClient` that follows redirects would let whatever answers on the local port redirect a cleartext POST body, which can hold link and archive passwords, to another host before the library rejects the answer.
- **Redirects are enforced.** Redirects are disabled again after every handler configuration of this client, whatever the registration order: a `SocketsHttpHandler` or `HttpClientHandler` primary handler always ends with `AllowAutoRedirect = false`, even when you replace or reconfigure it through the returned `IHttpClientBuilder`, through `ConfigureAll<HttpClientFactoryOptions>` or through a later `AddHttpClient(HttpClientName)`. Not covered: a primary handler of another type (it is used as it is and must not follow redirects), a later `PostConfigure` of `HttpClientFactoryOptions`, and an `IHttpMessageHandlerBuilderFilter` that swaps the handler.
- **Keep the other protections.** Do not call `AddLogger`, `AddDefaultLogger` or extended HTTP logging on the returned builder. Non-`SocketsHttpHandler` primary handlers set by `ConfigureHttpClientDefaults` are replaced. Handlers added by `ConfigureHttpClientDefaults` (resilience, logging) also apply to this client: a retry handler would replay device calls, which are not idempotent.
- **Options are shared.** `LocalJDownloaderOptions` is the unnamed options instance: every `Configure<LocalJDownloaderOptions>` in the container applies, in registration order. Do not bind `AllowUnencryptedRemoteHost` to configuration you do not trust.
- **Bypasses.** A `LocalJDownloaderClient` registered before `AddLocalJDownloader` is kept and bypasses these protections. So does a typed client, `AddHttpClient<LocalJDownloaderClient>`, whatever the call order: it registers its own client that does not use the `HttpClientName` configuration.
- **Coexistence.** `AddMyJDownloader` and `AddLocalJDownloader` can be used in the same container: their client names, option types and client types are distinct.
- **Validation.** `ArgumentNullException` is thrown for a `null` argument. Invalid options throw `ArgumentException` at the first resolution of `LocalJDownloaderClient`, not at registration.

### Example

Registration in the application startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader.Local;

services.AddLocalJDownloader(options =>
{
    options.BaseAddress = new Uri("http://127.0.0.1:3128"); // the default
})
// Optional: safe settings on the named client. Keep redirects disabled.
.ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(30));

services.AddSingleton<LocalDeviceMonitor>();
```

Consumption through constructor injection:

```csharp
using MyJdownloader.Local;

public sealed class LocalDeviceMonitor(LocalJDownloaderClient client)
{
    public Task<long> GetVersionAsync(CancellationToken cancellationToken)
        => client.Device.Jd.GetVersionAsync(cancellationToken);
}
```

## See also

- [Client API](client.md): the client, the session and the options registered by this extension
- [Local client](local-client.md): the local API client and the options registered by this extension
- [Common types](common-types.md)
- [Errors](errors.md)
- [Getting started](../guides/getting-started.md)
- [Dependency injection](../guides/dependency-injection.md)
- [Local API](../guides/local-api.md): the local API path, its security model and the registration
- [ADR-003: injected HttpClient and named DI client](../adr/ADR-003-injected-httpclient-and-named-di-client.md)
- [ADR-012: local API transport](../adr/ADR-012-local-api-transport.md)
