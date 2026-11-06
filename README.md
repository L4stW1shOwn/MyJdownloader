# MyJdownloader

A .NET client library for the [MyJDownloader](https://my.jdownloader.org) API.

> **Status: pre-1.0.** The library can connect, reconnect and disconnect,
> export and restore the session (with automatic renewal), list devices and
> make generic encrypted device calls via `GetDevice(id).Channel`. Typed
> namespace facades come in later versions. The API may still change before 1.0.

## Packages

- `MyJdownloader`: core library.
- `MyJdownloader.Extensions.DependencyInjection`: integration with
  `Microsoft.Extensions.DependencyInjection`.

```shell
dotnet add package MyJdownloader
dotnet add package MyJdownloader.Extensions.DependencyInjection
```

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;

services.AddMyJDownloader(o => o.AppKey = "MyApp");

// AddMyJDownloader returns an IHttpClientBuilder, so the client can be
// enriched, for example with resilience (package
// Microsoft.Extensions.Http.Resilience):
// services.AddMyJDownloader(o => o.AppKey = "MyApp")
//     .AddStandardResilienceHandler();
```

Resolve the `MyJDownloaderClient` singleton, connect, and call a device:

```csharp
// Placeholders: email, password, deviceId (from GetDevicesAsync), args, MyResult
// (your response type) and MyContext (your source-generated JsonSerializerContext).
var services = new ServiceCollection();
services.AddMyJDownloader(o => o.AppKey = "MyApp");
using var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<MyJDownloaderClient>();
var session = await client.ConnectAsync(email, password);

// Store this securely, see "Security warning" below.
string json = session.ToJson();

// Later, restore it instead of reconnecting with the password.
client.RestoreSession(MyJdSession.FromJson(json));

// Generic encrypted call; the JsonTypeInfo comes from the source-generated context.
IReadOnlyList<JsonElement> args = [];
var device = client.GetDevice(deviceId);
var result = await device.Channel.CallAsync("/jd/version", MyContext.Default.MyResult, args);
```

Without dependency injection, build the `HttpClient` yourself with a
long-lived `SocketsHttpHandler` (`PooledConnectionLifetime` set, for example
two minutes; `AllowAutoRedirect = false`) and no HTTP logging:

```csharp
var http = new HttpClient(new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    AllowAutoRedirect = false,
});
var client = new MyJDownloaderClient(http, new MyJDownloaderOptions { AppKey = "MyApp" });
```

## Logging and tracing

Device-call URL paths contain the session token and the device id. The named
client registered by `AddMyJDownloader` removes the default HTTP loggers; do
not re-add them (`AddLogger`, extended HTTP logging). The `System.Net.Http`
EventSource and the `url.full` Activity tag redact only the query string, so
exclude this client's traces or add a redacting enricher. A
`MyJDownloaderClient` registered before `AddMyJDownloader` is kept and bypasses
these protections.

## Security warning

The exported session contains keys derived from your password. Treat it as a
secret: store it in a protected location (for example DPAPI or a secret
vault), never in plain text, in source control or in logs.

## Design decisions

Architecture decisions are documented in docs/adr in the source
repository.

## License

MIT
