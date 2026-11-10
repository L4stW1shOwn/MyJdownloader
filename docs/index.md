# MyJdownloader documentation

MyJdownloader is a .NET client for the MyJDownloader API (<https://my.jdownloader.org>).
It connects to a JDownloader instance through the MyJDownloader relay, lists the
devices of an account, manages links and downloads, and administers the device.

The library is pre-1.0: the API may still change before 1.0. The packages are
`MyJdownloader` (the core library, AOT compatible) and
`MyJdownloader.Extensions.DependencyInjection` (registration in
`Microsoft.Extensions.DependencyInjection`).

New to the library? Start with [Getting started](guides/getting-started.md).

## Guides

Task-oriented guides, one per use case.

- [Getting started](guides/getting-started.md): install the packages, connect, list devices, make a first call and disconnect, with or without dependency injection.
- [Sessions and authentication](guides/sessions-and-authentication.md): connect with email and password, export and restore the session, automatic renewal, the session-changed event and how to store the session.
- [Dependency injection](guides/dependency-injection.md): register the client, its options, enrich the named HTTP client, and the configurations to avoid.
- [Generic device calls](guides/generic-device-calls.md): call any device action through `IDeviceChannel` when no typed facade exists.
- [Managing downloads](guides/managing-downloads.md): add links, work with the link grabber and the download list, control the downloads, run the download list context menu and clean up.
- [Device administration](guides/device-administration.md): version and plugins, power actions, internet reconnect, advanced configuration, accounts, captchas, extensions, updates, archive extraction and support logs.
- [Events](guides/events.md): subscribe to device events and run the long-polling loop you own, with cancellation.
- [Error handling](guides/error-handling.md): the `MyJdException` hierarchy, what to catch, rate limits, device offline, and retry policies.
- [Logging and security](guides/logging-and-security.md): what is sensitive, how to protect the session, and how to configure logging without leaking tokens.

## API reference

Reference pages for each public type and facade.

- [API coverage](api-coverage.md): which official device actions have a typed method, per namespace.
- [Client](api/client.md): `MyJDownloaderClient`, connection, session and device access.
- [Device channel](api/device-channel.md): `IDeviceChannel`, the generic encrypted device call.
- [Errors](api/errors.md): the `MyJdException` hierarchy and its members.
- [Dependency injection](api/dependency-injection.md): `AddMyJDownloader` and the registered services.
- [Common types](api/common-types.md): shared enums and value types, such as `Priority` and `CleanupMode`.
- [Link grabber](api/link-grabber.md): `device.LinkGrabber`, collecting and listing links.
- [Downloads](api/downloads.md): `device.Downloads`, the download list.
- [Download controller](api/download-controller.md): `device.DownloadController`, start, stop, pause and state.
- [Jd](api/jd.md): `device.Jd`, version, uptime, core revision and plugins.
- [System](api/system.md): `device.System`, restart, exit and power actions.
- [Config](api/config.md): `device.Config`, advanced configuration entries.
- [Accounts](api/accounts.md): `device.Accounts`, premium hoster and basic authentication accounts.
- [Captcha](api/captcha.md): `device.Captcha`, pending captcha challenges.
- [Dialogs](api/dialogs.md): `device.Dialogs`, open dialogs, their type info and answers.
- [Plugins](api/plugins.md): `device.Plugins`, plugin settings and URL patterns.
- [Events](api/events.md): `device.Events`, subscriptions and long polling.
- [Extensions](api/extensions.md): `device.Extensions`, JDownloader extensions.
- [Update](api/update.md): `device.Update`, update checks and restart.
- [Extraction](api/extraction.md): `device.Extraction`, archive queue and settings.
- [Device](api/device.md): `device.Device`, liveness check (ping).
- [Link crawler](api/link-crawler.md): `device.LinkCrawler`, whether the link crawler is running.
- [Reconnect](api/reconnect.md): `device.Reconnect`, reconnect the device internet connection.
- [Polling](api/polling.md): `device.Polling`, snapshot of the download, link grabber and captcha state.
- [Log](api/log.md): `device.Log`, log folders and upload of logs to JDownloader support (third party, consent required).
- [ContentV2](api/content-v2.md): `device.ContentV2`, hoster favicons and icons as PNG bytes (untrusted, unauthenticated) and icon descriptions.
- [Ui](api/ui.md): `device.Ui`, the download list context menu and its actions (actions act at once, consent required).

## Architecture decisions

The design choices behind the library (typed facades over a generic channel, the
BCL-only AOT core, the injected `HttpClient`, the immutable session, error
handling and events) are recorded as ADRs.

- [Architecture decision records](adr/README.md): index of ADR-001 to ADR-011.

## Project files

- [README](../README.md): overview and installation.
- [Changelog](../CHANGELOG.md): notable changes per version.
