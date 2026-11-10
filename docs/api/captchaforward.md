# CaptchaForward

The captchaforward namespace asks the device to solve a reCAPTCHA v2 challenge of a website. You create a job with the site key and the domain, then read the token of the job once it is solved.

**Coverage:** 2/2 actions of `/captchaforward` (complete).

Reach it as `device.CaptchaForward`, which returns a `CaptchaForwardApi`. The C# namespace is `MyJdownloader.CaptchaForward`, and the device API path is `/captchaforward`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning: consent and credentials.** `CreateJobRecaptchaV2Async` puts a challenge on the captcha pipeline of the device: a person at the device is prompted, or a solving service answers it. Only queue a job for a site and for a user who agreed to it, and never in unattended jobs.
>
> The token returned by `GetResultAsync` is a credential for the target site. Never log it, keep it in memory only as long as you need it, and do not store it in plain text.
>
> A `TimeoutException`, `OperationCanceledException` or `HttpRequestException` on `CreateJobRecaptchaV2Async` means the job may have been queued. Do not create another job blindly: a replay queues a second challenge.

## CaptchaForwardApi

`public sealed class CaptchaForwardApi`

Typed facade over the `/captchaforward` namespace of one device (reCAPTCHA v2 challenges solved by the device). All content returned by the device is untrusted.

| Member | Signature | Description |
|---|---|---|
| `CreateJobRecaptchaV2Async` | `Task<long> CreateJobRecaptchaV2Async(string siteKey, string domain, string? stoken = null, string? reason = null, CancellationToken cancellationToken = default)` | Queues a reCAPTCHA v2 challenge (`/captchaforward/createJobRecaptchaV2`) and returns the identifier of the job. `siteKey` and `domain` are required. `stoken` and `reason` are sent as an empty string when `null`. Throws `ArgumentNullException` for a `null` `siteKey` or `domain`, and `ArgumentException` for a blank text or a text with control characters or lone surrogates; `stoken` and `reason` are checked only when not `null`. These checks run before any request. A full queue on the device is rejected as a `MyJdException`. A response without a job identifier throws `MyJdProtocolException`. |
| `GetResultAsync` | `Task<string?> GetResultAsync(long jobId, CancellationToken cancellationToken = default)` | Reads the token of a job (`/captchaforward/getResult`). Returns the token, or `null` when the device sent none. A successful read removes the job. See the remarks for the status codes. |

### CreateJobRecaptchaV2Async

Remarks:

- The device solves the challenge within 5 minutes. The call does not wait for the solution: poll `GetResultAsync` with a delay between the calls. There is no built-in loop (see [ADR-005](../adr/ADR-005-error-handling.md) and [ADR-009](../adr/ADR-009-events-long-polling-without-built-in-loop.md)).
- The call is not idempotent: a replay queues another job.
- `reason` is ignored by current JDownloader versions. It is accepted for the device protocol only.

### GetResultAsync

Remarks:

- While the job is not finished, the device answers `404`, which surfaces as a `MyJdApiException` with `StatusCode` set to `HttpStatusCode.NotFound`. The device answers `404` too for an unknown job and for a job whose token was already read, so a `404` does not tell these cases apart.
- A job that failed is answered with `500`, which surfaces as a `MyJdApiException` with `StatusCode` set to `HttpStatusCode.InternalServerError`. Do not poll again in a loop after a `500`: the job will not succeed.
- A successful read removes the job. The token can be read only once: store it on the first successful read.
- The token comes from the device and is untrusted. Do not log it, and do not render it in a page.

### Example

Create a job, then poll for the token with a delay. The loop gives up after about 6 minutes, longer than the 5-minute device timeout.

```csharp
using System.Net;
using MyJdownloader;
using MyJdownloader.CaptchaForward;

// Ask the user first: a person at the device, or a solving service, answers the challenge.
long jobId = await device.CaptchaForward.CreateJobRecaptchaV2Async(
    siteKey,
    "example.com",
    cancellationToken: cancellationToken);

string? token = null;
bool solved = false;
for (int attempt = 0; attempt < 72 && !solved; attempt++)
{
    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    try
    {
        // A successful read removes the job: keep the token from this point on.
        token = await device.CaptchaForward.GetResultAsync(jobId, cancellationToken);
        solved = true;
    }
    catch (MyJdApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // Not solved yet (or unknown, or already read): wait, then poll again.
    }
}

if (token is not null)
{
    // Use the token once, for the target site. Never log it.
}
```

`siteKey` and `cancellationToken` stand for your own values. A `500` from `GetResultAsync` is not caught here: it means the job failed, and it propagates.

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [Device administration](../guides/device-administration.md)
