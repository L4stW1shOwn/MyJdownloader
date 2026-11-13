# Link grabber

The link grabber is JDownloader's staging area: links that have been accepted but not yet moved to the download list. Use it to add links, review the collected links and packages, rename or prioritize them, and then move them to the download list (see [Downloads](downloads.md)).

**Coverage:** 29/29 actions of `/linkgrabberv2` (complete).

Reach it as `device.LinkGrabber`, which returns a `LinkGrabberApi`. The C# namespace is `MyJdownloader.LinkGrabber`, and the device API path is `/linkgrabberv2`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## LinkGrabberApi

`public sealed class LinkGrabberApi`

Typed facade over the `/linkgrabberv2` namespace of one device (links collected but not yet in the download list).

| Member | Signature | Description |
|---|---|---|
| `AddLinksAsync` | `Task<LinkCollectingJob> AddLinksAsync(AddLinksRequest request, CancellationToken cancellationToken = default)` | Adds links to the link grabber (`/linkgrabberv2/addLinks`) and returns the collecting job. Throws `ArgumentNullException` for a null request or link, and `ArgumentException` for a missing link, a link that is blank or contains whitespace, control characters or lone surrogates, a text option (`PackageName`, `DestinationFolder`, `SourceUrl`) that is blank, contains control characters or is longer than its allowed length (see [Argument validation](../guides/error-handling.md#argument-validation)) (the links themselves have no per-link length cap), or `Priority.Unknown` (or an undefined priority). A timeout means the action may have been executed. |
| `AddContainerAsync` | `Task<LinkCollectingJob?> AddContainerAsync(ContainerType type, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)` | Adds the links of a container file (DLC, CCF, RSDF, NZB, crawljob, SFT, SFDL, Metalink) to the link grabber (`/linkgrabberv2/addContainer`) and returns the collecting job, or `null` when the device could not store the container. `ContainerType.Unknown` or an undefined value throws `ArgumentException`, and so does `content` that is empty or larger than 524 288 bytes (512 KiB); both are checked before any request. The library encodes the bytes as a base64 data URL. A timeout means the action may have been executed. |
| `QueryLinksAsync` | `Task<IReadOnlyList<CrawledLink>> QueryLinksAsync(LinkQuery? query = null, CancellationToken cancellationToken = default)` | Lists the collected links (`/linkgrabberv2/queryLinks`). A `null` query returns all links with all fields except the download password. Pages larger than 1 MiB fail with `MyJdProtocolException`: page with `LinkQuery.StartAt` and `LinkQuery.MaxResults`. |
| `QueryPackagesAsync` | `Task<IReadOnlyList<CrawledPackage>> QueryPackagesAsync(PackageQuery? query = null, CancellationToken cancellationToken = default)` | Lists the collected packages (`/linkgrabberv2/queryPackages`). A `null` query returns all fields of all packages. The same 1 MiB limit applies: page with `PackageQuery.StartAt` and `PackageQuery.MaxResults`. |
| `MoveToDownloadListAsync` | `Task MoveToDownloadListAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Moves links and packages to the download list (`/linkgrabberv2/moveToDownloadlist`). A timeout may still have moved them. |
| `RemoveLinksAsync` | `Task RemoveLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Removes links and packages from the link grabber (`/linkgrabberv2/removeLinks`). |
| `ClearListAsync` | `Task<bool> ClearListAsync(CancellationToken cancellationToken = default)` | Clears the whole link grabber list (`/linkgrabberv2/clearList`) and returns the success flag of the device (`true` once the list is cleared). |
| `IsCollectingAsync` | `Task<bool> IsCollectingAsync(CancellationToken cancellationToken = default)` | Returns `true` while links are still being collected (`/linkgrabberv2/isCollecting`). |
| `GetPackageCountAsync` | `Task<int> GetPackageCountAsync(CancellationToken cancellationToken = default)` | Returns the number of packages in the link grabber (`/linkgrabberv2/getPackageCount`). |
| `RenameLinkAsync` | `Task RenameLinkAsync(long linkId, string newName, CancellationToken cancellationToken = default)` | Renames a link (`/linkgrabberv2/renameLink`). The name becomes a file name on the device; `ArgumentNullException` for a null name and `ArgumentException` for a blank name, one with control characters, or one longer than its allowed length (see [Argument validation](../guides/error-handling.md#argument-validation)). |
| `RenamePackageAsync` | `Task RenamePackageAsync(long packageId, string newName, CancellationToken cancellationToken = default)` | Renames a package (`/linkgrabberv2/renamePackage`). Same argument rules as `RenameLinkAsync`. |
| `SetEnabledAsync` | `Task SetEnabledAsync(bool enabled, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Enables or disables links and packages (`/linkgrabberv2/setEnabled`). |
| `SetPriorityAsync` | `Task SetPriorityAsync(Priority priority, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Sets the priority of links and packages (`/linkgrabberv2/setPriority`). `Priority.Unknown` or an undefined value throws `ArgumentException`. |
| `CleanupAsync` | `Task CleanupAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CleanupAction action, CleanupMode mode, SelectionType selectionType, CancellationToken cancellationToken = default)` | Cleans up links and packages of the list (`/linkgrabberv2/cleanup`). Destructive; see the remarks below. `Unknown` or an undefined `action`, `mode` or `selectionType` throws `ArgumentException`, as does `SelectionType.Unselected` with both identifier lists empty (use `SelectionType.All`). |
| `MoveLinksAsync` | `Task MoveLinksAsync(IReadOnlyList<long> linkIds, long afterLinkId, long destinationPackageId, CancellationToken cancellationToken = default)` | Moves links after a given link, into a destination package (`/linkgrabberv2/moveLinks`). `linkIds` must contain at least one (`ArgumentException` otherwise); a negative `afterLinkId` or `destinationPackageId` throws `ArgumentOutOfRangeException`. The meaning of `0` for `afterLinkId` is to be confirmed by live tests. |
| `MovePackagesAsync` | `Task MovePackagesAsync(IReadOnlyList<long> packageIds, long afterPackageId, CancellationToken cancellationToken = default)` | Moves packages after a given package (`/linkgrabberv2/movePackages`). `packageIds` must contain at least one; a negative `afterPackageId` throws `ArgumentOutOfRangeException`. The meaning of `0` for `afterPackageId` is to be confirmed by live tests. |
| `MoveToNewPackageAsync` | `Task MoveToNewPackageAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, string newPackageName, string downloadPath, CancellationToken cancellationToken = default)` | Moves links and packages to a new package (`/linkgrabberv2/movetoNewPackage`). `downloadPath` is a path on the device. A null text throws `ArgumentNullException`; a blank text, one with control characters or lone surrogates, or one longer than its allowed length throws `ArgumentException` (see [Argument validation](../guides/error-handling.md#argument-validation)). The name and path are not filtered. |
| `SplitPackageByHosterAsync` | `Task SplitPackageByHosterAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Splits the selected packages by hoster (`/linkgrabberv2/splitPackageByHoster`). |
| `SetDownloadDirectoryAsync` | `Task SetDownloadDirectoryAsync(string directory, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Sets the download directory of packages (`/linkgrabberv2/setDownloadDirectory`). `directory` is a path on the device, not filtered by the library; same argument rules as `MoveToNewPackageAsync`. |
| `StartOnlineStatusCheckAsync` | `Task StartOnlineStatusCheckAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Starts an online status check of the selected links and packages (`/linkgrabberv2/startOnlineStatusCheck`). |
| `SetDownloadPasswordAsync` | `Task<bool> SetDownloadPasswordAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, string? password, CancellationToken cancellationToken = default)` | Sets or clears the download password of links and packages (`/linkgrabberv2/setDownloadPassword`). `null` clears it, and a blank password throws `ArgumentException`, so only `null` clears. Returns `true` when the device applied the change; an empty answer fails with `MyJdException`. The effect of `null` is to be confirmed by live tests. |
| `GetDownloadUrlsAsync` | `Task<IReadOnlyDictionary<string, IReadOnlyList<long>>> GetDownloadUrlsAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, IReadOnlyList<UrlDisplayType> urlDisplayTypes, CancellationToken cancellationToken = default)` | Gets the link identifiers by download URL (`/linkgrabberv2/getDownloadUrls`). `urlDisplayTypes` must contain at least one value, without duplicates, `Unknown` or undefined values (`ArgumentException`). Empty when the device answers nothing. The accepted `UrlDisplayType` values are to be confirmed by live tests. |
| `GetVariantsAsync` | `Task<IReadOnlyList<LinkVariant>> GetVariantsAsync(long linkId, CancellationToken cancellationToken = default)` | Lists the variants of a link (`/linkgrabberv2/getVariants`). Empty when the device answers nothing. A negative `linkId` throws `ArgumentOutOfRangeException`. |
| `SetVariantAsync` | `Task SetVariantAsync(long linkId, string variantId, CancellationToken cancellationToken = default)` | Selects the variant of a link (`/linkgrabberv2/setVariant`). `variantId` is returned by `GetVariantsAsync`; it throws `ArgumentNullException` when null and `ArgumentException` when blank, with control characters or lone surrogates, or too long. A negative `linkId` throws `ArgumentOutOfRangeException`. |
| `AddVariantCopyAsync` | `Task AddVariantCopyAsync(long linkId, long afterLinkId, long destinationPackageId, string variantId, CancellationToken cancellationToken = default)` | Adds a copy of a link with another variant (`/linkgrabberv2/addVariantCopy`). Same `variantId` rules as `SetVariantAsync`; a negative identifier throws `ArgumentOutOfRangeException`. |
| `QueryLinkCrawlerJobsAsync` | `Task<IReadOnlyList<LinkCrawlerJob>> QueryLinkCrawlerJobsAsync(LinkCrawlerJobQuery? query = null, CancellationToken cancellationToken = default)` | Lists the link crawler jobs (`/linkgrabberv2/queryLinkCrawlerJobs`). A `null` query returns all jobs. Empty when the device answers nothing. A negative job identifier in the query throws `ArgumentOutOfRangeException`. |
| `AbortAsync` | `Task<bool> AbortAsync(CancellationToken cancellationToken = default)` | Aborts the link crawler jobs (`/linkgrabberv2/abort`). Stops every running crawl job (to be confirmed by live tests). Returns the value reported by the device. |
| `AbortAsync` | `Task<bool> AbortAsync(long jobId, CancellationToken cancellationToken = default)` | Aborts one link crawler job (`/linkgrabberv2/abort`). A negative `jobId` throws `ArgumentOutOfRangeException`. Returns the value reported by the device. |
| `GetChildrenChangedAsync` | `Task<long> GetChildrenChangedAsync(long structureWatermark, CancellationToken cancellationToken = default)` | Gets the children change counter of the link grabber (`/linkgrabberv2/getChildrenChanged`). A negative watermark throws `ArgumentOutOfRangeException`. The exact semantics of the watermark and of the result are to be confirmed by live tests. |
| `GetDownloadFolderHistoryAsync` | `Task<IReadOnlyList<string>> GetDownloadFolderHistoryAsync(CancellationToken cancellationToken = default)` | Lists the download folder history (`/linkgrabberv2/getDownloadFolderHistorySelectionBase`). Empty when the device answers nothing. Entries are untrusted device paths that may contain user names or share credentials: do not log them. |

Remarks:

- `CleanupAsync`, `MoveLinksAsync`, `MovePackagesAsync`, `MoveToNewPackageAsync`, `SplitPackageByHosterAsync`, `SetDownloadDirectoryAsync`, `StartOnlineStatusCheckAsync`, `SetDownloadPasswordAsync`, `SetVariantAsync`, `AddVariantCopyAsync`, `AddContainerAsync` and both `AbortAsync` overloads are applied without confirmation. On a timeout or a network error the action may have been executed.
- `AddContainerAsync` parses the container on the device, which treats its content as untrusted. A container may hold credentials (links of a DLC or CCF, servers of an NZB): never log it, nor the `content` argument. The links are added without confirmation, and whether they start follows the device settings or the keys of the container (`autoStart` in a crawljob). A corrupt container still starts a job, which yields no links. Only send containers from a trusted source: a crawljob can set the destination folder, extraction passwords and autostart on the device. The enum is described in [ContainerType](#containertype).
- `CleanupAsync` can delete files on the device, and the library adds no confirmation step: callers are responsible for confirming with the user. With `CleanupMode.RemoveLinksAndDeleteFiles` the files are deleted, with `CleanupMode.RemoveLinksAndRecycleFiles` they are moved to the recycle bin. With `SelectionType.All` the identifiers are ignored, and with `SelectionType.Unselected` every item except the given ones is targeted: combined with `RemoveLinksAndDeleteFiles` this can delete every downloaded file on the device. Targeting every item requires `SelectionType.All`: `Unselected` with both identifier lists empty is rejected. The enums are described in [common types](common-types.md#cleanupaction) ([`CleanupMode`](common-types.md#cleanupmode), [`SelectionType`](common-types.md#selectiontype)).
- The download password given to `SetDownloadPasswordAsync` is a secret: it is sent as a call argument, never retained or logged by the library. Do not log it either.
- The URL keys returned by `GetDownloadUrlsAsync` are untrusted device URLs that may embed credentials: never log them. `UrlDisplayType` is described in [common types](common-types.md#urldisplaytype).
- Variant names and identifiers, and the folders returned by `GetDownloadFolderHistoryAsync`, are untrusted device data.
- The package name and the paths given to `MoveToNewPackageAsync` and `SetDownloadDirectoryAsync` are not filtered: callers handling untrusted input must allow-list them.
- Arguments are validated synchronously, before any request is sent. Exception messages never contain argument values.
- Links and URLs may embed credentials, and packages can carry a download password. Do not log whole `AddLinksRequest`, `CrawledLink` or `CrawledPackage` values.
- The library does not filter URL schemes or destination paths. The device fetches the links from its own network, and `AddLinksRequest.DestinationFolder` is a path on the device. Callers handling untrusted input must allow-list both.
- Identifier lists must not be `null`; passing `null` throws `ArgumentNullException`.

### Example

```csharp
using MyJdownloader.LinkGrabber;

var job = await device.LinkGrabber.AddLinksAsync(new AddLinksRequest
{
    Links = ["https://host.example/file1", "https://host.example/file2"],
    PackageName = "my package",
});

// Pages are limited by the 1 MiB response cap: use StartAt and MaxResults on large lists.
var packages = await device.LinkGrabber.QueryPackagesAsync(new PackageQuery { StartAt = 0, MaxResults = 100 });

await device.LinkGrabber.MoveToDownloadListAsync([], [.. packages.Select(p => p.Uuid)]);

// Pick another variant of a link, then follow the crawler jobs.
var variants = await device.LinkGrabber.GetVariantsAsync(linkId);
if (variants.Count > 0 && variants[0].Id is { } variantId)
{
    await device.LinkGrabber.SetVariantAsync(linkId, variantId);
}

var jobs = await device.LinkGrabber.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { JobIds = [job.Id] });
await device.LinkGrabber.AbortAsync(job.Id);
```

## AddLinksRequest

`public sealed record AddLinksRequest`

Parameters of `/linkgrabberv2/addLinks`. Members left `null` are not sent, so the device keeps its default. The record masks the passwords in `ToString()`, but links and `SourceUrl` may still embed credentials.

| Property | Type | Description |
|---|---|---|
| `Links` | `IReadOnlyList<string>` (required) | The links to add. They are sent as one string joined with line feeds. |
| `PackageName` | `string?` | Name of the package to create. |
| `DestinationFolder` | `string?` | Destination folder on the device. Not filtered by the library. |
| `ExtractPassword` | `string?` | Archive extraction password. |
| `DownloadPassword` | `string?` | Download password. |
| `SourceUrl` | `string?` | URL of the page the links come from. |
| `Priority` | `Priority?` | Priority of the added links. `Priority.Unknown` is rejected. |
| `Autostart` | `bool?` | Whether the downloads start automatically. |
| `AutoExtract` | `bool?` | Whether archives are extracted automatically. |
| `DeepDecrypt` | `bool?` | Whether deep link decryption is used. |
| `OverwritePackagizerRules` | `bool?` | Whether packagizer rules are overwritten. |
| `AssignJobId` | `bool?` | Whether the device returns a job identifier. |

### Example

```csharp
var request = new AddLinksRequest
{
    Links = ["https://host.example/archive.part1.rar"],
    AutoExtract = true,
    Priority = Priority.High,
};
```

## LinkCollectingJob

`public sealed record LinkCollectingJob`

A link collecting job started by `AddLinksAsync`.

| Property | Type | Description |
|---|---|---|
| `Id` | `long` | Job identifier. |

## ContainerType

`public enum ContainerType`

The format of a container file that the device can import, as used by the JDownloader API. Passed to `AddContainerAsync`, which sends the lowercase extension of the member as the format.

| Member | Signature | Description |
|---|---|---|
| `Unknown` | `Unknown = 0` | The value is missing or not recognized. It cannot be sent: `AddContainerAsync` rejects it. |
| `Dlc` | `Dlc = 1` | DLC container (`dlc`). |
| `Ccf` | `Ccf = 2` | CryptLoad CCF container (`ccf`). |
| `Rsdf` | `Rsdf = 3` | RSDF container (`rsdf`). |
| `Nzb` | `Nzb = 4` | NZB Usenet index (`nzb`). |
| `CrawlJob` | `CrawlJob = 5` | JDownloader folder watch crawljob (`crawljob`). |
| `Sft` | `Sft = 6` | SFT container (`sft`). |
| `Sfdl` | `Sfdl = 7` | SFDL container (`sfdl`). |
| `Metalink` | `Metalink = 8` | Metalink 3 document (`metalink`). |
| `Meta4` | `Meta4 = 9` | Metalink 4 document (`meta4`). |

**Remarks**

- The JDownloader backup formats are not exposed. Values outside this list cannot be sent.
- The format is fixed by the library: the caller never supplies a file extension, so no free text reaches the device as a file name.

### Example

```csharp
// A crawljob with autostart off: the link is added but not started.
var crawlJob = System.Text.Encoding.UTF8.GetBytes("text=https://host.example/file1\nautoStart=FALSE\n");
var job = await device.LinkGrabber.AddContainerAsync(ContainerType.CrawlJob, crawlJob);
```

## CrawledLink

`public sealed record CrawledLink`

A link in the link grabber list. Fields that the query did not request are `null`.

Do not destructure or serialize this record into logs. `ToString()` masks `DownloadPassword`, but `Url` may embed credentials.

| Property | Type | Description |
|---|---|---|
| `Uuid` | `long` | Link identifier. |
| `PackageUuid` | `long` | Identifier of the package containing the link. |
| `Name` | `string?` | Link name. |
| `Url` | `string?` | Link URL. May embed credentials; the raw value is not validated. |
| `Host` | `string?` | Host serving the link. |
| `BytesTotal` | `long?` | Total size in bytes. |
| `Availability` | `AvailableLinkState?` | Online availability. |
| `Enabled` | `bool?` | Whether the link is enabled. |
| `Priority` | `Priority?` | Priority. |
| `Comment` | `string?` | Comment. |
| `DownloadPassword` | `string?` | Download password, only returned when requested by the query. |
| `Variants` | `bool?` | Whether the link has variants. |
| `Variant` | `LinkVariant?` | Selected variant. |
| `Status` | `string?` | Human-readable status text, returned when the status flag is set. Not listed in the official MyJDownloader API documentation, so the device may omit it. |
| `StatusIconKey` | `string?` | Key of the status icon. Not listed in the official MyJDownloader API documentation, so the device may omit it. |

## CrawledPackage

`public sealed record CrawledPackage`

A package in the link grabber list. Fields that the query did not request are `null`.

Do not destructure or serialize this record into logs. `ToString()` masks `DownloadPassword`. `QueryPackagesAsync` may still return it, because the API has no flag to opt out.

| Property | Type | Description |
|---|---|---|
| `Uuid` | `long` | Package identifier. |
| `Name` | `string?` | Package name. |
| `BytesTotal` | `long?` | Total size in bytes. |
| `ChildCount` | `int?` | Number of links in the package. |
| `Comment` | `string?` | Comment. |
| `DownloadPassword` | `string?` | Download password, when the device returns it. |
| `Enabled` | `bool?` | Whether the package is enabled. |
| `Hosts` | `IReadOnlyList<string>?` | Hosts serving the package links. |
| `OnlineCount` | `int?` | Number of online links. |
| `OfflineCount` | `int?` | Number of offline links. |
| `TempUnknownCount` | `int?` | Number of temporarily unknown links. |
| `UnknownCount` | `int?` | Number of links with an unknown state. |
| `Priority` | `Priority?` | Priority. |
| `SaveTo` | `string?` | Folder the package is saved to. |
| `Status` | `string?` | Human-readable status text, returned when the status flag is set. Not listed in the official MyJDownloader API documentation, so the device may omit it. |
| `StatusIconKey` | `string?` | Key of the status icon. Not listed in the official MyJDownloader API documentation, so the device may omit it. |

## LinkVariant

`public sealed record LinkVariant`

A selectable variant (for example a quality) of a crawled link.

| Property | Type | Description |
|---|---|---|
| `Id` | `string?` | Variant identifier. |
| `Name` | `string?` | Variant display name. |
| `IconKey` | `string?` | Key of the variant icon. |

## LinkQuery

`public sealed record LinkQuery`

Selects the fields and range returned by `QueryLinksAsync`. Every field flag is `true` by default, except `Password`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Availability` | `bool` | `true` | Returns the availability. |
| `BytesTotal` | `bool` | `true` | Returns the total size. |
| `Comment` | `bool` | `true` | Returns the comment. |
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `Host` | `bool` | `true` | Returns the host. |
| `Priority` | `bool` | `true` | Returns the priority. |
| `Status` | `bool` | `true` | Returns the status. |
| `Url` | `bool` | `true` | Returns the URL. |
| `VariantId` | `bool` | `true` | Returns the variant identifier. |
| `VariantIcon` | `bool` | `true` | Returns the variant icon. |
| `VariantName` | `bool` | `true` | Returns the variant name. |
| `Variants` | `bool` | `true` | Returns the variants flag. |
| `Password` | `bool` | `false` | Returns the download password. |
| `PackageUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these packages. `null` means all. |
| `JobUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these collecting jobs. `null` means all. |
| `MaxResults` | `int` | `-1` | Maximum number of results. `-1` means no limit. Values below `-1` throw `ArgumentException`. |
| `StartAt` | `int` | `0` | Index of the first result. Negative values throw `ArgumentException`. |

### Example

```csharp
var links = await device.LinkGrabber.QueryLinksAsync(new LinkQuery { MaxResults = 200 });
```

## PackageQuery

`public sealed record PackageQuery`

Selects the fields and range returned by `QueryPackagesAsync`. Every field flag is `true` by default.

| Property | Type | Default | Description |
|---|---|---|---|
| `AvailableOfflineCount` | `bool` | `true` | Returns the offline link count. |
| `AvailableOnlineCount` | `bool` | `true` | Returns the online link count. |
| `AvailableTempUnknownCount` | `bool` | `true` | Returns the temporarily unknown link count. |
| `AvailableUnknownCount` | `bool` | `true` | Returns the unknown link count. |
| `BytesTotal` | `bool` | `true` | Returns the total size. |
| `ChildCount` | `bool` | `true` | Returns the child count. |
| `Comment` | `bool` | `true` | Returns the comment. |
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `Hosts` | `bool` | `true` | Returns the hosts. |
| `Priority` | `bool` | `true` | Returns the priority. |
| `SaveTo` | `bool` | `true` | Returns the save folder. |
| `Status` | `bool` | `true` | Returns the status. |
| `PackageUuids` | `IReadOnlyList<long>?` | `null` | Restricts the query to these packages. `null` means all. |
| `MaxResults` | `int` | `-1` | Maximum number of results. `-1` means no limit. Values below `-1` throw `ArgumentException`. |
| `StartAt` | `int` | `0` | Index of the first result. Negative values throw `ArgumentException`. |

## LinkCrawlerJob

`public sealed record LinkCrawlerJob`

The state of a link crawler job, as reported by `QueryLinkCrawlerJobsAsync`. Every property is nullable: the device may omit it.

| Property | Type | Description |
|---|---|---|
| `JobId` | `long?` | Identifier of the collecting job. |
| `CrawlerId` | `long?` | Identifier of the crawler. |
| `Crawling` | `bool?` | Whether the job is still crawling. |
| `Checking` | `bool?` | Whether the job is checking the online status. |
| `Crawled` | `int?` | Number of crawled links. |
| `Broken` | `int?` | Number of broken links. |
| `Filtered` | `int?` | Number of filtered links. |
| `Unhandled` | `int?` | Number of unhandled links. |

## LinkCrawlerJobQuery

`public sealed record LinkCrawlerJobQuery`

Selects the jobs returned by `QueryLinkCrawlerJobsAsync`.

| Property | Type | Default | Description |
|---|---|---|---|
| `JobIds` | `IReadOnlyList<long>?` | `null` | Restricts the query to these jobs. `null` means all. A negative identifier throws `ArgumentOutOfRangeException`. |
| `CollectorInfo` | `bool` | `false` | Asks the device to fill the collector counters of each job (`Crawled`, `Broken`, `Filtered`, `Unhandled`; expected, to be confirmed by live tests). `false` is omitted from the request, which assumes the device default is `false` (to be confirmed by live tests). |

### Example

```csharp
var jobs = await device.LinkGrabber.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { CollectorInfo = true });
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
