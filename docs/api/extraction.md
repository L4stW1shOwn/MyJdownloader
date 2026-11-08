# Extraction

The extraction namespace manages archive extraction on the device: it reports the extraction queue and the state of each archive, adjusts how archives are extracted, stores archive passwords, and starts or cancels an extraction.

**Coverage:** 7/7 actions of `/extraction` (complete).

Reach it as `device.Extraction`, which returns an `ExtractionApi`. The C# namespace is `MyJdownloader.Extraction`, and the device API path is `/extraction`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

Text returned by the device is untrusted: encode it before displaying it. Archive passwords are secrets: they are sent as call arguments and are never retained or logged by the facade. Do not log them either.

## ExtractionApi

`public sealed class ExtractionApi`

Typed facade over the `/extraction` namespace of one device (archive extraction).

| Member | Signature | Description |
|---|---|---|
| `GetQueueAsync` | `Task<IReadOnlyList<ArchiveStatus>> GetQueueAsync(CancellationToken cancellationToken = default)` | Returns the archives being extracted or queued (`/extraction/getQueue`). Empty when the device returns no data. |
| `GetArchiveInfoAsync` | `Task<IReadOnlyList<ArchiveStatus>> GetArchiveInfoAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Returns the archive status of the given links and packages (`/extraction/getArchiveInfo`). Empty lists are sent as empty arrays. `null` throws `ArgumentNullException`. |
| `GetArchiveSettingsAsync` | `Task<IReadOnlyList<ArchiveSettings>> GetArchiveSettingsAsync(IReadOnlyList<string> archiveIds, CancellationToken cancellationToken = default)` | Returns the extraction settings of archives (`/extraction/getArchiveSettings`) by `ArchiveStatus.ArchiveId`. The settings contain passwords in clear: never log them. |
| `SetArchiveSettingsAsync` | `Task<bool> SetArchiveSettingsAsync(string archiveId, ArchiveSettings settings, CancellationToken cancellationToken = default)` | Changes the extraction settings of an archive (`/extraction/setArchiveSettings`) and returns `true` when the device applied the change. Applied immediately, without confirmation. See the remarks for the replacement semantics. |
| `AddArchivePasswordAsync` | `Task AddArchivePasswordAsync(string password, CancellationToken cancellationToken = default)` | Adds a password to the device's list of archive passwords (`/extraction/addArchivePassword`). The password is added to the persisted list. A timeout means the password may have been added. |
| `StartExtractionNowAsync` | `Task<IReadOnlyDictionary<string, bool?>> StartExtractionNowAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Starts the extraction of links and packages immediately (`/extraction/startExtractionNow`). Returns the outcome per archive identifier; the value is `null` when the device gives none. Starts without confirmation. |
| `CancelExtractionAsync` | `Task<bool> CancelExtractionAsync(long controllerId, CancellationToken cancellationToken = default)` | Cancels a running extraction (`/extraction/cancelExtraction`) identified by `ArchiveStatus.ControllerId`, and returns `true` when the device cancelled it. A negative `controllerId` throws `ArgumentOutOfRangeException`. |

Remarks:

- `SetArchiveSettingsAsync` replaces the settings it is given: a member left `null` is not sent and keeps the device default.
- An empty `ArchiveSettings.Passwords` list is sent as is and may clear the candidate passwords of the archive. Leave `Passwords` as `null` to keep them.
- A blank `ArchiveSettings.FinalPassword` is rejected, so this method cannot clear it.
- `ArchiveSettings.ArchiveId`, when set, must be `null` or equal to the `archiveId` argument; otherwise `ArgumentException` is thrown.
- `ArchiveSettings.ExtractPath` is a path on the device. Do not pass untrusted input.
- `SetArchiveSettingsAsync` and `StartExtractionNowAsync` act immediately. A timeout means the action may have been executed.

### Example

```csharp
using MyJdownloader.Extraction;

var queue = await device.Extraction.GetQueueAsync();
foreach (ArchiveStatus archive in queue)
{
    Console.WriteLine($"{archive.ControllerStatus}: {archive.ArchiveId}");
}

if (queue.Count > 0 && queue[0].ArchiveId is { } archiveId)
{
    // Members left null are not sent and keep the device default.
    await device.Extraction.SetArchiveSettingsAsync(archiveId, new ArchiveSettings { AutoExtract = true });
}
```

## ArchiveStatus

`public sealed record ArchiveStatus`

The extraction status of an archive on the device.

Text fields come from the device and must be treated as untrusted.

| Property | Type | Description |
|---|---|---|
| `ArchiveId` | `string?` | Archive identifier, used by the settings methods. |
| `ArchiveName` | `string?` | Archive name. |
| `ControllerId` | `long?` | Extraction controller identifier. `-1` when no controller is assigned; `null` when not returned. Pass it to `CancelExtractionAsync`. |
| `ControllerStatus` | `ExtractionControllerStatus` | State of the extraction controller assigned to the archive. |
| `States` | `IReadOnlyDictionary<string, ArchiveFileStatus>?` | Status of each part file, keyed by file name; `null` when not returned. |
| `Type` | `string?` | Archive type reported by the device (for example `RAR_MULTI`); `null` when not returned. |

## ArchiveSettings

`public sealed record ArchiveSettings`

The extraction settings of an archive, used for reading and updating.

The device returns passwords in clear. Do not log or serialize this record: `ToString()` masks the secrets, but value equality includes them. Members left `null` are not sent, so the device keeps its default.

| Property | Type | Description |
|---|---|---|
| `ArchiveId` | `string?` | Archive identifier. When sent through `SetArchiveSettingsAsync`, it must be `null` or equal to the identifier argument. |
| `AutoExtract` | `bool?` | Whether the archive is extracted automatically. |
| `ExtractPath` | `string?` | Extraction target path on the device. Do not pass untrusted input. |
| `FinalPassword` | `string?` | Final password, returned in clear by the device. A blank value is rejected when sent. |
| `Passwords` | `IReadOnlyList<string>?` | Candidate passwords, returned in clear by the device. An empty list is sent as is and may clear them; `null` keeps them. |
| `RemoveDownloadLinksAfterExtraction` | `bool?` | `true` removes the links from the device download list after extraction. This cannot be undone. |
| `RemoveFilesAfterExtraction` | `bool?` | `true` deletes the archive files on the device after extraction. This cannot be undone. |

## ArchiveFileStatus

`public enum ArchiveFileStatus`

Availability of one part file of an archive, as reported by the device. Unrecognized values are read as `Unknown`.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The value is missing or not recognized by this library. |
| `Complete` | `1` | The part file is completely downloaded (`COMPLETE`). |
| `Incomplete` | `2` | The part file is not completely downloaded (`INCOMPLETE`). |
| `Missing` | `3` | The part file is missing (`MISSING`). |

## ExtractionControllerStatus

`public enum ExtractionControllerStatus`

State of the extraction controller assigned to an archive, as reported by the device. Unrecognized values are read as `Unknown`.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The value is missing or not recognized by this library. |
| `Running` | `1` | The extraction is running (`RUNNING`). |
| `Queued` | `2` | The extraction is queued (`QUEUED`). |
| `Na` | `3` | No controller is assigned to the archive (`NA`). |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
