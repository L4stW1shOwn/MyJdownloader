# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `device.LinkGrabber`: typed `/linkgrabberv2` facade (add links, query links and packages, move to download list, remove, rename, enable, priority).
- `MyJDownloaderClient.GetDevice` and `JDownloaderDevice`: per-device entry point exposing the generic device channel.
- `AddMyJDownloader` registers the `MyJDownloaderClient` singleton; the named HTTP client does not follow redirects and has no default HTTP loggers.
- IDeviceChannel: typed, AOT-compatible encrypted calls to any device action through the relay.
- MyJDownloaderClient: automatic single-flight session renewal on TOKEN_INVALID with a single replay of the failed call.
- MyJDownloaderClient: connect, reconnect, disconnect, device listing, session export/restore and SessionChanged notifications.
- DeviceInfo: device identifier, name and type returned by the MyJD server.
- Solution skeleton.
- `MyJDownloaderOptions`.
- `AddMyJDownloader` named HTTP client registration.
- Exception hierarchy: MyJdException, MyJdAuthenticationException, MyJdDeviceOfflineException, MyJdRateLimitException, MyJdApiException, MyJdProtocolException, MyJdErrorSource.
- MyJdSession: immutable session with masked ToString() and versioned ToJson()/FromJson() export.
