# Managing downloads

A JDownloader device has three areas, each with its own facade on
`JDownloaderDevice`:

| Facade                    | Area                                                          | Relay path         |
|---------------------------|---------------------------------------------------------------|--------------------|
| `device.LinkGrabber`     | Links collected but not yet in the download list.             | `/linkgrabberv2`   |
| `device.Downloads`       | The download list: links and packages being downloaded.       | `/downloadsV2`     |
| `device.DownloadController` | Global control: start, stop, pause and state of the downloads. | `/downloadcontroller` |

Links usually go through the link grabber first, then to the download list with
`MoveToDownloadListAsync`. The examples below use the `device` variable from
[Getting started](getting-started.md) and a `cancellationToken` from your caller.

## Security notes that apply to every example

- The device fetches the links from its own network. The library does not filter
  URL schemes, and `DestinationFolder` and `SaveTo` are paths on the device.
  Callers that handle untrusted input must allow-list the URLs and the folders.
- Links and URLs can embed credentials. Never log whole `AddLinksRequest`,
  `CrawledLink`, `CrawledPackage`, `DownloadLink` or `DownloadPackage` values.
- `DownloadPackage.DownloadPassword` and `DownloadLink.DownloadPassword` are
  returned by the device without an opt-out.
- Responses are capped at 1 MiB of encrypted body (about 768 KiB of JSON). Page
  long lists with `StartAt` and `MaxResults`. A larger answer fails with
  `MyJdProtocolException`.
- Each action is applied immediately on the device, without confirmation. A
  timeout does not mean the action failed; check the state before replaying it.

## Add links to the link grabber

```csharp
using MyJdownloader.LinkGrabber;

LinkCollectingJob job = await device.LinkGrabber.AddLinksAsync(new AddLinksRequest
{
    Links = ["https://host.example/file1", "https://host.example/file2"],
    PackageName = "my package",
    Autostart = false,
    Priority = Priority.Default,
});

// job.Id identifies the collection job. Collection runs on the device:
// wait for it to finish before listing the packages.
while (await device.LinkGrabber.IsCollectingAsync(cancellationToken))
{
    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
}
```

`AddLinksRequest` also accepts `DestinationFolder`, `DownloadPassword`,
`ExtractPassword`, `AutoExtract`, `DeepDecrypt`, `SourceUrl`, `AssignJobId` and
`OverwritePackagizerRules`. Leave the optional values unset to use the device
settings.

## List the link grabber, page by page

```csharp
const int pageSize = 100;
var packages = new List<CrawledPackage>();

for (var startAt = 0; ; startAt += pageSize)
{
    IReadOnlyList<CrawledPackage> page = await device.LinkGrabber.QueryPackagesAsync(
        new PackageQuery { StartAt = startAt, MaxResults = pageSize },
        cancellationToken);

    packages.AddRange(page);
    if (page.Count < pageSize)
    {
        break;
    }
}
```

`QueryLinksAsync(LinkQuery?)` pages the same way with `LinkQuery.StartAt` and
`LinkQuery.MaxResults`. `GetPackageCountAsync()` returns the number of packages
without fetching them.

## Move links and packages to the download list

```csharp
IReadOnlyList<long> packageIds = packages.Select(p => p.Uuid).ToList();

// linkIds first, then packageIds. Pass an empty list for the other kind.
await device.LinkGrabber.MoveToDownloadListAsync([], packageIds, cancellationToken);
```

`RemoveLinksAsync`, `SetEnabledAsync`, `SetPriorityAsync`, `RenameLinkAsync` and
`RenamePackageAsync` work on the link grabber with the same id lists.
`ClearListAsync()` empties the link grabber.

## List the download list

```csharp
using MyJdownloader.Downloads;

IReadOnlyList<DownloadPackage> downloads = await device.Downloads.QueryPackagesAsync(
    new DownloadPackageQuery { MaxResults = 100 },
    cancellationToken);

IReadOnlyList<DownloadLink> links = await device.Downloads.QueryLinksAsync(
    new DownloadLinkQuery { MaxResults = 100 },
    cancellationToken);
```

Use `DownloadPackageQuery.PackageUuids` or `DownloadLinkQuery.PackageUuids` to
restrict a query to some packages.

## Change priority, rename, move

```csharp
IReadOnlyList<long> ids = downloads.Select(p => p.Uuid).ToList();

await device.Downloads.SetPriorityAsync(Priority.High, [], ids, cancellationToken);

await device.Downloads.RenamePackageAsync(ids[0], "renamed package", cancellationToken);

// Moves some links into a new package stored in another folder on the device.
await device.Downloads.MoveToNewPackageAsync(
    linkIds: [],
    packageIds: ids,
    newPackageName: "archive",
    downloadPath: "/data/archive",   // a path on the device
    cancellationToken);

// Changes the download folder of existing packages.
await device.Downloads.SetDownloadDirectoryAsync("/data/archive", ids, cancellationToken);
```

`Priority` values, from the highest: `Highest`, `Higher`, `High`, `Default`,
`Low`, `Lower`, `Lowest`. `Unknown` is returned when the device sends a value this
library does not know.

## Reset, resume, force

```csharp
await device.Downloads.ResetLinksAsync([], ids, cancellationToken);   // restart from zero
await device.Downloads.ResumeLinksAsync([], ids, cancellationToken);  // continue
bool forced = await device.Downloads.ForceDownloadAsync([], ids, cancellationToken);
```

## Clean up

`CleanupAsync` removes entries from the download list. Its `CleanupMode` decides
what happens to the files:

```csharp
// Removes the finished entries from the list and keeps the files on disk.
await device.Downloads.CleanupAsync(
    linkIds: [],
    packageIds: ids,
    action: CleanupAction.DeleteFinished,
    mode: CleanupMode.RemoveLinksOnly,
    selectionType: SelectionType.All,
    cancellationToken: cancellationToken);
```

- `CleanupMode.RemoveLinksOnly` removes the entries only.
- `CleanupMode.RemoveLinksAndRecycleFiles` and
  `CleanupMode.RemoveLinksAndDeleteFiles` also remove the files on the device.
  Ask for a confirmation before using them.
- `CleanupAsync` has no confirmation step.

`RemoveLinksAsync` removes the given links or packages from the download list
without the cleanup rules.

## Start, stop, pause

```csharp
using MyJdownloader.DownloadController;

await device.DownloadController.StartAsync(cancellationToken);
await device.DownloadController.PauseAsync(true, cancellationToken);   // pause
await device.DownloadController.PauseAsync(false, cancellationToken);  // resume
await device.DownloadController.StopAsync(cancellationToken);

DownloadState state = await device.DownloadController.GetCurrentStateAsync(cancellationToken);
int bytesPerSecond = await device.DownloadController.GetSpeedAsync(cancellationToken);
```

`DownloadState` values: `Idle`, `Running`, `Pause`, `Stopping`, `Stopped` and
`Unknown`. A state this library does not recognize is read as `Unknown`.
`StartAsync`, `StopAsync` and `PauseAsync` return `true` when the device accepted
the request. They do not wait for the state to change: read
`GetCurrentStateAsync` if you need to confirm it.

## Toolbar: status and toggles

The browser-extension toolbar of the device has its own status and settings,
read through `device.Toolbar`.

```csharp
using MyJdownloader.Toolbar;

ToolbarStatus status = await device.Toolbar.GetStatusAsync(cancellationToken);
Console.WriteLine($"Speed: {status.Speed} B/s, speed limit on: {status.SpeedLimitEnabled}");

// Toggles flip a setting and return its new state. They act at once, without confirmation.
bool limitNow = await device.Toolbar.ToggleDownloadSpeedLimitAsync(cancellationToken);
```

- A toggle is not idempotent: after a timeout, a replay flips the setting back.
  Read `GetStatusAsync` before you call it again.
- To pause or resume, prefer `device.DownloadController.PauseAsync(paused)`: it
  takes the target state.
- `TriggerUpdateAsync` only starts an update check in the background. See
  [Update](device-administration.md#update-check-and-restart) to read its result.
- `ToolbarStatus.State` is device text: show it as plain text.
- The page lists the four toolbar actions that are not exposed, and why: [Toolbar](../api/toolbar.md).

## Run an action of the download list context menu

The right-click menu of the download list is read through `device.Ui`. Its
actions run on the links and packages the user selected. The menu belongs to the
device and can be customized there, so take the identifiers from the menu and do
not hard-code them.

```csharp
using MyJdownloader.Ui;

MenuItem menu = await device.Ui.GetMenuAsync(MenuContext.Dlc, cancellationToken);

// After the user confirmed the action, pass the identifier of an Action item.
// The action runs at once, without confirmation on the device.
_ = await device.Ui.InvokeActionAsync(
    MenuContext.Dlc,
    actionId,
    selectedLinkIds,
    selectedPackageIds,
    cancellationToken);
```

- `checkonline` queues an online check of the selection and `forcedls` forces
  the downloads of the selection. Both act on the device at once.
- Items of type `Link` (for example `enable` and `rename`) do nothing on the
  device. Handle them in your own code, with the facade methods of this guide.
- Only the download list context (`MenuContext.Dlc`) is implemented by the device.
- After a timeout, check the download list before you replay the action.

The menu is untrusted device content: show its names as plain text. The full
walk over the menu tree is in the [Ui page](../api/ui.md).
