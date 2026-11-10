# Captcha

The captcha namespace handles the captcha challenges that block downloads until a human answers them. Use it to list the pending challenges, fetch the image, submit an answer, or skip the challenge when the answer is not available.

**Coverage:** 5/5 actions of `/captcha` (complete).

Reach it as `device.Captcha`, which returns a `CaptchaApi`. The C# namespace is `MyJdownloader.Captcha`, and the device API path is `/captcha`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

The captcha text passed to `SolveAsync` is sent to the device as is and is never retained or logged by the facade. Treat the text and images returned by the device as untrusted.

## CaptchaApi

`public sealed class CaptchaApi`

Typed facade over the `/captcha` namespace of one device (pending captcha challenges).

| Member | Signature | Description |
|---|---|---|
| `ListAsync` | `Task<IReadOnlyList<CaptchaJob>> ListAsync(CancellationToken cancellationToken = default)` | Lists the pending captcha jobs (`/captcha/list`). Empty when the device returns no data. |
| `GetJobAsync` | `Task<CaptchaJob> GetJobAsync(long id, CancellationToken cancellationToken = default)` | Returns one pending captcha job (`/captcha/getCaptchaJob`). When the captcha no longer exists, the device usually answers `404 NOT_AVAILABLE`, which surfaces as a `MyJdApiException`. |
| `GetImageAsync` | `Task<string> GetImageAsync(long id, string? format = null, CancellationToken cancellationToken = default)` | Returns the image of a captcha job (`/captcha/get`) as a Base64 data URL (`data:image/...;base64,...`), not decoded. `format` is the requested image format, or `null` to let the device choose; it is not sent when `null`. A blank `format`, one with control characters or lone surrogates, or one longer than its allowed length (see [Argument validation](../guides/error-handling.md#argument-validation)), throws `ArgumentException`. When the captcha no longer exists, the device usually answers `404 NOT_AVAILABLE`, which surfaces as a `MyJdApiException`. |
| `SolveAsync` | `Task<bool> SolveAsync(long id, string result, string? resultFormat = null, CancellationToken cancellationToken = default)` | Submits the answer to a captcha job (`/captcha/solve`). `result` is sent unchanged. Returns `true` when the device accepted the answer. `resultFormat` is the format of `result`, or `null` for the device default. |
| `SkipAsync` | `Task<bool> SkipAsync(long id, CaptchaSkipRequest type, CancellationToken cancellationToken = default)` | Skips a captcha job (`/captcha/skip`) with the scope given by `type`. `CaptchaSkipRequest.Unknown` is rejected. Returns `true` when the device applied the request. |

Remarks:

- `GetImageAsync` returns text supplied by the device, so treat it as untrusted: check the `data:image/` prefix and decode the Base64 before rendering it, and HTML-encode it when embedding it in a page.
- `SolveAsync` and `SkipAsync` act on the device immediately. A timeout means the action may have been executed.

### Example

```csharp
using MyJdownloader.Captcha;

var jobs = await device.Captcha.ListAsync();
if (jobs.Count > 0)
{
    // Image is a Base64 data URL: validate the prefix before decoding or rendering it.
    string image = await device.Captcha.GetImageAsync(jobs[0].Id);

    bool solved = await device.Captcha.SolveAsync(jobs[0].Id, "answer");

    // Or skip this single challenge instead of answering it.
    // bool skipped = await device.Captcha.SkipAsync(jobs[0].Id, CaptchaSkipRequest.Single);
}
```

## CaptchaJob

`public sealed record CaptchaJob`

A captcha challenge pending on the device. Fields that the device does not return are `null`.

`Hoster`, `Explain`, `Type` and `CaptchaCategory` are raw strings supplied by the device: encode them before displaying them.

| Property | Type | Description |
|---|---|---|
| `Id` | `long` | Captcha job identifier, used by the other `CaptchaApi` methods. |
| `LinkId` | `long` | Identifier of the link the captcha belongs to (JSON `link`). |
| `Hoster` | `string?` | Hoster that requires the captcha. |
| `Type` | `string?` | Challenge type. Raw device value; its set of values is not documented. |
| `CaptchaCategory` | `string?` | Captcha category. |
| `Explain` | `string?` | Explanation shown to the user. |
| `Created` | `DateTimeOffset?` | Creation date, or `null` when not set. |
| `Timeout` | `int?` | Timeout as reported by the device. Raw value; its unit is not documented. |

## CaptchaSkipRequest

`public enum CaptchaSkipRequest`

Scope of a captcha skip request, as used by the JDownloader API. Unrecognized values are read as `Unknown`, and `Unknown` cannot be sent.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The value is missing or not recognized by this library. |
| `Single` | `1` | Skips this captcha only (`SINGLE`). |
| `BlockHoster` | `2` | Skips all captchas of the hoster (`BLOCK_HOSTER`). |
| `BlockAllCaptchas` | `3` | Skips all captchas (`BLOCK_ALL_CAPTCHAS`). |
| `BlockPackage` | `4` | Skips all captchas of the package (`BLOCK_PACKAGE`). |
| `Refresh` | `5` | Requests a new captcha (`REFRESH`). |
| `StopCurrentAction` | `6` | Stops the current action (`STOP_CURRENT_ACTION`). |
| `Timeout` | `7` | Skips the captcha as timed out (`TIMEOUT`). |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
