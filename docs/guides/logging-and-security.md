# Logging and security

This guide lists what is sensitive in the library, what the library protects for
you, and what you must do yourself: where the session lives, how HTTP logging must
be configured, and what not to log.

## What is sensitive

| Data                                  | Where it appears                                              | Rule                                                        |
|---------------------------------------|---------------------------------------------------------------|-------------------------------------------------------------|
| Account password                      | Argument of `ConnectAsync`                                    | Never stored by the library. Keep it in your secret store.  |
| Exported session (`ToJson()`)         | Your storage                                                  | A secret: keys derived from the password and session tokens. |
| Session token and device id in URLs   | Device-call paths (`/t_<sessionToken>_<deviceId><path>`)      | Never log full request URIs.                                |
| Links and URLs with credentials       | `AddLinksRequest`, `CrawledLink`, `DownloadLink`              | Never log whole records.                                    |
| Download passwords                    | `DownloadPackage`, `DownloadLink`, `CrawledPackage`           | Returned by the device without opt-out. Never log records.  |
| Basic authentication passwords        | `BasicAuthentication.Password`, from `ListBasicAuthAsync`     | Returned in clear. Never log or serialize them.             |
| Archive passwords                     | `ArchiveSettings`                                             | Masked in `ToString()`, returned in clear by the device.    |
| Config values                         | `ConfigEntry.Value`, `ConfigEntry.DefaultValue`               | May hold secrets. Never log them.                           |
| Event data                            | `DeviceEvent.Data`                                            | Untyped, may hold secrets. Do not log it.                   |
| Text from the device                  | Names, errors (`ErrorType`), captcha images and text, events  | Untrusted. Encode before rendering.                         |

`MyJdSession.ToString()` shows only a masked email and no secret, so it is safe
to log. Do not rely on `ToString()` for the other types: log the fields you need.

## The session is a credential

The exported session holds keys derived from your password. Anyone who has it can
act on your account until the tokens expire. Protect it:

- Store it in a protected location: a platform protection API (for example
  DPAPI on Windows), an encrypted file, or a secret vault.
- Never write it in plain text, in source control, in configuration files that
  are committed, in logs, in telemetry or in exception messages.
- Do not keep the password next to the session for reconnection. The client does
  not need it: the regain token renews the session. Use the password only for
  `ConnectAsync`.

The library cannot clear a `string` from memory, so a session exported as JSON
stays in memory until the garbage collector reclaims it. Keep the time during
which you hold it short.

## Logging HTTP traffic

Device-call URL paths contain the session token and the device id. The
`System.Net.Http` diagnostics would expose them. The rules are:

- The named client registered by `AddMyJDownloader` removes the default HTTP
  loggers. Do not add them back: no `AddLogger`, no `AddDefaultLogger`, no
  extended HTTP logging on this client.
- The `System.Net.Http` EventSource and the `url.full` Activity tag redact only the
  query string, not the path. Exclude this client's traces from your tracing
  pipeline, or add a redacting enricher that removes the path.
- A `MyJDownloaderClient` you register before `AddMyJDownloader` is kept, and
  bypasses these protections. The same goes for a typed client
  (`AddHttpClient<MyJDownloaderClient>`), whatever the registration order: it
  registers a transient `MyJDownloaderClient` that replaces the singleton, and its
  `HttpClient` does not get the named client's configuration (redirects disabled,
  no loggers, pooled connection lifetime). Use `AddMyJDownloader` only.
- With a caller-built `HttpClient` (see [Getting started](getting-started.md)),
  apply the same rules yourself: `AllowAutoRedirect = false`, a
  `PooledConnectionLifetime`, and no HTTP logging.

## Log the library's behavior safely

Use `ILogger<T>` with message templates, and log identifiers and outcomes, not
content:

```csharp
logger.LogInformation("Submitted {LinkCount} links to the link grabber", request.Links.Count);
logger.LogWarning("Device call {ErrorType} failed with HTTP {StatusCode}", ex.ErrorType, ex.StatusCode);
```

Never log:

- the password, the exported session or any token;
- request or response bodies, request URLs and arguments of device calls;
- whole records of links, packages, accounts or captchas;
- `DeviceEvent.Data`, `ConfigEntry.Value` and archive passwords.

Encode the text from the device (`ErrorType`, `DeviceEvent.Publisher`,
`EventId`, names) before you put it in a log line that a viewer renders as
markup.

## Operational checklist

- [ ] The password comes from a secret store and is not kept after `ConnectAsync`.
- [ ] The exported session is encrypted or in a vault, and it is deleted on disconnect.
- [ ] `AddMyJDownloader` is the only way the client is registered, and no HTTP logger is added to it.
- [ ] Tracing and logging exclude or redact this client's URLs.
- [ ] Device text is encoded before it is displayed.
- [ ] Destinations and URLs that come from users are allow-listed.

The security decisions behind the session design are recorded in
[ADR-004](../adr/ADR-004-immutable-session-and-automatic-reconnection.md) and
[ADR-006](../adr/ADR-006-relay-transport-and-cryptographic-constraints.md).
