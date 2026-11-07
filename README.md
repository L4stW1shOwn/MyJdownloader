# MyJdownloader

A .NET client library for the [MyJDownloader](https://my.jdownloader.org) API.

> **Status: pre-1.0.** The library can connect, reconnect and disconnect,
> export and restore the session (with automatic renewal), list devices and
> make generic encrypted device calls via `GetDevice(id).Channel`. The typed
> `device.LinkGrabber`, `device.Downloads`, `device.DownloadController`, `device.Jd`, `device.System`, `device.Config` and `device.Accounts` facades are available; other namespaces come in later versions. The API may still change before 1.0.

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

## LinkGrabber

`device.LinkGrabber` is the typed facade over `/linkgrabberv2`:

```csharp
using MyJdownloader.LinkGrabber;

var job = await device.LinkGrabber.AddLinksAsync(new AddLinksRequest
{
    Links = ["https://host.example/file1", "https://host.example/file2"],
    PackageName = "my package",
});

// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): page large lists with StartAt / MaxResults.
var packages = await device.LinkGrabber.QueryPackagesAsync(new PackageQuery { StartAt = 0, MaxResults = 100 });

await device.LinkGrabber.MoveToDownloadListAsync([], [.. packages.Select(p => p.Uuid)]);
```

- The encrypted response body is limited to 1 MiB (about 768 KiB of JSON); a
  larger one fails with a `MyJdProtocolException`: use paging.
- Links and URLs may embed credentials: never log whole `AddLinksRequest`,
  `CrawledLink` or `CrawledPackage` values.
- The library does not filter URL schemes nor destination paths. The device
  fetches the links from its own network and `DestinationFolder` is a path on
  the device: callers handling untrusted input must allow-list both.

## Downloads

`device.Downloads` is the typed facade over `/downloadsV2` (the download list):

```csharp
var packages = await device.Downloads.QueryPackagesAsync(new DownloadPackageQuery { MaxResults = 100 });
var ids = packages.Select(p => p.Uuid).ToList();

await device.Downloads.SetPriorityAsync(Priority.High, [], ids);

// Removes the list entries only; CleanupMode.RemoveLinksAndDeleteFiles would delete files on the device.
await device.Downloads.CleanupAsync([], ids, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All);
```

- Same 1 MiB response limit as above: page with `StartAt` / `MaxResults`.
- `DownloadPackage.DownloadPassword` is returned by the device without an
  opt-out: never log whole records.
- `CleanupAsync` has no confirmation step; destination paths are not filtered.

## DownloadController

`device.DownloadController` is the typed facade over `/downloadcontroller` (start, stop, pause and state of the downloads):

```csharp
await device.DownloadController.StartAsync();
var state = await device.DownloadController.GetCurrentStateAsync(); // DownloadState.Running, ...
await device.DownloadController.PauseAsync(true);
```

## Jd

`device.Jd` is the typed facade over `/jd` (uptime, version, core revision, plugin refresh):

```csharp
TimeSpan uptime = await device.Jd.GetUptimeAsync();
int revision = await device.Jd.GetCoreRevisionAsync();
```

## System

`device.System` is the typed facade over `/system` (restart or exit JDownloader, shut down, standby or hibernate the device):

```csharp
await device.System.RestartAsync();
```

Warning: these actions are destructive and the device usually goes offline before answering, so a timeout or network error does not mean the action failed.

## Config

`device.Config` is the typed facade over `/config` (advanced configuration entries). Values are untyped and exposed as `JsonElement`:

```csharp
using System.Text.Json;
using MyJdownloader.Config;

// Current values are returned only with Values = true; Pattern is a regular expression evaluated on the device.
var entries = await device.Config.QueryAsync(new ConfigQuery { Pattern = "maxsimultanedownloads", Values = true });

var entry = entries[0];
JsonElement current = await device.Config.GetAsync(entry.InterfaceName!, entry.Storage, entry.Key!);

// Applied immediately, without confirmation.
await device.Config.SetAsync(entry.InterfaceName!, entry.Storage, entry.Key!, JsonSerializer.SerializeToElement(3));
```

- Values may hold secrets (passwords, tokens): never log them.
- `SetAsync` sends the value as its JSON text (strings quoted); to be confirmed against a real device.
- Same 1 MiB response limit as above: restrict queries with `ConfigInterface` / `Pattern`.

## Accounts

`device.Accounts` is the typed facade over `/accountsV2` (premium hoster accounts and basic authentication entries):

```csharp
using MyJdownloader.Accounts;

// Credentials are sent to the device; a timeout or network error does not mean the action failed.
await device.Accounts.AddAccountAsync("example-hoster.com", "my-user", passwordFromYourSecretStore);

var accounts = await device.Accounts.ListAccountsAsync();
await device.Accounts.RefreshAccountsAsync([accounts[0].Uuid]);

long authId = await device.Accounts.AddBasicAuthAsync(BasicAuthType.Http, "*.example.com", "my-user", passwordFromYourSecretStore);
```

- Passwords are never retained or logged by the library; do not log them either.
- `ListBasicAuthAsync` returns basic authentication passwords in clear (the device has no opt-out): never log or serialize those entries.

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
