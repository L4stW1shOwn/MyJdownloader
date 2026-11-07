# MyJdownloader

A .NET client library for the [MyJDownloader](https://my.jdownloader.org) API.

> **Status: pre-1.0.** The library can connect, reconnect and disconnect,
> export and restore the session (with automatic renewal), list devices and
> make generic encrypted device calls via `GetDevice(id).Channel`. The typed
> `device.LinkGrabber`, `device.Downloads`, `device.DownloadController`, `device.Jd`, `device.System`, `device.Config`, `device.Accounts`, `device.Captcha`, `device.Events`, `device.Extensions`, `device.Extraction` and `device.Update` facades are available; other namespaces come in later versions. The API may still change before 1.0.

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

## Captcha

`device.Captcha` is the typed facade over `/captcha` (pending captcha challenges):

```csharp
using MyJdownloader.Captcha;

var jobs = await device.Captcha.ListAsync();
string image = await device.Captcha.GetImageAsync(jobs[0].Id); // Base64 data URL
bool solved = await device.Captcha.SolveAsync(jobs[0].Id, "answer");
bool skipped = await device.Captcha.SkipAsync(jobs[0].Id, CaptchaSkipRequest.Single);
```

- When a captcha id no longer exists the device usually answers 404 NOT_AVAILABLE, which surfaces as a `MyJdApiException`.
- Values returned by the device (image data URL, `Hoster`, `Explain`, `Type`, `CaptchaCategory`) are untrusted: validate the `data:image/` prefix and decode the Base64 before rendering, and HTML-encode text before embedding it in a page.

## Events

`device.Events` is the typed facade over `/events` (subscriptions and long polling). There is no built-in loop: call `ListenAsync` repeatedly.

```csharp
using System.Net;
using MyJdownloader;
using MyJdownloader.Events;

var subscription = await device.Events.SubscribeAsync(["downloads.*"], cancellationToken: ct);
// Keep the poll timeout well below HttpClient.Timeout (100 s by default).
await device.Events.ChangeSubscriptionTimeoutsAsync(subscription.SubscriptionId, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(120), ct);
try
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var events = await device.Events.ListenAsync(subscription.SubscriptionId, ct);
            if (events.Count == 0)
            {
                // Avoid a hot loop if the device answers immediately with no events.
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            foreach (var evt in events)
            {
                Console.WriteLine($"{evt.Publisher}/{evt.EventId}");
            }
        }
        catch (MyJdApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Expired or unknown subscription (device 404): resubscribe. Other errors propagate.
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            subscription = await device.Events.SubscribeAsync(["downloads.*"], cancellationToken: ct);
            await device.Events.ChangeSubscriptionTimeoutsAsync(subscription.SubscriptionId, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(120), ct);
        }
    }
}
finally
{
    try
    {
        await device.Events.UnsubscribeAsync(subscription.SubscriptionId, CancellationToken.None);
    }
    catch (MyJdException)
    {
        // Best effort: the subscription may already have expired.
    }
}
```

- Subscription and exclusion patterns are regular expressions evaluated on the device.
- `ListenAsync` is bounded by `HttpClient.Timeout`, which the library neither raises nor bypasses: keep the poll timeout well below it (for example 25 s for the default 100 s).
- `DeviceEvent.Publisher` and `EventId` are device-controlled strings: sanitize or encode them before logging or rendering. `DeviceEvent.Data` is untyped and may hold secrets: do not log it. The event shape and the millisecond durations are assumptions to confirm against a real device.

## Extensions

`device.Extensions` is the typed facade over `/extensions`. Text returned by the device is untrusted: encode it before displaying it.

```csharp
using MyJdownloader.DeviceExtensions;

var extensions = await device.Extensions.ListAsync(new ExtensionQuery { Pattern = ".*Extraction.*" }, ct);
foreach (var extension in extensions)
{
    Console.WriteLine($"{extension.Id}: enabled={extension.Enabled}");
}

if (extensions.Count > 0 && extensions[0].Id is { } id)
{
    // Applied immediately, without confirmation; a timeout does not mean the action failed.
    await device.Extensions.SetEnabledAsync(id, true, ct);
}
```

## Update

`device.Update` is the typed facade over `/update`.

```csharp
await device.Update.RunUpdateCheckAsync(ct); // may return before the check finishes
if (await device.Update.IsUpdateAvailableAsync(ct))
{
    // Restarts JDownloader immediately, without confirmation; the device usually goes
    // offline before answering, so a timeout does not mean the action failed.
    await device.Update.RestartAndUpdateAsync(ct);
}
```

## Extraction

`device.Extraction` is the typed facade over `/extraction`.

```csharp
foreach (var archive in await device.Extraction.GetQueueAsync(ct))
{
    Console.WriteLine($"{archive.ArchiveName}: {archive.ControllerStatus}");
}

// Adds the password to the persisted password list of the device.
await device.Extraction.AddArchivePasswordAsync(password, ct);

// Applied immediately, without confirmation; a timeout does not mean the action failed.
// Passwords in ArchiveSettings are masked in ToString() but returned in clear by the device.
await device.Extraction.SetArchiveSettingsAsync(archiveId, new ArchiveSettings { AutoExtract = true }, ct);
```

## Logging and tracing

Device-call URL paths contain the session token and the device id. The named
client registered by `AddMyJDownloader` removes the default HTTP loggers; do
not re-add them (`AddLogger`, extended HTTP logging). The `System.Net.Http`
EventSource and the `url.full` Activity tag redact only the query string, so
exclude this client's traces or add a redacting enricher. A
`MyJDownloaderClient` registered before `AddMyJDownloader` is kept and bypasses
these protections. So does a typed client (`AddHttpClient<MyJDownloaderClient>`):
its `HttpClient` does not get the named client's configuration (redirects
disabled, no loggers, pooled connection lifetime). Use `AddMyJDownloader`.

## Security warning

The exported session contains keys derived from your password. Treat it as a
secret: store it in a protected location (for example DPAPI or a secret
vault), never in plain text, in source control or in logs.

## Design decisions

Architecture decisions are documented in docs/adr in the source
repository.

## License

MIT
