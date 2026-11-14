# Local client

> Package `MyJdownloader` · namespace `MyJdownloader.Local`

This page covers the entry point of the local API path: `LocalJDownloaderClient` drives one JDownloader device through its own HTTP API, with no account, no session and no relay. It exposes the same typed facades as a device from `MyJDownloaderClient`. For the usage, the HTTP setup and the choice between the two paths, see [Local API](../guides/local-api.md); for error types see [errors.md](errors.md).

## LocalJDownloaderClient

`public sealed class LocalJDownloaderClient`

Client for the local API served by a JDownloader device. The client is thread-safe as long as the `HttpClient` is, and it is designed to be a long-lived singleton. It does not own the `HttpClient` it receives and it is not `IDisposable`: the caller disposes the `HttpClient`. The client keeps no state.

| Member | Signature | Description |
|---|---|---|
| constructor | `LocalJDownloaderClient(HttpClient httpClient, LocalJDownloaderOptions? options = null)` | Creates the client over a caller-owned `HttpClient`. Options are validated synchronously; `null` uses the defaults (`http://127.0.0.1:3128`). |
| `Device` | `JDownloaderDevice Device { get; }` | The device served by the local API, with its typed facades. Its `Id` is `local`. No request is sent to obtain it. |

**Remarks**

- There is no connect, disconnect, session, session export or reconnection on this client.
- Calls go to `LocalJDownloaderOptions.BaseAddress` only; `HttpClient.BaseAddress` is ignored.
- Configure the handler of the `HttpClient` with `AllowAutoRedirect = false`. A redirect to another origin is rejected after the request has been sent.
- `HttpClient.Timeout` bounds each call, body included. An expired timeout throws `TimeoutException`.
- No exception raised by the client carries the path, the URL, the response body or the arguments.
- Security: the local API has no authentication and no encryption. Anyone who can reach the address can drive the device. Use `https` for a path that leaves the machine (see the guide).

### Example

```csharp
using MyJdownloader;
using MyJdownloader.Local;

using var http = new HttpClient(new SocketsHttpHandler
{
    AllowAutoRedirect = false,
});

// Default address: http://127.0.0.1:3128
var client = new LocalJDownloaderClient(http);

long version = await client.Device.Jd.GetVersionAsync();
```

## LocalJDownloaderOptions

`public sealed class LocalJDownloaderOptions`

Configuration of the local client. The client validates the values once, in its constructor, and works on its own copy: later changes to the instance have no effect on it.

| Member | Signature | Description |
|---|---|---|
| constructor | `LocalJDownloaderOptions()` | Creates the options with the default values listed below. |
| `BaseAddress` | `Uri BaseAddress { get; set; }` | Base address of the local API. Default `http://127.0.0.1:3128`. The API paths are appended to it. |
| `AllowUnencryptedRemoteHost` | `bool AllowUnencryptedRemoteHost { get; set; }` | Accepts an `http` address whose host is not a loopback address. Default `false`. |
| `MaxResponseBodyLength` | `int MaxResponseBodyLength { get; set; }` | Maximum size, in bytes, of an HTTP response body, as received. Default `1048576` (1 MiB). Must be at least `1024`. |

**Remarks**

- The setters do not validate. Validation happens in the `LocalJDownloaderClient` constructor, which throws `ArgumentException` for an invalid value.
- `BaseAddress` is not validated for DNS: no name is resolved. Validation is synchronous and lexical.
- `AllowUnencryptedRemoteHost` makes every call and answer travel in clear text to a remote host, with no authentication. Enable it only on a network you trust.
- `MaxResponseBodyLength` has no upper bound. A larger body throws `MyJdProtocolException` with the message "The MyJD device response exceeds the configured size limit.". Each in-flight call can hold several times this value in memory.

### Validation rules

The constructor validates in this order and throws the first failure.

| Condition | Exception | Message |
|---|---|---|
| `httpClient` is `null` | `ArgumentNullException` (`httpClient`) | Standard message of `ArgumentNullException`. |
| `BaseAddress` is `null`, not absolute, not `http` or `https`, or has user info, a query or a fragment | `ArgumentException` (`options`) | `BaseAddress must be an absolute http or https URI without user info, query or fragment.` |
| `BaseAddress` uses `http`, its host is not loopback, and `AllowUnencryptedRemoteHost` is `false` | `ArgumentException` (`options`) | `BaseAddress uses http with a host that is not a loopback address: use https or set AllowUnencryptedRemoteHost.` |
| `MaxResponseBodyLength` is less than `1024` | `ArgumentException` (`options`) | `MaxResponseBodyLength must be greater than or equal to 1024.` |

The messages never contain the address.

**Loopback hosts.** An `http` address is accepted without `AllowUnencryptedRemoteHost` when its host is:

- the host name `localhost`, case-insensitive;
- an IPv4 literal in `127.0.0.0/8`;
- the IPv6 literal `::1`;
- an IPv4-mapped loopback address (`::ffff:127.x.x.x`).

Any other host is treated as remote, private LAN addresses included, even when a name resolves to loopback. Use `https`, or set `AllowUnencryptedRemoteHost`.

### Example

```csharp
using MyJdownloader.Local;

using var http = new HttpClient(new SocketsHttpHandler
{
    AllowAutoRedirect = false,
});

var options = new LocalJDownloaderOptions
{
    BaseAddress = new Uri("https://192.0.2.10:3128"),
    MaxResponseBodyLength = 4_194_304,
};

// Throws ArgumentException (parameter "options") if the options are not valid.
var client = new LocalJDownloaderClient(http, options);
```

The sample uses a documentation address (RFC 5737). The client accepts it only over `https`, because it is not a loopback address. The certificate of the device is self-signed: pin it on the handler of `http`, as described in the guide.

## See also

- [Local API](../guides/local-api.md): enabling the API, Docker, remote usages and the warning about the missing authentication.
- [Client](client.md): the relay client, `MyJDownloaderClient`.
- [Errors](errors.md): the `MyJdException` hierarchy used by both clients.
- [Device channel](device-channel.md): the generic channel behind the facades.
- [ADR-012: local API transport](../adr/ADR-012-local-api-transport.md).
