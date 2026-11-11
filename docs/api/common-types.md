# Common Types

> Package `MyJdownloader` · namespace `MyJdownloader`

Enumerations shared by several facades. Each enumeration has an `Unknown` member with the value `0`: a value that the device returns and that this library does not recognize is read as `Unknown`, and `Unknown` cannot be sent to the device. Pass one of the other members when you call an action. The numeric values are explicit and fixed by the library.

## AvailableLinkState

`public enum AvailableLinkState`

Online availability of a link, as reported by the JDownloader API. Returned by the link grabber, where a link can be checked before it is downloaded.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The state is missing or not recognized by this library. |
| `Online` | `Online = 1` | The link is online (`ONLINE`). |
| `Offline` | `Offline = 2` | The link is offline (`OFFLINE`). |
| `TempUnknown` | `TempUnknown = 3` | The state is temporarily unknown (`TEMP_UNKNOWN`). The API's own `UNKNOWN` value is also read as `Unknown`. |

### Example

```csharp
IReadOnlyList<CrawledLink> links = await device.LinkGrabber.QueryLinksAsync();

var offline = links.Where(link => link.Availability == AvailableLinkState.Offline);
```

## CleanupAction

`public enum CleanupAction`

Selects which links a cleanup removes, as used by the JDownloader API. Passed to the cleanup action of the downloads facade.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent: the downloads facade rejects it. |
| `DeleteAll` | `DeleteAll = 1` | All links (`DELETE_ALL`). |
| `DeleteDisabled` | `DeleteDisabled = 2` | Disabled links (`DELETE_DISABLED`). |
| `DeleteFailed` | `DeleteFailed = 3` | Failed links (`DELETE_FAILED`). |
| `DeleteFinished` | `DeleteFinished = 4` | Finished links (`DELETE_FINISHED`). |
| `DeleteOffline` | `DeleteOffline = 5` | Offline links (`DELETE_OFFLINE`). |
| `DeleteDupe` | `DeleteDupe = 6` | Duplicate links (`DELETE_DUPE`). |
| `DeleteMode` | `DeleteMode = 7` | Links matching the mode-specific rule (`DELETE_MODE`). |

**Remarks**

- `CleanupAction` chooses which links are targeted; the cleanup then applies a [`CleanupMode`](#cleanupmode) to them and a [`SelectionType`](#selectiontype) to the identifiers given. `DownloadsApi.CleanupAsync` rejects `Unknown` for all three.
- Destructive: with `CleanupMode.RemoveLinksAndDeleteFiles` the files are deleted on the device. The library adds no confirmation step; confirm with the user first. See the [downloads](downloads.md) page for the full call.

### Example

```csharp
// Remove the finished links from the download list, keeping their files on disk.
await device.Downloads.CleanupAsync(
    linkIds: [],
    packageIds: [],
    action: CleanupAction.DeleteFinished,
    mode: CleanupMode.RemoveLinksOnly,
    selectionType: SelectionType.All);
```

## CleanupMode

`public enum CleanupMode`

Selects what a cleanup does with the links and their files, as used by the JDownloader API.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent. |
| `RemoveLinksAndDeleteFiles` | `RemoveLinksAndDeleteFiles = 1` | Removes the links and **deletes their files** (`REMOVE_LINKS_AND_DELETE_FILES`). Destructive: this deletes files on the device. |
| `RemoveLinksAndRecycleFiles` | `RemoveLinksAndRecycleFiles = 2` | Removes the links and moves their files to the recycle bin (`REMOVE_LINKS_AND_RECYCLE_FILES`). |
| `RemoveLinksOnly` | `RemoveLinksOnly = 3` | Removes the links and keeps their files (`REMOVE_LINKS_ONLY`). |

### Example

```csharp
// Safer default: keep the files on disk, drop only the list entries.
CleanupMode mode = CleanupMode.RemoveLinksOnly;
```

## Priority

`public enum Priority`

Download priority of a link or a package, as used by the JDownloader API. Values are ordered from the highest priority (`Highest`, `1`) to the lowest (`Lowest`, `7`).

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized by this library. It cannot be sent. |
| `Highest` | `Highest = 1` | Highest priority (`HIGHEST`). |
| `Higher` | `Higher = 2` | Higher priority (`HIGHER`). |
| `High` | `High = 3` | High priority (`HIGH`). |
| `Default` | `Default = 4` | Default priority (`DEFAULT`). |
| `Low` | `Low = 5` | Low priority (`LOW`). |
| `Lower` | `Lower = 6` | Lower priority (`LOWER`). |
| `Lowest` | `Lowest = 7` | Lowest priority (`LOWEST`). |

### Example

```csharp
// Raise the priority of the selected links in the link grabber.
await device.LinkGrabber.SetPriorityAsync(Priority.Higher, linkIds, packageIds);
```

## SelectionType

`public enum SelectionType`

Selects which of the given items an action applies to, as used by the JDownloader API.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent. |
| `Selected` | `Selected = 1` | Only the given items (`SELECTED`). |
| `Unselected` | `Unselected = 2` | Every item except the given ones (`UNSELECTED`). |
| `All` | `All = 3` | All items (`ALL`). The given identifiers are ignored. |
| `None` | `None = 4` | No item (`NONE`). |

**Remarks**

- With `Unselected`, every item except the given ones is targeted. Combined with `CleanupMode.RemoveLinksAndDeleteFiles`, this can delete every downloaded file on the device. Use `Selected` when you mean a precise list.

## SkipReason

`public enum SkipReason`

Why a download link is skipped, as reported by the JDownloader API. Passed to the unskip action of the downloads facade.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent: the downloads facade rejects it. |
| `ConnectionUnavailable` | `ConnectionUnavailable = 1` | No connection available (`CONNECTION_UNAVAILABLE`). |
| `TooManyRetries` | `TooManyRetries = 2` | Too many retries (`TOO_MANY_RETRIES`). |
| `Captcha` | `Captcha = 3` | A captcha is required (`CAPTCHA`). |
| `Manual` | `Manual = 4` | Skipped manually (`MANUAL`). |
| `DiskFull` | `DiskFull = 5` | The disk is full (`DISK_FULL`). |
| `NoAccount` | `NoAccount = 6` | No account available (`NO_ACCOUNT`). |
| `InvalidDestination` | `InvalidDestination = 7` | Invalid destination (`INVALID_DESTINATION`). |
| `FileExists` | `FileExists = 8` | The file already exists (`FILE_EXISTS`). |
| `UpdateRestartRequired` | `UpdateRestartRequired = 9` | An update restart is required (`UPDATE_RESTART_REQUIRED`). |
| `FfmpegMissing` | `FfmpegMissing = 10` | FFmpeg is missing (`FFMPEG_MISSING`). |
| `FfprobeMissing` | `FfprobeMissing = 11` | FFprobe is missing (`FFPROBE_MISSING`). |

**Remarks**

- Unrecognized values returned by a device are read as `Unknown`.

## UrlDisplayType

`public enum UrlDisplayType`

Which URL a download link displays, as used by the JDownloader API. Passed to the download URLs action of the downloads facade.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent: the downloads facade rejects it. |
| `Custom` | `Custom = 1` | Custom URL (`CUSTOM`). |
| `Referrer` | `Referrer = 2` | Referrer URL (`REFERRER`). |
| `Origin` | `Origin = 3` | Origin URL (`ORIGIN`). |
| `Container` | `Container = 4` | Container URL (`CONTAINER`). |
| `Content` | `Content = 5` | Content URL (`CONTENT`). |

**Remarks**

- Unrecognized values returned by a device are read as `Unknown`.
- The values are to be confirmed by live tests.

## See also

- [Client API](client.md)
- [Device channel](device-channel.md)
- [Errors](errors.md)
- [Dependency injection](dependency-injection.md)
- [Downloads](downloads.md) · [Link grabber](link-grabber.md) · [Download controller](download-controller.md)
- [Getting started](../guides/getting-started.md)
