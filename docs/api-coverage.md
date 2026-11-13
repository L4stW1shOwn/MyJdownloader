# API coverage

Each row compares two counts for one device namespace. The first is the number of official actions of the namespace, as listed on the [MyJDownloader developer page](https://my.jdownloader.org/developers/). The second is how many of those actions have a typed method in the library, shown as `typed/official`. Actions without a typed method remain reachable through `IDeviceChannel` (see [generic device calls](guides/generic-device-calls.md)). Source: the official list, checked on 2026-11-09 against the action strings called in the `*Api.cs` files.

## Typed facades

| Facade | Namespace | Coverage | Not yet typed |
|---|---|---|---|
| [LinkGrabber](api/link-grabber.md) | `/linkgrabberv2` | ✅ 29/29 | — |
| [Downloads](api/downloads.md) | `/downloadsV2` | ✅ 26/26 | — |
| [DownloadController](api/download-controller.md) | `/downloadcontroller` | 5/6 | `forceDownload` |
| [Jd](api/jd.md) | `/jd` | 4/6 | `doSomethingCool`, `sum` (test endpoints, not planned) |
| [System](api/system.md) | `/system` | ✅ 7/7 | — |
| [Config](api/config.md) | `/config` | 6/7 | `list` |
| [Accounts](api/accounts.md) | `/accountsV2` | ✅ 14/14 | — |
| [Captcha](api/captcha.md) | `/captcha` | ✅ 5/5 | — |
| [Dialogs](api/dialogs.md) | `/dialogs` | ✅ 4/4 | — |
| [Plugins](api/plugins.md) | `/plugins` | ✅ 7/7 | — |
| [Events](api/events.md) | `/events` | ✅ 10/10 | — |
| [Extensions](api/extensions.md) | `/extensions` | ✅ 5/5 | — |
| [Update](api/update.md) | `/update` | ✅ 3/3 | — |
| [Extraction](api/extraction.md) | `/extraction` | ✅ 7/7 | — |
| [Device](api/device.md) | `/device` | 1/3 | `getDirectConnectionInfos`, `getSessionPublicKey` |
| [LinkCrawler](api/link-crawler.md) | `/linkcrawler` | ✅ 1/1 | — |
| [Reconnect](api/reconnect.md) | `/reconnect` | ✅ 1/1 | — |
| [Polling](api/polling.md) | `/polling` | ✅ 1/1 | — |
| [Log](api/log.md) | `/log` | ✅ 2/2 | — |
| [ContentV2](api/content-v2.md) | `/contentV2` | ✅ 4/4 | — |
| [Ui](api/ui.md) | `/ui` | ✅ 2/2 | — |
| [CaptchaForward](api/captchaforward.md) | `/captchaforward` | ✅ 2/2 | — |
| [Toolbar](api/toolbar.md) | `/toolbar` | 11/15 | `addLinksFromDOM`, `checkLinksFromDOM`, `pollCheckedLinksFromDOM` (browser-extension DOM protocol with named HTTP parameters, not sendable through `IDeviceChannel`), `specialURLHandling` (returns JavaScript for a browser) |
| [DownloadEvents](api/downloadevents.md) | `/downloadevents` | 1/2 | `queryLinks` (always fails with "Not Implemented" on the device) |

Subtotal for the typed facades: 158/169 actions.

## Out of scope

The following namespaces are not planned:

- Legacy v1 namespaces: `/accounts`, `/downloads`, `/linkcollector`, `/content`.
- Local HTTP endpoints: `/` and `/flash`.
- `/session`: handshake and disconnect of direct connections. The library uses the relay only (see [ADR-006](adr/ADR-006-relay-transport-and-cryptographic-constraints.md)).

## Totals

Across all official namespaces, 158 of the 169 actions have a typed method (`158/169`).
