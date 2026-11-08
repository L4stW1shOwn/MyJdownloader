# Downloads

The downloads namespace manages the download list: the links and packages JDownloader is downloading, has finished, or is holding for later. Use it to query progress, change priorities and enable state, rename or move entries, reset or force them, and clean them up once they are done.

Reach it as `device.Downloads`, which returns a `DownloadsApi`. The C# namespace is `MyJdownloader.Downloads`, and the device API path is `/downloadsV2`. To start or stop the download queue itself, use [DownloadController](download-controller.md).

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## DownloadsApi

`public sealed class DownloadsApi`

Typed facade over the `/downloadsV2` namespace of one device (the download list).

| Member | Signature | Description |
|---|---|---|
| `QueryLinksAsync` | `Task<IReadOnlyList<DownloadLink>> QueryLinksAsync(DownloadLinkQuery? query = null, CancellationToken cancellationToken = default)` | Lists the download links (`/downloadsV2/queryLinks`). A `null` query returns all fields except the download password. Pages larger than 1 MiB fail with `MyJdProtocolException`: page with `DownloadLinkQuery.StartAt` and `DownloadLinkQuery.MaxResults`. |
| `QueryPackagesAsync` | `Task<IReadOnlyList<DownloadPackage>> QueryPackagesAsync(DownloadPackageQuery? query = null, CancellationToken cancellationToken = default)` | Lists the download packages (`/downloadsV2/queryPackages`). A `null` query returns all fields of all packages. The same 1 MiB limit applies: page with `DownloadPackageQuery.StartAt` and `DownloadPackageQuery.MaxResults`. |
| `GetPackageCountAsync` | `Task<int> GetPackageCountAsync(CancellationToken cancellationToken = default)` | Returns the number of packages in the download list (`/downloadsV2/packageCount`). |
| `RemoveLinksAsync` | `Task RemoveLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Removes entries from the download list (`/downloadsV2/removeLinks`). Downloaded files are kept. Use `CleanupAsync` to delete files. |
| `CleanupAsync` | `Task CleanupAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CleanupAction action, CleanupMode mode, SelectionType selectionType, CancellationToken cancellationToken = default)` | Cleans up entries of the download list (`/downloadsV2/cleanup`). `CleanupMode` decides whether files are deleted or recycled. Destructive; see the remarks below. |
| `ForceDownloadAsync` | `Task<bool> ForceDownloadAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Forces the download of links and packages (`/downloadsV2/forceDownload`) and returns the success flag of the device (`true` once the request is accepted). |
| `ResetLinksAsync` | `Task ResetLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Resets links and packages (`/downloadsV2/resetLinks`). Downloaded data is discarded. |
| `ResumeLinksAsync` | `Task ResumeLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Resumes links and packages (`/downloadsV2/resumeLinks`). |
| `SetEnabledAsync` | `Task SetEnabledAsync(bool enabled, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Enables or disables links and packages (`/downloadsV2/setEnabled`). |
| `SetPriorityAsync` | `Task SetPriorityAsync(Priority priority, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Sets the priority of links and packages (`/downloadsV2/setPriority`). `Priority.Unknown` or an undefined value throws `ArgumentException`. |
| `RenameLinkAsync` | `Task RenameLinkAsync(long linkId, string newName, CancellationToken cancellationToken = default)` | Renames a link (`/downloadsV2/renameLink`). The name becomes a file name on the device. A blank name or one with control characters throws `ArgumentException`. |
| `RenamePackageAsync` | `Task RenamePackageAsync(long packageId, string newName, CancellationToken cancellationToken = default)` | Renames a package (`/downloadsV2/renamePackage`). Same argument rules as `RenameLinkAsync`. |
| `SetDownloadDirectoryAsync` | `Task SetDownloadDirectoryAsync(string directory, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Sets the download directory of packages (`/downloadsV2/setDownloadDirectory`). The directory is a path on the device and is not filtered. |
| `MoveToNewPackageAsync` | `Task MoveToNewPackageAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, string newPackageName, string downloadPath, CancellationToken cancellationToken = default)` | Moves links and packages into a new package (`/downloadsV2/movetoNewPackage`). The package name and download path are not filtered. |

Remarks:

- Arguments are validated synchronously, before any request is sent. Exception messages never contain argument values.
- Links may embed credentials, and packages carry a download password. Do not log whole `DownloadLink` or `DownloadPackage` values.
- Destination paths are paths on the device and are not filtered. Callers handling untrusted input must allow-list them.
- `CleanupAsync` can delete files on the device, and the library adds no confirmation step.
  - `CleanupMode.RemoveLinksAndDeleteFiles` deletes the files on the device. `CleanupMode.RemoveLinksAndRecycleFiles` moves them to the recycle bin. `CleanupMode.RemoveLinksOnly` keeps the files.
  - With `SelectionType.All`, the identifiers are ignored. With `SelectionType.Unselected`, every item except the given ones is targeted, so combined with `RemoveLinksAndDeleteFiles` it can delete every downloaded file on the device.
  - `CleanupAction.Unknown` and `CleanupMode.Unknown` are rejected, as is `SelectionType.Unknown`.

### Example

```csharp
using MyJdownloader.Downloads;

var packages = await device.Downloads.QueryPackagesAsync(new DownloadPackageQuery { MaxResults = 100 });
var ids = packages.Select(p => p.Uuid).ToList();

await device.Downloads.SetPriorityAsync(Priority.High, [], ids);

// Removes the list entries only. RemoveLinksAndDeleteFiles would also delete the files on the device.
await device.Downloads.CleanupAsync([], ids, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All);
```

## DownloadLink

`public sealed record DownloadLink`

A link in the download list. Fields that the query did not request are `null`.

Do not destructure or serialize this record into logs. `ToString()` masks `DownloadPassword`, but `Url` may embed credentials.

| Property | Type | Description |
|---|---|---|
| `Uuid` | `long` | Link identifier. |
| `PackageUuid` | `long` | Identifier of the package containing the link. |
| `Name` | `string?` | Link name. |
| `Url` | `string?` | Link URL. May embed credentials. |
| `Host` | `string?` | Host serving the link. |
| `Comment` | `string?` | Comment. |
| `DownloadPassword` | `string?` | Download password, when requested. |
| `BytesLoaded` | `long?` | Downloaded size in bytes. |
| `BytesTotal` | `long?` | Total size in bytes. |
| `Speed` | `long?` | Current speed in bytes per second. |
| `Eta` | `TimeSpan?` | Estimated time remaining, or `null` when unknown. |
| `Enabled` | `bool?` | Whether the link is enabled. |
| `Finished` | `bool?` | Whether the download is finished. |
| `Running` | `bool?` | Whether the link is downloading. |
| `Skipped` | `bool?` | Whether the link was skipped. |
| `Priority` | `Priority?` | Priority. |
| `Status` | `string?` | Human-readable status text. |
| `StatusIconKey` | `string?` | Key of the status icon. |
| `ExtractionStatus` | `string?` | Archive extraction status. Its set of values is not documented. |
| `AddedDate` | `DateTimeOffset?` | Date the link was added, or `null` when not set. |
| `FinishedDate` | `DateTimeOffset?` | Date the download finished, or `null` when not set. |

## DownloadPackage

`public sealed record DownloadPackage`

A package in the download list. Fields that the query did not request are `null`.

Do not destructure or serialize this record into logs. `ToString()` masks `DownloadPassword`. `QueryPackagesAsync` may still return it, because the API has no flag to opt out.

| Property | Type | Description |
|---|---|---|
| `Uuid` | `long` | Package identifier. |
| `Name` | `string?` | Package name. |
| `ActiveTask` | `string?` | Task currently running on the package. Its set of values is not documented. |
| `BytesLoaded` | `long?` | Downloaded size in bytes. |
| `BytesTotal` | `long?` | Total size in bytes. |
| `Speed` | `long?` | Current speed in bytes per second. |
| `Eta` | `TimeSpan?` | Estimated time remaining, or `null` when unknown. |
| `ChildCount` | `int?` | Number of links in the package. |
| `Comment` | `string?` | Comment. |
| `DownloadPassword` | `string?` | Download password. The API returns it without an opt-out flag. |
| `Enabled` | `bool?` | Whether the package is enabled. |
| `Finished` | `bool?` | Whether the package is finished. |
| `Running` | `bool?` | Whether the package is downloading. |
| `Hosts` | `IReadOnlyList<string>?` | Hosts serving the package links. |
| `Priority` | `Priority?` | Priority. |
| `SaveTo` | `string?` | Folder the package is saved to. |
| `Status` | `string?` | Human-readable status text. |
| `StatusIconKey` | `string?` | Key of the status icon. |

## DownloadLinkQuery

`public sealed record DownloadLinkQuery`

Selects the fields and range returned by `QueryLinksAsync`. Every field flag is `true` by default, except `Password`.

| Property | Type | Default | Description |
|---|---|---|---|
| `AddedDate` | `bool` | `true` | Returns the added date. |
| `BytesLoaded` | `bool` | `true` | Returns the downloaded size. |
| `BytesTotal` | `bool` | `true` | Returns the total size. |
| `Comment` | `bool` | `true` | Returns the comment. |
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `Eta` | `bool` | `true` | Returns the estimated time remaining. |
| `ExtractionStatus` | `bool` | `true` | Returns the extraction status. |
| `Finished` | `bool` | `true` | Returns the finished flag. |
| `FinishedDate` | `bool` | `true` | Returns the finished date. |
| `Host` | `bool` | `true` | Returns the host. |
| `Priority` | `bool` | `true` | Returns the priority. |
| `Running` | `bool` | `true` | Returns the running flag. |
| `Skipped` | `bool` | `true` | Returns the skipped flag. |
| `Speed` | `bool` | `true` | Returns the speed. |
| `Status` | `bool` | `true` | Returns the status. |
| `Url` | `bool` | `true` | Returns the URL. |
| `Password` | `bool` | `false` | Returns the download password. |
| `PackageUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these packages. `null` means all. |
| `JobUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these collecting jobs. `null` means all. |
| `MaxResults` | `int` | `-1` | Maximum number of results. `-1` means no limit. Values below `-1` throw `ArgumentException`. |
| `StartAt` | `int` | `0` | Index of the first result. Negative values throw `ArgumentException`. |

## DownloadPackageQuery

`public sealed record DownloadPackageQuery`

Selects the fields and range returned by `QueryPackagesAsync`. Every field flag is `true` by default.

| Property | Type | Default | Description |
|---|---|---|---|
| `BytesLoaded` | `bool` | `true` | Returns the downloaded size. |
| `BytesTotal` | `bool` | `true` | Returns the total size. |
| `ChildCount` | `bool` | `true` | Returns the child count. |
| `Comment` | `bool` | `true` | Returns the comment. |
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `Eta` | `bool` | `true` | Returns the estimated time remaining. |
| `Finished` | `bool` | `true` | Returns the finished flag. |
| `Hosts` | `bool` | `true` | Returns the hosts. |
| `Priority` | `bool` | `true` | Returns the priority. |
| `Running` | `bool` | `true` | Returns the running flag. |
| `SaveTo` | `bool` | `true` | Returns the save folder. |
| `Speed` | `bool` | `true` | Returns the speed. |
| `Status` | `bool` | `true` | Returns the status. |
| `PackageUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these packages. `null` means all. |
| `MaxResults` | `int` | `-1` | Maximum number of results. `-1` means no limit. Values below `-1` throw `ArgumentException`. |
| `StartAt` | `int` | `0` | Index of the first result. Negative values throw `ArgumentException`. |

### Example

```csharp
var links = await device.Downloads.QueryLinksAsync(new DownloadLinkQuery { MaxResults = 50 });
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
