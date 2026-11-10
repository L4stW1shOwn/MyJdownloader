# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `device.DownloadEvents`: typed `/downloadevents` facade (set the push interval of the `downloads.*` interval events for an events subscription, in whole milliseconds); `queryLinks` is not typed because the device always fails it ("Not Implemented").
- `device.Toolbar`: typed `/toolbar` facade (read the toolbar status, start and stop the downloads, trigger an update check, flip six settings); each toggle returns the new state and is not idempotent, so read the status again after a timeout; the four DOM and script actions are not typed.
- `device.CaptchaForward`: typed `/captchaforward` facade (queue a reCAPTCHA v2 challenge on the device and poll for its token, read once); the job prompts the device user or a solving service, and the token is a credential for the target site: never log it.
- `device.Ui`: typed `/ui` facade (read the download list context menu and run its actions on selected links and packages); actions act on the device at once without confirmation (`checkonline` queues an online check, `forcedls` forces downloads), and the menu is untrusted device content.
- `device.ContentV2`: typed `/contentV2` facade (hoster favicon, file-type icon and icon by key as PNG bytes, icon description); the icon bytes are unauthenticated (ADR-011): the client checks only the status, the `image/png` type, the 1 MiB limit and the PNG signature, so decode them in a sandboxed decoder and never serve them under another content type.
- `device.Log`: typed `/log` facade (list the device log folders, upload selected folders to JDownloader support and get the log id); the upload goes to a third party without confirmation and logs can hold personal data, so obtain the end user's consent first; listing writes a thread dump on the device.
- `device.Polling`: typed `/polling` facade (poll the download progress, download state, link grabber state, waiting captchas and aggregated numbers as a snapshot); the event `data` is untyped JSON and untrusted, and at least one event must be requested.
- `device.Reconnect`: typed `/reconnect` facade (ask the device to reconnect its internet connection); acts without confirmation, interrupts downloads and may fail with `MyJdDeviceOfflineException` or a timeout while the reconnect happens.
- `device.LinkCrawler`: typed `/linkcrawler` facade (is the link crawler running).
- `device.Device`: typed `/device` facade (ping the device); `getDirectConnectionInfos` and `getSessionPublicKey` are not typed.
- `device.Plugins`: typed `/plugins` facade (get, set and reset a plugin setting, list plugins, query plugin entries, get all plugin regexes, get the regexes matching a URL); `QueryAsync` reuses `ConfigQuery` and the config value types; values may hold secrets and are masked in `ToString()`, set and reset calls act without confirmation; patterns are device-side regular expressions and device text is untrusted.
- `device.Dialogs`: typed `/dialogs` facade (list, get, get type info, answer); answering acts on the device without confirmation; device text is untrusted.
- `device.Events.StreamAsync` and `EventStreamOptions`: opt-in event stream that subscribes, resubscribes on expiry (device-side 404) and unsubscribes at the end of the enumeration (cleanup bounded to 10 s, independent of the caller's token); no network retry, events published between expiry and resubscription are lost.
- Package metadata: repository and project URLs (`https://github.com/L4stW1shOwn/MyJdownloader`) on both NuGet packages.
- `device.Extraction`: typed `/extraction` facade (add archive password, cancel extraction, archive info, get and set archive settings, queue, start extraction now); `ArchiveSettings` passwords are masked in `ToString()`, set and start calls act without confirmation.
- `device.Update`: typed `/update` facade (is update available, run update check, restart and update); `RestartAndUpdateAsync` restarts JDownloader without confirmation.
- `device.Extensions`: typed `/extensions` facade (list, install, is installed, is enabled, set enabled); the query pattern is a device-side regular expression.
- `device.Events`: typed `/events` facade (subscribe, listen with long polling, get, add, remove and set subscription patterns, change timeouts, unsubscribe, list publishers); no built-in listen loop, patterns are device-side regular expressions.
- `device.Accounts`: typed `/accountsV2` facade (premium hoster accounts, hoster lists and URLs, basic authentication entries); passwords are never retained or logged, `BasicAuthentication.Password` is masked in `ToString()`.
- `device.Captcha`: typed `/captcha` facade (list and get pending captcha jobs, get the image as a data URL, skip, solve); the answer passed to `SolveAsync` is sent as is and never retained or logged.
- `device.Config`: typed `/config` facade (get, default, set, reset, query, enum options); untyped values as `JsonElement`.
- `device.System`: typed `/system` facade (restart or exit JDownloader, shut down, standby or hibernate the device).
- `device.Jd`: typed `/jd` facade (uptime, version, core revision, plugin refresh).
- `device.DownloadController`: typed `/downloadcontroller` facade (start, stop, pause, current state, download speed).
- `device.Downloads`: typed `/downloadsV2` facade (query links and packages, remove, cleanup, enable, priority, rename, force/reset/resume, download directory, move to new package).
- `device.LinkGrabber`: typed `/linkgrabberv2` facade (add links, query links and packages, move to download list, remove, rename, enable, priority).
- `MyJDownloaderClient.GetDevice` and `JDownloaderDevice`: per-device entry point exposing the generic device channel.
- `AddMyJDownloader` registers the `MyJDownloaderClient` singleton; the named HTTP client does not follow redirects and has no default HTTP loggers.
- IDeviceChannel: typed, AOT-compatible encrypted calls to any device action through the relay.
- MyJDownloaderClient: automatic single-flight session renewal on TOKEN_INVALID with a single replay of the failed call; a session restored during a renewal or a `ReconnectAsync` call is kept.
- MyJDownloaderClient: connect, reconnect, disconnect, device listing, session export/restore and SessionChanged notifications; the current session is hidden from the debugger variables view.
- DeviceInfo: device identifier, name and type returned by the MyJD server.
- Solution skeleton.
- `MyJDownloaderOptions`.
- `AddMyJDownloader` named HTTP client registration.
- Exception hierarchy: MyJdException, MyJdAuthenticationException, MyJdDeviceOfflineException, MyJdRateLimitException, MyJdApiException, MyJdProtocolException, MyJdErrorSource; the service-reported `ErrorType` is capped at 256 characters and its control, format, line and paragraph separator characters and unpaired surrogates are replaced with `?`.
- MyJdSession: immutable session with masked ToString() and versioned ToJson()/FromJson() export.
