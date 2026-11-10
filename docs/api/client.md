# Client API

> Package `MyJdownloader` · namespace `MyJdownloader`

This page covers the entry point of the library: `MyJDownloaderClient` connects to a MyJDownloader account, lists its devices and hands out one `JDownloaderDevice` per device. The session that the client holds (`MyJdSession`) is immutable and carries secrets, so it is documented here together with the events and options that surround it. For the wire-level call mechanism see [device-channel.md](device-channel.md); for error types see [errors.md](errors.md); for enumerations used across facades see [common-types.md](common-types.md).

## MyJDownloaderClient

`public sealed class MyJDownloaderClient`

Client for the MyJDownloader relay server. The client is thread-safe and designed to be a long-lived singleton. It does not own the `HttpClient` it receives and it is not `IDisposable`: the caller disposes the `HttpClient`. The session is kept in memory only; the client never stores the password.

| Member | Signature | Description |
|---|---|---|
| constructor | `MyJDownloaderClient(HttpClient httpClient, MyJDownloaderOptions? options = null, TimeProvider? timeProvider = null)` | Creates the client over a caller-owned `HttpClient`. Options are copied and validated; invalid options throw `ArgumentException`. `timeProvider` defaults to `TimeProvider.System`. |
| `IsConnected` | `bool IsConnected { get; }` | `true` when the client currently holds a session. |
| `ConnectAsync` | `Task<MyJdSession> ConnectAsync(string email, string password, CancellationToken cancellationToken = default)` | Connects to the server and replaces any current session. The password is used once and never stored. |
| `ReconnectAsync` | `Task<MyJdSession> ReconnectAsync(CancellationToken cancellationToken = default)` | Refreshes the current session with its regain token. Throws `InvalidOperationException` when not connected. |
| `DisconnectAsync` | `Task DisconnectAsync(CancellationToken cancellationToken = default)` | Clears the local session before calling the server. The client is disconnected even if the server call fails. |
| `ExportSession` | `MyJdSession? ExportSession()` | Returns the current session, or `null` when not connected. Use it to persist the latest state. |
| `RestoreSession` | `void RestoreSession(MyJdSession session)` | Installs a previously exported session without sending a request and without raising `SessionChanged`. |
| `GetDevicesAsync` | `Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)` | Lists the devices attached to the account. Renews the session once and replays the call once when the server answers `TOKEN_INVALID`. |
| `GetDevice` | `JDownloaderDevice GetDevice(string deviceId)` | Returns the facade root of one device. No request is sent and the device is not checked to exist. |
| `SessionChanged` | `event EventHandler<MyJdSessionChangedEventArgs>? SessionChanged` | Raised when the session is established, refreshed or cleared by the client. Not raised by `RestoreSession`. |

**Remarks**

- `SessionChanged` handlers run on an arbitrary thread; marshal to a UI thread yourself. Persist `ExportSession()` (the latest state) rather than `MyJdSessionChangedEventArgs.Session`, which may already be outdated when the handler runs.
- All handlers are invoked even if one throws. Their exceptions propagate to the caller of the operation after the session has been updated: a single failure is rethrown as is, several failures are wrapped in an `AggregateException`. Check `IsConnected` or `ExportSession()` to know the state.
- During an automatic renewal that the server rejects, the client disconnects and raises `SessionChanged` with a `null` session; the `MyJdAuthenticationException` prevails over handler exceptions.
- Argument validation on `ConnectAsync` (`ArgumentException`, `ArgumentNullException`) is thrown synchronously, before the returned task, so the password is never captured by asynchronous code.
- Security: device-call URL paths carry the session token. Configure the `HttpClient` with a `SocketsHttpHandler` whose `PooledConnectionLifetime` is set and whose `AllowAutoRedirect` is `false`, and do not enable HTTP logging for this client. When you use `AddMyJDownloader`, the library applies these settings for you (see [dependency-injection.md](dependency-injection.md)).

### Example

```csharp
using System.Net.Http;
using MyJdownloader;

var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    AllowAutoRedirect = false,
};
using var http = new HttpClient(handler);
var client = new MyJDownloaderClient(http);

// Read the credentials from a secure source; never hard-code them.
var email = Environment.GetEnvironmentVariable("MYJD_EMAIL")
    ?? throw new InvalidOperationException("MYJD_EMAIL is not set.");
var password = Environment.GetEnvironmentVariable("MYJD_PASSWORD")
    ?? throw new InvalidOperationException("MYJD_PASSWORD is not set.");

MyJdSession session = await client.ConnectAsync(email, password);

// The session holds secrets: persist ToJson() encrypted at rest (your own secret store).
SecretStore.Save("myjd-session", session.ToJson());

IReadOnlyList<DeviceInfo> devices = await client.GetDevicesAsync();
JDownloaderDevice device = client.GetDevice(devices[0].Id);
```

## MyJDownloaderOptions

`public sealed class MyJDownloaderOptions`

Configuration of the MyJDownloader client. The client copies the values when it is constructed: later changes to the instance have no effect on it.

| Member | Signature | Description |
|---|---|---|
| constructor | `MyJDownloaderOptions()` | Creates the options with the default values listed below. |
| `AppKey` | `string AppKey { get; set; }` | Application identifier sent to the `/my/connect` endpoint. Default `"MyJdownloader.NET"`. Must be non-blank, at most 256 characters, without control characters. |
| `BaseAddress` | `Uri BaseAddress { get; set; }` | Base address of the relay server. Default `https://api.jdownloader.org`. Must be an absolute `https` URI without user info, query or fragment. |
| `ApiVersion` | `int ApiVersion { get; set; }` | The `apiVer` value used for device calls. Default `1`. Must be at least `1`. |

**Remarks**

- The defaults target the public MyJDownloader relay. The relay is the only transport in version 1 (ADR-006).
- The setters do not validate. Validation happens once, in the `MyJDownloaderClient` constructor, which throws `ArgumentException` for an invalid value. With dependency injection, the same exception is thrown at the first resolution of the client.
- `AppKey` is not trimmed: leading and trailing spaces are kept as they are.
- `BaseAddress` must not be `null`. HTTPS is required because device-call URL paths carry the session token, and the relay paths are appended to the address.
- `ApiVersion` has no upper bound; the value is written into the encrypted body of every device call.

### Example

```csharp
var options = new MyJDownloaderOptions
{
    AppKey = "MyApp",
    ApiVersion = 1,
};

var client = new MyJDownloaderClient(http, options);
```

## MyJdSession

`public sealed class MyJdSession`

An immutable MyJDownloader session, produced by `ConnectAsync` or `ReconnectAsync` and restored with `FromJson` or `RestoreSession`. It holds the keys derived from the account password and the tokens of the session.

| Member | Signature | Description |
|---|---|---|
| `Email` | `string Email { get; }` | The account e-mail address. The only session field exposed; no secret is public. |
| `ToJson` | `string ToJson()` | Exports the session as versioned JSON that `FromJson` can restore. |
| `FromJson` | `static MyJdSession FromJson(string json)` | Restores a session exported by `ToJson`. Throws `ArgumentNullException` for `null` and `FormatException` for invalid data. |
| `ToString` | `string ToString()` | Returns a representation with a masked e-mail and no secret, for example `MyJdSession { Email = u***@example.com }`. |

**Remarks**

- **Secrets.** The session holds keys derived from the account password. `ToJson()` returns a managed string that cannot be cleared from memory. Store or export the session protected: DPAPI, a secret store, or encryption at rest.
- **No `ToString()` leak.** `ToString()` never reveals a secret, so it is safe in logs and debugger views. Do not log the output of `ToJson()`.
- **Immutability.** A session is never modified. Each refresh produces a new instance, which is why `ExportSession()` should be called again after a `SessionChanged` notification.
- `FromJson` rejects oversized input and returns a `FormatException` whose message never contains a value from the input.
- There is no public constructor: a session is obtained from the client or from `FromJson`.

### Example

```csharp
// Store: encrypted at rest, never in source control or logs.
string json = session.ToJson();
SecretStore.Save("myjd-session", json);

// Restore on the next start, then install it in the client.
MyJdSession restored = MyJdSession.FromJson(SecretStore.Load("myjd-session"));
client.RestoreSession(restored);
```

## MyJdSessionChangedEventArgs

`public sealed class MyJdSessionChangedEventArgs(MyJdSession? session) : EventArgs`

Event data for `MyJDownloaderClient.SessionChanged`. The `Session` value is a snapshot taken when the change occurred; it may already be outdated when the handler runs.

| Member | Signature | Description |
|---|---|---|
| constructor | `MyJdSessionChangedEventArgs(MyJdSession? session)` | Creates the event data. Raised by the client; applications rarely construct it. |
| `Session` | `MyJdSession? Session { get; }` | The session at the time of the change, or `null` when the client is disconnected. |

### Example

```csharp
client.SessionChanged += (_, e) =>
{
    // Runs on an arbitrary thread. Persist the latest state, not e.Session.
    var json = client.ExportSession()?.ToJson();
    if (json is null)
    {
        SecretStore.Delete("myjd-session");
    }
    else
    {
        SecretStore.Save("myjd-session", json);
    }
};
```

## DeviceInfo

`public sealed record DeviceInfo`

A device registered on the MyJD account, as reported by the MyJD server. Returned by `GetDevicesAsync`, used to obtain a `JDownloaderDevice` through `GetDevice`.

| Member | Signature | Description |
|---|---|---|
| `Id` | `string Id { get; init; }` | The device identifier. Pass it to `GetDevice`. Escape it when it is used in a URL. |
| `Name` | `string Name { get; init; }` | The device display name. |
| `Type` | `string Type { get; init; }` | The device type reported by the server. |

**Remarks**

- The values come from the MyJD server and are untrusted: do not log or display them unfiltered.
- Being a record, two instances with the same values compare equal.

### Example

```csharp
IReadOnlyList<DeviceInfo> devices = await client.GetDevicesAsync();
foreach (var info in devices)
{
    Console.WriteLine($"{info.Name} ({info.Type})");
    JDownloaderDevice device = client.GetDevice(info.Id);
}
```

## JDownloaderDevice

`public sealed class JDownloaderDevice`

Entry point of the typed facade for one JDownloader device of the account. Obtained from `MyJDownloaderClient.GetDevice`. The instance holds no session and no secret: it carries the device id and an `IDeviceChannel` that reads the current session of the client on every call. Session renewals are therefore transparent to the device object.

Each facade property is created on first access. The facades are stateless, so a concurrent first access may create more than one instance; this is harmless.

| Member | Signature | Description |
|---|---|---|
| `Id` | `string Id { get; }` | The device identifier, as supplied to `GetDevice`. |
| `Channel` | `IDeviceChannel Channel { get; }` | The channel to the device. Can invoke any device action, including those not yet exposed by a typed facade. See [device-channel.md](device-channel.md). |
| [`LinkGrabber`](link-grabber.md) | `LinkGrabberApi LinkGrabber { get; }` | Facade over the `/linkgrabberv2` namespace: collecting and managing links before download. |
| [`Downloads`](downloads.md) | `DownloadsApi Downloads { get; }` | Facade over the `/downloadsV2` namespace: the download list, packages and links. |
| [`DownloadController`](download-controller.md) | `DownloadControllerApi DownloadController { get; }` | Facade over the `/downloadcontroller` namespace: the state of the download engine (idle, running, paused, stopping, stopped). |
| [`Jd`](jd.md) | `JdApi Jd { get; }` | Facade over the `/jd` namespace: version, uptime and plugin information. |
| [`System`](system.md) | `SystemApi System { get; }` | Facade over the `/system` namespace: device-level actions such as restart or shutdown. |
| [`Config`](config.md) | `ConfigApi Config { get; }` | Facade over the `/config` namespace: reading and writing JDownloader settings. |
| [`Accounts`](accounts.md) | `AccountsApi Accounts { get; }` | Facade over the `/accountsV2` namespace: premium and basic-auth accounts. |
| [`Captcha`](captcha.md) | `CaptchaApi Captcha { get; }` | Facade over the `/captcha` namespace: captcha jobs awaiting a solution. |
| [`Dialogs`](dialogs.md) | `DialogsApi Dialogs { get; }` | Facade over the `/dialogs` namespace: dialogs JDownloader is waiting on. |
| [`Plugins`](plugins.md) | `PluginsApi Plugins { get; }` | Facade over the `/plugins` namespace: plugin settings and URL patterns. |
| [`Events`](events.md) | `EventsApi Events { get; }` | Facade over the `/events` namespace: event subscriptions and polling. |
| [`Extensions`](extensions.md) | `ExtensionsApi Extensions { get; }` | Facade over the `/extensions` namespace (type in `MyJdownloader.DeviceExtensions`). |
| [`Extraction`](extraction.md) | `ExtractionApi Extraction { get; }` | Facade over the `/extraction` namespace: archive extraction. |
| [`Update`](update.md) | `UpdateApi Update { get; }` | Facade over the `/update` namespace: update checks and restart-and-update. |
| [`Device`](device.md) | `DeviceApi Device { get; }` | Facade over the `/device` namespace: liveness check (ping). |
| [`LinkCrawler`](link-crawler.md) | `LinkCrawlerApi LinkCrawler { get; }` | Facade over the `/linkcrawler` namespace: whether the link crawler is running. |
| [`Reconnect`](reconnect.md) | `ReconnectApi Reconnect { get; }` | Facade over the `/reconnect` namespace: reconnect the device internet connection. |
| [`Polling`](polling.md) | `PollingApi Polling { get; }` | Facade over the `/polling` namespace: snapshot of the device state. |
| [`Log`](log.md) | `LogApi Log { get; }` | Facade over the `/log` namespace: log folders and upload of logs to JDownloader support (privacy warning in the page). |
| [`ContentV2`](content-v2.md) | `ContentV2Api ContentV2 { get; }` | Facade over the `/contentV2` namespace: hoster favicons and icons as PNG bytes (unauthenticated, see the page) and icon descriptions. |
| [`Ui`](ui.md) | `UiApi Ui { get; }` | Facade over the `/ui` namespace: the download list context menu and its actions (actions act on the device at once, see the page). |
| [`CaptchaForward`](captchaforward.md) | `CaptchaForwardApi CaptchaForward { get; }` | Facade over the `/captchaforward` namespace: reCAPTCHA v2 challenges solved by the device (the token is a credential, see the page). |
| [`Toolbar`](toolbar.md) | `ToolbarApi Toolbar { get; }` | Facade over the `/toolbar` namespace: status and toggles of the browser-extension toolbar (toggles are not idempotent, see the page). |
| [`DownloadEvents`](downloadevents.md) | `DownloadEventsApi DownloadEvents { get; }` | Facade over the `/downloadevents` namespace: push interval of the download events of an event subscription. |

**Remarks**

- The device id is validated and escaped in request paths, but it is not checked against the account when the object is obtained.
- Destructive facade actions (for example `SystemApi` restart or shutdown) may execute on the device even when the call reports a timeout or a network error. Read the facade page before retrying.

### Example

```csharp
using MyJdownloader;
using MyJdownloader.LinkGrabber;

JDownloaderDevice device = client.GetDevice(deviceId);

// Typed facade: list the links currently in the link grabber.
IReadOnlyList<CrawledLink> links = await device.LinkGrabber.QueryLinksAsync();

// Untyped escape hatch: any device action through the channel.
// See device-channel.md for the JsonTypeInfo overload.
```

## See also

- [Device channel](device-channel.md)
- [Errors](errors.md)
- [Common types](common-types.md)
- [Dependency injection](dependency-injection.md)
- [Link grabber](link-grabber.md) · [Downloads](downloads.md) · [Download controller](download-controller.md) · [Jd](jd.md) · [System](system.md) · [Config](config.md) · [Accounts](accounts.md) · [Captcha](captcha.md) · [Dialogs](dialogs.md) · [Plugins](plugins.md) · [Events](events.md) · [Extensions](extensions.md) · [Extraction](extraction.md) · [Update](update.md) · [Device](device.md) · [Link crawler](link-crawler.md) · [Reconnect](reconnect.md) · [Polling](polling.md) · [Log](log.md) · [ContentV2](content-v2.md)
- [Getting started](../guides/getting-started.md)
- [Sessions and authentication](../guides/sessions-and-authentication.md)
- [Error handling](../guides/error-handling.md)
- [Dependency injection](../guides/dependency-injection.md)
- [ADR-004: immutable session and automatic reconnection](../adr/ADR-004-immutable-session-and-automatic-reconnection.md)
