# API coverage

Each row compares two counts for one device namespace. The first is the number of official actions of the namespace, as listed on the [MyJDownloader developer page](https://my.jdownloader.org/developers/). The second is how many of those actions have a typed method in the library, shown as `typed/official`. Actions without a typed method remain reachable through `IDeviceChannel` (see [generic device calls](guides/generic-device-calls.md)). Source: the official list, checked on 2026-11-08 against the action strings called in the `*Api.cs` files.

## Typed facades

| Facade | Namespace | Coverage | Not yet typed |
|---|---|---|---|
| [LinkGrabber](api/link-grabber.md) | `/linkgrabberv2` | 12/29 | `abort`, `addContainer`, `addVariantCopy`, `cleanup`, `getChildrenChanged`, `getDownloadFolderHistorySelectionBase`, `getDownloadUrls`, `getVariants`, `moveLinks`, `movePackages`, `movetoNewPackage`, `queryLinkCrawlerJobs`, `setDownloadDirectory`, `setDownloadPassword`, `setVariant`, `splitPackageByHoster`, `startOnlineStatusCheck` |
| [Downloads](api/downloads.md) | `/downloadsV2` | 14/26 | `getDownloadUrls`, `getStopMark`, `getStopMarkedLink`, `getStructureChangeCounter`, `moveLinks`, `movePackages`, `removeStopMark`, `setDownloadPassword`, `setStopMark`, `splitPackageByHoster`, `startOnlineStatusCheck`, `unskip` |
| [DownloadController](api/download-controller.md) | `/downloadcontroller` | 5/6 | `forceDownload` |
| [Jd](api/jd.md) | `/jd` | 4/6 | `doSomethingCool`, `sum` (test endpoints, not planned) |
| [System](api/system.md) | `/system` | 5/7 | `getStorageInfos`, `getSystemInfos` |
| [Config](api/config.md) | `/config` | 6/7 | `list` |
| [Accounts](api/accounts.md) | `/accountsV2` | ✅ 14/14 | — |
| [Captcha](api/captcha.md) | `/captcha` | ✅ 5/5 | — |
| [Events](api/events.md) | `/events` | ✅ 10/10 | — |
| [Extensions](api/extensions.md) | `/extensions` | ✅ 5/5 | — |
| [Update](api/update.md) | `/update` | ✅ 3/3 | — |
| [Extraction](api/extraction.md) | `/extraction` | ✅ 7/7 | — |

Subtotal for the typed facades: 90/125 actions.

## Namespaces without a typed facade

These namespaces have no typed facade yet. Their actions are callable today through `IDeviceChannel.CallAsync` (see [generic device calls](guides/generic-device-calls.md)).

| Namespace | Coverage | Actions |
|---|---|---|
| `/dialogs` | 0/4 | `answer`, `get`, `getTypeInfo`, `list` |
| `/plugins` | 0/7 | `get`, `getAllPluginRegex`, `getPluginRegex`, `list`, `query`, `reset`, `set` |
| `/log` | 0/2 | `getAvailableLogs`, `sendLogFile` |
| `/contentV2` | 0/4 | `getFavIcon`, `getFileIcon`, `getIcon`, `getIconDescription` |
| `/ui` | 0/2 | `getMenu`, `invokeAction` |
| `/captchaforward` | 0/2 | `createJobRecaptchaV2`, `getResult` |
| `/toolbar` | 0/15 | `addLinksFromDOM`, `checkLinksFromDOM`, `getStatus`, `isAvailable`, `pollCheckedLinksFromDOM`, `specialURLHandling`, `startDownloads`, `stopDownloads`, `toggleAutomaticReconnect`, `toggleClipboardMonitoring`, `toggleDownloadSpeedLimit`, `togglePauseDownloads`, `togglePremium`, `toggleStopAfterCurrentDownload`, `triggerUpdate` |
| `/downloadevents` | 0/2 | `queryLinks`, `setStatusEventInterval` |
| `/device` | 0/3 | `getDirectConnectionInfos`, `getSessionPublicKey`, `ping` |
| `/linkcrawler` | 0/1 | `isCrawling` |
| `/polling` | 0/1 | `poll` |
| `/reconnect` | 0/1 | `doReconnect` |

## Out of scope

The following namespaces are not planned:

- Legacy v1 namespaces: `/accounts`, `/downloads`, `/linkcollector`, `/content`.
- Local HTTP endpoints: `/` and `/flash`.
- `/session`: handshake and disconnect of direct connections. The library uses the relay only (see [ADR-006](adr/ADR-006-relay-transport-and-cryptographic-constraints.md)).

## Totals

Across all official namespaces, 90 of the 169 actions have a typed method (`90/169`).
