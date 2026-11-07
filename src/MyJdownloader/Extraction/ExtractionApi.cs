namespace MyJdownloader.Extraction;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/extraction</c> namespace of one device (archive extraction).</summary>
/// <remarks>
/// Text returned by the device is untrusted: encode it before displaying it.
/// Archive passwords are secrets: they are sent as call arguments and are never retained or logged by the facade; do not log them either.
/// </remarks>
public sealed class ExtractionApi
{
    private const string Base = "/extraction/";

    private readonly IDeviceChannel _channel;

    internal ExtractionApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Adds a password to the list of archive passwords of the device (<c>/extraction/addArchivePassword</c>).</summary>
    /// <param name="password">The archive password; sent unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// The password is added to the persisted password list of the device. A timeout or a network error does not mean the action failed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="password"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task AddArchivePasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(password, nameof(password));
        return this._channel.CallAsync(Base + "addArchivePassword", [Str(password)], cancellationToken);
    }

    /// <summary>Cancels a running extraction (<c>/extraction/cancelExtraction</c>).</summary>
    /// <param name="controllerId">The extraction controller identifier (<see cref="ArchiveStatus.ControllerId"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device cancelled the extraction.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="controllerId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> CancelExtractionAsync(long controllerId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(controllerId);
        var args = new[] { JsonSerializer.SerializeToElement(controllerId, FacadeJsonContext.Default.Int64) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "cancelExtraction", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Gets the archive status of links and packages (<c>/extraction/getArchiveInfo</c>).</summary>
    /// <param name="linkIds">The link identifiers; an empty list sends an empty array.</param>
    /// <param name="packageIds">The package identifiers; an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The archive statuses; empty when the device returns no data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<ArchiveStatus>> GetArchiveInfoAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        var args = FacadeGuard.IdArgs(linkIds, packageIds);
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "getArchiveInfo", ExtractionJsonContext.Default.ListArchiveStatus, args, cancellationToken));
    }

    /// <summary>Gets the extraction settings of archives (<c>/extraction/getArchiveSettings</c>).</summary>
    /// <param name="archiveIds">The archive identifiers (<see cref="ArchiveStatus.ArchiveId"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The archive settings; empty when the device returns no data.</returns>
    /// <remarks>The settings returned by the device contain passwords in clear: never log them.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="archiveIds"/> or one of its items is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <paramref name="archiveIds"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request (for example <c>BAD_PARAMETERS</c>), or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<ArchiveSettings>> GetArchiveSettingsAsync(IReadOnlyList<string> archiveIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveIds);
        foreach (var archiveId in archiveIds)
        {
            FacadeGuard.ValidateText(archiveId, nameof(archiveIds));
        }

        var args = new[] { JsonSerializer.SerializeToElement([.. archiveIds], ExtractionJsonContext.Default.StringArray) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "getArchiveSettings", ExtractionJsonContext.Default.ListArchiveSettings, args, cancellationToken));
    }

    /// <summary>Gets the extraction queue (<c>/extraction/getQueue</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The archives being extracted or queued; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<ArchiveStatus>> GetQueueAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "getQueue", ExtractionJsonContext.Default.ListArchiveStatus, [], cancellationToken));

    /// <summary>Changes the extraction settings of an archive (<c>/extraction/setArchiveSettings</c>).</summary>
    /// <param name="archiveId">The archive identifier (<see cref="ArchiveStatus.ArchiveId"/>).</param>
    /// <param name="settings">The settings to apply; members left <see langword="null"/> are not sent and keep the device default.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the change.</returns>
    /// <remarks>
    /// The change (extraction path, passwords, delete-after-extraction flags) is applied immediately, without confirmation.
    /// <see cref="ArchiveSettings.ExtractPath"/> is a path on the device: do not pass untrusted input. A timeout does not mean the action failed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="archiveId"/>, <paramref name="settings"/> or a password item is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A text value is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request (for example <c>BAD_PARAMETERS</c>), or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetArchiveSettingsAsync(string archiveId, ArchiveSettings settings, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(archiveId, nameof(archiveId));
        ArgumentNullException.ThrowIfNull(settings);
        FacadeGuard.ValidateOptionalText(settings.ExtractPath, nameof(settings));
        FacadeGuard.ValidateOptionalText(settings.FinalPassword, nameof(settings));
        if (settings.Passwords is { } passwords)
        {
            foreach (var password in passwords)
            {
                FacadeGuard.ValidateText(password, nameof(settings));
            }
        }

        var args = new[] { Str(archiveId), JsonSerializer.SerializeToElement(settings, ExtractionJsonContext.Default.ArchiveSettings) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "setArchiveSettings", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Starts the extraction of links and packages immediately (<c>/extraction/startExtractionNow</c>).</summary>
    /// <param name="linkIds">The link identifiers; an empty list sends an empty array.</param>
    /// <param name="packageIds">The package identifiers; an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The outcome per archive identifier (<see langword="null"/> when the device gives none); empty when the device returns no data.</returns>
    /// <remarks>The extraction starts immediately, without confirmation. A timeout does not mean the action failed.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<IReadOnlyDictionary<string, bool?>> StartExtractionNowAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        var args = FacadeGuard.IdArgs(linkIds, packageIds);
        return ToResultsAsync(this._channel.CallAsync(Base + "startExtractionNow", ExtractionJsonContext.Default.DictionaryStringNullableBoolean, args, cancellationToken));
    }

    private static async Task<IReadOnlyDictionary<string, bool?>> ToResultsAsync(Task<Dictionary<string, bool?>?> call)
        => await call.ConfigureAwait(false) ?? [];

    private static JsonElement Str(string value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.String);
}
