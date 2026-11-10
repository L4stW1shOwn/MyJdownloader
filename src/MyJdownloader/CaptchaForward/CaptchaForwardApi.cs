namespace MyJdownloader.CaptchaForward;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/captchaforward</c> namespace of one device (reCAPTCHA v2 challenges solved by the device).</summary>
/// <remarks>
/// <para>A job runs on the captcha pipeline of the device (dialog, solvers) and times out after 5 minutes. The device runs a limited number of jobs at once and queues the others; a full queue is answered with an error.</para>
/// <para>All content returned by the device is untrusted. The solved token is a credential for the target site: never log it.</para>
/// </remarks>
public sealed class CaptchaForwardApi
{
    private const string Base = "/captchaforward/";

    private readonly IDeviceChannel _channel;

    internal CaptchaForwardApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Queues a reCAPTCHA v2 challenge on the device (<c>/captchaforward/createJobRecaptchaV2</c>).</summary>
    /// <param name="siteKey">The reCAPTCHA site key of the target site; only ASCII letters, digits, <c>-</c> and <c>_</c> are accepted (the device inserts it into an HTML page).</param>
    /// <param name="domain">The host name of the target site (DNS name or IPv4 address, no scheme, path or port); an internationalized name must be sent as punycode. The device inserts it into an HTML page.</param>
    /// <param name="stoken">The optional <c>data-stoken</c> of the challenge; only ASCII letters, digits, <c>-</c> and <c>_</c> are accepted; <see langword="null"/> sends an empty string, which the device treats as no token.</param>
    /// <param name="reason">An optional reason for the job; <see langword="null"/> sends an empty string. Current JDownloader versions ignore it.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The identifier of the job, to pass to <see cref="GetResultAsync"/>.</returns>
    /// <remarks>
    /// <para>The challenge is solved on the device (dialog, solvers) within 5 minutes. There is no built-in polling loop: call <see cref="GetResultAsync"/> with a delay between calls (ADR-005, ADR-009).</para>
    /// <para>The call is not idempotent: a replay queues another job.</para>
    /// <para>Keep at most one pending job per user action and never create jobs in a loop: each job prompts the device user or a paid solver, and a full queue is rejected with <see cref="MyJdApiException"/>.</para>
    /// <para>The solved token must never go into logs, telemetry, exception messages or URLs.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="siteKey"/> or <paramref name="domain"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A text is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates, <paramref name="siteKey"/> or <paramref name="stoken"/> contains a character other than an ASCII letter, digit, <c>-</c> or <c>_</c>, or <paramref name="domain"/> is not a DNS host name or IPv4 address (<paramref name="stoken"/> and <paramref name="reason"/> are checked only when not <see langword="null"/>).</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request (for example when the queue is full), or the response is invalid (including a missing job identifier).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled; the job may have been queued.</exception>
    /// <exception cref="TimeoutException">The request timed out; the job may have been queued.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the job may have been queued.</exception>
    public Task<long> CreateJobRecaptchaV2Async(string siteKey, string domain, string? stoken = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(siteKey);
        ArgumentNullException.ThrowIfNull(domain);
        FacadeGuard.ValidateUrlSafeToken(siteKey, nameof(siteKey), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateHostName(domain, nameof(domain));
        if (stoken is not null)
        {
            FacadeGuard.ValidateUrlSafeToken(stoken, nameof(stoken), FacadeGuard.MaxLongTextLength);
        }

        FacadeGuard.ValidateOptionalText(reason, nameof(reason), FacadeGuard.MaxLongTextLength);
        var args = new[] { Text(siteKey), Text(stoken ?? string.Empty), Text(domain), Text(reason ?? string.Empty) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "createJobRecaptchaV2", FacadeJsonContext.Default.NullableInt64, args, cancellationToken));
    }

    /// <summary>Reads the token of a finished job (<c>/captchaforward/getResult</c>).</summary>
    /// <param name="jobId">The identifier returned by <see cref="CreateJobRecaptchaV2Async"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The solved token, or <see langword="null"/> when the device sent none.</returns>
    /// <remarks>
    /// <para>While the job is not finished the device answers 404 (<see cref="MyJdApiException"/>); it answers 404 too for an unknown or already read job. A successful read removes the job: the token can be read only once. A failed job is answered with 500. Poll with a delay; there is no built-in loop (ADR-005, ADR-009).</para>
    /// <para>The token is a credential for the target site: never log it. It comes from the device and is untrusted.</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request (404 while the job is not finished, unknown or already read; 500 when the job failed), or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled; the token may have been consumed.</exception>
    /// <exception cref="TimeoutException">The request timed out; the token may have been consumed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the token may have been consumed.</exception>
    public Task<string?> GetResultAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var args = new[] { JsonSerializer.SerializeToElement(jobId, FacadeJsonContext.Default.Int64) };
        return this._channel.CallAsync(Base + "getResult", FacadeJsonContext.Default.String, args, cancellationToken);
    }

    private static JsonElement Text(string value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.String);
}
