# Dependency injection

The `MyJdownloader.Extensions.DependencyInjection` package registers the options,
a named `HttpClient` and the `MyJDownloaderClient` singleton with
`Microsoft.Extensions.DependencyInjection`. This guide covers the registration,
the options, how to enrich the HTTP pipeline, and the configurations to avoid.
The design is recorded in [ADR-003](../adr/ADR-003-injected-httpclient-and-named-di-client.md).
The local API client has its own registration, `AddLocalJDownloader`, described
at the end of this guide.

## Register the client

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;

services.AddMyJDownloader(o =>
{
    o.AppKey = "MyApp";
});
```

`AddMyJDownloader` (in the `Microsoft.Extensions.DependencyInjection` namespace)
registers:

- the `MyJDownloaderOptions` binding, read by the client;
- the named `HttpClient` `MyJDownloaderServiceCollectionExtensions.HttpClientName`
  (the constant value is `"MyJdownloader"`). The name is reserved for this
  library;
- a `MyJDownloaderClient` singleton, added with `TryAdd`. It uses the registered
  `TimeProvider` when there is one.

Inject the client through its constructor:

```csharp
public sealed class DeviceVersionService(MyJDownloaderClient client)
{
    public async Task<long> GetVersionAsync(string deviceId, CancellationToken ct)
    {
        JDownloaderDevice device = client.GetDevice(deviceId);
        return await device.Jd.GetVersionAsync(ct);
    }
}
```

Do not resolve the client through a service locator in your own code.

## Options

`MyJDownloaderOptions` is read when the client is first resolved. Invalid
values throw `ArgumentException` at that point.

| Property                | Default                       | Notes                                                             |
|-------------------------|-------------------------------|-------------------------------------------------------------------|
| `AppKey`                | `"MyJdownloader.NET"`         | Set it to your application name.                                  |
| `BaseAddress`           | `https://api.jdownloader.org` | Version 1 uses only the official relay (ADR-006).                 |
| `ApiVersion`            | `1`                           | `apiVer` used for device calls.                                   |
| `MaxResponseBodyLength` | `1048576` (1 MiB)             | Largest accepted response body, in bytes; at least `1024`. Raise it for large lists. |

```csharp
services.AddMyJDownloader(o => o.AppKey = "MyApp");
```

## The named HttpClient

The registration configures the named client as follows:

- `SocketsHttpHandler.PooledConnectionLifetime` is two minutes, so DNS changes
  are picked up without rotating the handler.
- `SocketsHttpHandler.AllowAutoRedirect` is `false`.
- All HTTP loggers are removed.
- The handler lifetime is infinite (`SetHandlerLifetime(Timeout.InfiniteTimeSpan)`).
  The client is a singleton, so handler rotation is disabled on purpose.

## Enrich the pipeline

`AddMyJDownloader` returns an `IHttpClientBuilder`. Use it to add handlers,
resilience or timeouts to the named client:

```csharp
// Package: Microsoft.Extensions.Http.Resilience
services.AddMyJDownloader(o => o.AppKey = "MyApp")
    .AddStandardResilienceHandler();
```

Read the warnings before enabling any retry:

- Device calls are HTTP `POST` requests. The library never retries them itself
  (see [ADR-005](../adr/ADR-005-error-handling.md)). A handler-level retry can
  replay a call that the device already executed: a restart, a shutdown, an
  `AddLinksAsync`, a password change.
- Check the retry defaults of the resilience package before adding it. Tune
  them, or limit retries to read-only calls, rather than accepting the defaults
  for every device action.
- `ConfigureHttpClient` may change the `Timeout`. Keep it above the event poll
  timeout (see [Events](events.md)).

Constraints on what you add to the builder:

- If you replace or reconfigure the primary handler, keep
  `SocketsHttpHandler.AllowAutoRedirect` set to `false`.
- Do not call `AddLogger`, `AddDefaultLogger` or add extended HTTP logging to
  this client. Device-call URL paths carry the session token and the device id.
  See [Logging and security](logging-and-security.md).

## Do not use a typed client

Do not register the client as a typed client:

```csharp
// Do not do this for MyJDownloaderClient.
services.AddHttpClient<MyJDownloaderClient>();
```

A typed client registers a transient `MyJDownloaderClient` that replaces the
singleton, whatever the registration order. Its `HttpClient` does not get the
named client's configuration: redirects are followed, loggers may be attached
and the pooled connection lifetime is not set. Use `AddMyJDownloader` only.

A `MyJDownloaderClient` you register yourself before calling `AddMyJDownloader`
is kept, so it bypasses the same protections. Do not register one yourself.

## Lifetime

The client is thread-safe and is registered as a singleton: every scope receives
the same instance. Do not create a second `MyJDownloaderClient` for the same
account, because each instance keeps its own session.

## Register the local API client

`AddLocalJDownloader` registers the `LocalJDownloaderClient` for the local API
of one JDownloader device, with no MyJDownloader account (see
[Local API](local-api.md)):

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader.Local;

services.AddLocalJDownloader(_ => { }); // defaults: http://127.0.0.1:3128
```

It registers the `LocalJDownloaderOptions` binding, the named `HttpClient`
`LocalJDownloaderServiceCollectionExtensions.HttpClientName` (the constant value
is `"MyJdownloader.Local"`), and a `LocalJDownloaderClient` singleton, added with
`TryAdd`. Inject the client through its constructor and use `client.Device` for
the typed facades. `JDownloaderDevice` is not registered: the relay client also
produces such instances, so a registration would be ambiguous when both clients
are in the container.

The registration disables redirects, and that matters for security. The local
API has no authentication and no encryption. If the `HttpClient` followed
redirects, whatever answers on the local port could redirect a cleartext POST
body, which can hold link and archive passwords, to another host before the
library rejects the answer. The registration therefore disables redirects again
after every handler configuration of this client, whatever the registration
order: a `SocketsHttpHandler` or `HttpClientHandler` primary handler always ends
with `AllowAutoRedirect = false`, even when you replace it on the returned
builder. A primary handler of another type is used as it is and must not follow
redirects.

Do not add HTTP loggers to this client: they log the device address and, at
trace level, the request headers, which hold the credentials of a reverse proxy
when you add them. Request bodies carry passwords: never log them from a custom
handler. Handlers added by `ConfigureHttpClientDefaults` (resilience, logging)
also apply to this client, and a retry handler would replay device calls, which
are not idempotent.

`LocalJDownloaderOptions` is the unnamed options instance, so every
`Configure<LocalJDownloaderOptions>` in the container applies. Do not bind
`AllowUnencryptedRemoteHost` to configuration you do not trust.

Both registrations can share one container, because their client names, option
types and client types are distinct:

```csharp
services.AddMyJDownloader(o => o.AppKey = "MyApp");
services.AddLocalJDownloader(o => o.BaseAddress = new Uri("http://127.0.0.1:3128"));
```

Do not register the local client as a typed client, and do not register a
`LocalJDownloaderClient` yourself before `AddLocalJDownloader`. Both bypass the
protections above:

```csharp
// Do not do this for LocalJDownloaderClient.
services.AddHttpClient<LocalJDownloaderClient>();
```

For a device on another host (https with a pinned certificate), see
[Local API](local-api.md#dependency-injection).
