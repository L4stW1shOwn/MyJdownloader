namespace MyJdownloader.Captcha;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/captcha</c> namespace of one device (pending captcha challenges).</summary>
/// <remarks>
/// The captcha text passed to <see cref="SolveAsync"/> is sent to the device as is and never retained or logged by the facade.
/// </remarks>
public sealed class CaptchaApi
{
    private const string Base = "/captcha/";

    private readonly IDeviceChannel _channel;

    internal CaptchaApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Lists the pending captcha jobs (<c>/captcha/list</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The jobs; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<CaptchaJob>> ListAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(_channel.CallAsync(Base + "list", CaptchaJsonContext.Default.ListCaptchaJob, [], cancellationToken));

    /// <summary>Gets a pending captcha job (<c>/captcha/getCaptchaJob</c>).</summary>
    /// <param name="id">The captcha job identifier (<see cref="CaptchaJob.Id"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The job.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the captcha id no longer exists the device usually answers 404 NOT_AVAILABLE, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<CaptchaJob> GetJobAsync(long id, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(_channel.CallAsync(Base + "getCaptchaJob", CaptchaJsonContext.Default.CaptchaJob, [Id(id)], cancellationToken));

    /// <summary>Gets the image of a captcha job (<c>/captcha/get</c>).</summary>
    /// <remarks>The returned text is supplied by the device and must be treated as untrusted: check the <c>data:image/</c> prefix and decode the Base64 before rendering, and HTML-encode it when embedding it in a page.</remarks>
    /// <param name="id">The captcha job identifier (<see cref="CaptchaJob.Id"/>).</param>
    /// <param name="format">The requested image format, or <see langword="null"/> to let the device choose; not sent when <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The image as a Base64 data URL (<c>data:image/...;base64,...</c>), as sent by the device and not decoded.</returns>
    /// <exception cref="ArgumentException"><paramref name="format"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the captcha id no longer exists the device usually answers 404 NOT_AVAILABLE, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<string> GetImageAsync(long id, string? format = null, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateOptionalText(format, nameof(format), FacadeGuard.MaxIdentifierLength);
        JsonElement[] args = format is null ? [Id(id)] : [Id(id), Str(format)];
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "get", FacadeJsonContext.Default.String, args, cancellationToken));
    }

    /// <summary>Skips a captcha job (<c>/captcha/skip</c>).</summary>
    /// <param name="id">The captcha job identifier (<see cref="CaptchaJob.Id"/>).</param>
    /// <param name="type">The scope of the skip; <see cref="CaptchaSkipRequest.Unknown"/> is rejected.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the request.</returns>
    /// <exception cref="ArgumentException"><paramref name="type"/> is not a known value.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the captcha id no longer exists the device usually answers 404 NOT_AVAILABLE, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SkipAsync(long id, CaptchaSkipRequest type, CancellationToken cancellationToken = default)
    {
        if (!FacadeGuard.IsKnown(type))
        {
            throw new ArgumentException("The skip type must be a known value.", nameof(type));
        }

        var args = new[] { Id(id), JsonSerializer.SerializeToElement(type, CaptchaJsonContext.Default.CaptchaSkipRequest) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "skip", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Solves a captcha job (<c>/captcha/solve</c>).</summary>
    /// <param name="id">The captcha job identifier (<see cref="CaptchaJob.Id"/>).</param>
    /// <param name="result">The captcha answer; sent unchanged and never retained or logged.</param>
    /// <param name="resultFormat">The format of <paramref name="result"/>, or <see langword="null"/> for the device default; not sent when <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the answer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="result"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates, or <paramref name="resultFormat"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the captcha id no longer exists the device usually answers 404 NOT_AVAILABLE, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SolveAsync(long id, string result, string? resultFormat = null, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(result, nameof(result), FacadeGuard.MaxLongTextLength);
        FacadeGuard.ValidateOptionalText(resultFormat, nameof(resultFormat), FacadeGuard.MaxIdentifierLength);
        JsonElement[] args = resultFormat is null ? [Id(id), Str(result)] : [Id(id), Str(result), Str(resultFormat)];
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "solve", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    private static JsonElement Id(long value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.Int64);

    private static JsonElement Str(string value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.String);
}
