# Getting started

This guide installs the packages, connects to your MyJDownloader account, lists
your devices, makes a first call and disconnects. It shows two setups: with
dependency injection (recommended in hosted applications) and without it.

## Requirements

- .NET 10 SDK (the packages target `net10.0`).
- A MyJDownloader account (<https://my.jdownloader.org>) with at least one
  JDownloader instance connected to it.

## Install the packages

```shell
dotnet add package MyJdownloader
dotnet add package MyJdownloader.Extensions.DependencyInjection
```

- `MyJdownloader` is the core library. It depends on the base class library
  only and is AOT compatible.
- `MyJdownloader.Extensions.DependencyInjection` registers the client in
  `Microsoft.Extensions.DependencyInjection`. Skip it if your application does
  not use the container.

## Option A: with dependency injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;

// email and password: read them from a secret store, never hard-code them.
var services = new ServiceCollection();
services.AddMyJDownloader(o => o.AppKey = "MyApp");

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<MyJDownloaderClient>();

await client.ConnectAsync(email, password);

IReadOnlyList<DeviceInfo> devices = await client.GetDevicesAsync();
JDownloaderDevice device = client.GetDevice(devices[0].Id); // assumes one device

long version = await device.Jd.GetVersionAsync();
Console.WriteLine($"JDownloader version {version}");

await client.DisconnectAsync();
```

`AddMyJDownloader` registers the options, a named `HttpClient` tuned for this
library, and a `MyJDownloaderClient` singleton. Resolve the client from the
container rather than creating it yourself. See
[Dependency injection](dependency-injection.md) for the options and for how to
enrich the HTTP pipeline.

## Option B: without dependency injection

Build the `HttpClient` yourself and keep it for the lifetime of the application.
The client does not own it and does not dispose it.

```csharp
using MyJdownloader;

using var http = new HttpClient(new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2), // picks up DNS changes
    AllowAutoRedirect = false,                          // redirects are not followed
});

// Do not add HTTP logging to this client: device-call URLs carry the session
// token and the device id. See logging-and-security.md.
var client = new MyJDownloaderClient(http, new MyJDownloaderOptions { AppKey = "MyApp" });

await client.ConnectAsync(email, password);

IReadOnlyList<DeviceInfo> devices = await client.GetDevicesAsync();
JDownloaderDevice device = client.GetDevice(devices[0].Id);

long version = await device.Jd.GetVersionAsync();

await client.DisconnectAsync();
```

`MyJDownloaderOptions` has three properties:

| Property      | Default                          | Meaning                                              |
|---------------|----------------------------------|------------------------------------------------------|
| `AppKey`      | `"MyJdownloader.NET"`            | Application identifier sent at connection.           |
| `BaseAddress` | `https://api.jdownloader.org`    | Relay address. Version 1 supports only the default.  |
| `ApiVersion`  | `1`                              | `apiVer` value used for device calls.                |

## Connect, list devices, first call

1. `ConnectAsync(email, password)` authenticates and stores the session in the
   client. The password is used for this call only and is never kept. See
   [Sessions and authentication](sessions-and-authentication.md).
2. `GetDevicesAsync()` lists the devices attached to the account.
3. `GetDevice(id)` returns the facade of one device. It sends no request, so
   an unknown id is only detected by the first call.
4. A typed facade (`device.Jd`, `device.LinkGrabber`, ...) makes the call. The
   list of facades is in [Device administration](device-administration.md) and
   [Managing downloads](managing-downloads.md).

## Disconnect

```csharp
await client.DisconnectAsync();
```

The local session is cleared before the server call. If the server call fails
or is cancelled, the client is still disconnected locally, but the tokens stay
valid on the server until they expire.

## Next steps

- [Sessions and authentication](sessions-and-authentication.md): export and
  restore the session without the password.
- [Error handling](error-handling.md): what to catch and how to retry safely.
- [Logging and security](logging-and-security.md): protect the session and the
  device-call URLs.
