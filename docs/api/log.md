# Log

The log namespace lists the log folders of the device and uploads selected folders to JDownloader's support servers, so that support can read them.

**Coverage:** 2/2 actions of `/log` (complete).

Reach it as `device.Log`, which returns a `LogApi`. The C# namespace is `MyJdownloader.Log`, and the device API path is `/log`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning: privacy.** `SendLogFileAsync` uploads the selected log folders of the device to JDownloader's support servers, a third party. It acts at once and without any confirmation. The logs can contain personal data: download URLs, file names and paths, account user names and IP addresses. The log of the current session is always sent, whatever the selection. Obtain the explicit consent of the end user before you call it, and never call it unattended (background jobs, batch tools, automated tests against a real device).
>
> The call is long: the device zips and uploads the folders. A `TimeoutException` or `HttpRequestException` means the upload may have been executed. Do not retry blindly: each call uploads the logs again. Cancelling the call does not withdraw the upload either: once the request reached the device, the logs may be sent anyway. Ask for consent before the call, not during it.

## LogApi

`public sealed class LogApi`

Typed facade over the `/log` namespace of one device (log folders and support upload).

| Member | Signature | Description |
|---|---|---|
| `GetAvailableLogsAsync` | `Task<IReadOnlyList<LogFolder>> GetAvailableLogsAsync(CancellationToken cancellationToken = default)` | Lists the log folders of the device (`/log/getAvailableLogs`). The folder of the current session comes first. Returns an empty list when the device returns none. |
| `SendLogFileAsync` | `Task<string?> SendLogFileAsync(IReadOnlyList<LogFolder> logFolders, CancellationToken cancellationToken = default)` | Uploads the selected folders to JDownloader's support servers (`/log/sendLogFile`) and returns the log id. Returns `null` when the device sent nothing: no folder matched, or the upload failed. Throws `ArgumentNullException` for `null`, and `ArgumentException` for an empty list, a `null` element or a folder without `Created` or `LastModified`. These checks run before any request. |

### GetAvailableLogsAsync

Remarks:

- The device writes a thread dump into its current log before it answers. The call therefore writes on the device; it is not read-only. The current log is always part of an upload, so the dump can reach support.
- The content comes from the device and is untrusted.

### SendLogFileAsync

Remarks:

- The device matches each selected folder with an available folder that has the same creation date and a last modification date not earlier than the selected one. Pass the folders returned by `GetAvailableLogsAsync` rather than dates you build yourself.
- The log of the current session is sent whatever the selection, so every upload includes it.
- The device zips each folder and uploads it. It answers once the first upload has returned its id and continues with the other folders in the background: uploads may still run after the call returns.
- The device rejects an empty selection with `400 BAD_PARAMETERS`, which surfaces as a `MyJdApiException`. The library rejects it earlier, without a request.
- The log id is untrusted text. Show it as plain text. It is the identifier that JDownloader support asks for.

## LogFolder

`public sealed record LogFolder`

One log folder of the device, as listed by `GetAvailableLogsAsync` and selected for `SendLogFileAsync`.

| Property | Type | Description |
|---|---|---|
| `Created` | `DateTimeOffset?` | Creation date of the folder, the start of its session. `null` when the device sends no value (zero or less). |
| `Current` | `bool` | `true` for the folder of the current session. |
| `LastModified` | `DateTimeOffset?` | Last modification date of the folder. `null` when the device sends no value (zero or less). |

**Remarks**

- Both dates must be set to select a folder with `SendLogFileAsync`.
- The values come from the device and are untrusted. They hold no personal data by themselves; the log content they designate does.

### Example

```csharp
using System.Linq;
using MyJdownloader.Log;

// 1. List the folders. The device writes a thread dump into its current log first.
IReadOnlyList<LogFolder> folders = await device.Log.GetAvailableLogsAsync(cancellationToken);

// 2. Keep the folders the user chose, with both dates set. The current session is sent in any case.
List<LogFolder> selected = folders
    .Where(folder => folder.Created is not null && folder.LastModified is not null)
    .Where(folder => ChosenByUser(folder))
    .ToList();

// 3. Upload only after the explicit consent of the end user.
if (selected.Count > 0 && AskUser("Send these logs to JDownloader support? They can contain URLs, file names, account names and IP addresses."))
{
    // 4. Upload. After a timeout, the logs may already have been uploaded: do not retry without asking the user.
    string? logId = await device.Log.SendLogFileAsync(selected, cancellationToken);

    // 5. Show the id. It is untrusted device text: encode it before you display it.
    Console.WriteLine(logId is null
        ? "Nothing was sent."
        : $"Support log id: {Encode(logId)}");
}
```

`ChosenByUser`, `AskUser` and `Encode` stand for your own selection screen, user prompt and output encoding.

## Remarks on consent

- The consent is the end user's, not the application's: ask for it in an interactive session, each time the logs are sent.
- Listing the folders is not an upload, but it writes a thread dump into the device log. The example lists first and asks for consent before any upload. Nothing is sent to the support servers until `SendLogFileAsync` is called.

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [Device administration](../guides/device-administration.md)
