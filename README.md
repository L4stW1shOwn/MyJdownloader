# MyJdownloader

[![CI](https://github.com/L4stW1shOwn/MyJdownloader/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/L4stW1shOwn/MyJdownloader/actions/workflows/ci.yml)
[![Release](https://github.com/L4stW1shOwn/MyJdownloader/actions/workflows/create-release.yml/badge.svg)](https://github.com/L4stW1shOwn/MyJdownloader/actions/workflows/create-release.yml)
[![NuGet](https://img.shields.io/nuget/v/MyJdownloader)](https://www.nuget.org/packages/MyJdownloader)
[![NuGet DI](https://img.shields.io/nuget/v/MyJdownloader.Extensions.DependencyInjection)](https://www.nuget.org/packages/MyJdownloader.Extensions.DependencyInjection)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/LICENSE)

A .NET client for the [MyJDownloader](https://my.jdownloader.org) API. It connects
an application to a JDownloader instance through the MyJDownloader relay, lists the
devices of an account, manages links, downloads and archives, and administers the
device. The core library depends on the base class library only and is AOT
compatible; the companion package registers the client with dependency injection.

> **Status: pre-1.0.** The API may still change before 1.0. Breaking changes are
> listed in the [changelog](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/CHANGELOG.md).

## Installation

```shell
dotnet add package MyJdownloader
dotnet add package MyJdownloader.Extensions.DependencyInjection
```

The second package is optional: it registers the client in
`Microsoft.Extensions.DependencyInjection`.

## Usage

Register the client, connect with your account, and add links to a device:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;
using MyJdownloader.LinkGrabber;

var services = new ServiceCollection();
services.AddMyJDownloader(o => o.AppKey = "MyApp");

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<MyJDownloaderClient>();

await client.ConnectAsync(email, password); // read them from a secret store

var devices = await client.GetDevicesAsync();
var device = client.GetDevice(devices[0].Id);

await device.LinkGrabber.AddLinksAsync(new AddLinksRequest
{
    Links = ["https://host.example/file1"],
    PackageName = "my package",
});
```

Export the session to avoid reconnecting with the password, then restore it:

```csharp
var json = client.ExportSession()?.ToJson(); // store it protected, see below
// Later, in another run:
client.RestoreSession(MyJdSession.FromJson(json!));
```

Without dependency injection, create the client yourself with a long-lived
`HttpClient`. The [getting started guide](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/docs/guides/getting-started.md)
shows both setups.

> **Security:** the exported session holds keys derived from your password. Store it
> in a protected location and never log it. See the
> [logging and security guide](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/docs/guides/logging-and-security.md).

## API coverage

The typed facades cover 157 of the 169 official device actions; the others stay reachable through `IDeviceChannel`.

| Facade | Coverage |
|---|---|
| LinkGrabber | 12/29 |
| Downloads | 14/26 |
| DownloadController | 5/6 |
| Jd | 4/6 |
| System | ✅ 7/7 |
| Config | 6/7 |
| Accounts | ✅ 14/14 |
| Captcha | ✅ 5/5 |
| Dialogs | ✅ 4/4 |
| Plugins | ✅ 7/7 |
| Events | ✅ 10/10 |
| Extensions | ✅ 5/5 |
| Update | ✅ 3/3 |
| Extraction | ✅ 7/7 |
| Device | 1/3 |
| LinkCrawler | ✅ 1/1 |
| Reconnect | ✅ 1/1 |
| Polling | ✅ 1/1 |
| Log | ✅ 2/2 |
| ContentV2 | ✅ 4/4 |
| Ui | ✅ 2/2 |
| CaptchaForward | ✅ 2/2 |
| Toolbar | 11/15 |
| DownloadEvents | 1/2 |

Full page: [API coverage](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/docs/api-coverage.md).

## Documentation

- [Documentation index](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/docs/index.md):
  guides for each use case, and the API reference.
- [Architecture decisions](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/docs/adr/README.md)
  (ADRs) behind the design.

## License

MIT. See [LICENSE](https://github.com/L4stW1shOwn/MyJdownloader/blob/main/LICENSE).
