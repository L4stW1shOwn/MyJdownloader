namespace MyJdownloader.Toolbar;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/toolbar</c> namespace of one device (the actions of the browser-extension toolbar).</summary>
/// <remarks>
/// <para>The facade exposes 11 of the 15 actions. <c>addLinksFromDOM</c>, <c>checkLinksFromDOM</c> and <c>pollCheckedLinksFromDOM</c> are not exposed: they are a browser-extension DOM protocol that reads named HTTP parameters, which the positional channel cannot send. <c>specialURLHandling</c> is not exposed either: it returns JavaScript meant to be evaluated by a browser.</para>
/// <para>Several actions overlap with the <see cref="MyJdownloader.DownloadController.DownloadControllerApi"/> and <see cref="MyJdownloader.Update.UpdateApi"/> facades, which expose them with more detail. All content returned by the device is untrusted.</para>
/// </remarks>
public sealed class ToolbarApi
{
    private const string Base = "/toolbar/";

    private readonly IDeviceChannel _channel;

    internal ToolbarApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Reads the state of the toolbar (<c>/toolbar/getStatus</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The state of the toolbar. <see cref="ToolbarStatus.State"/> is a label of the device and is untrusted.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing status).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<ToolbarStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getStatus", ToolbarJsonContext.Default.ToolbarStatus, [], cancellationToken));

    /// <summary>Checks that the toolbar API answers (<c>/toolbar/isAvailable</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> on current JDownloader versions; the call is mostly a liveness probe.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "isAvailable", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Starts the downloads (<c>/toolbar/startDownloads</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> on current JDownloader versions.</returns>
    /// <remarks>See also <see cref="MyJdownloader.DownloadController.DownloadControllerApi"/>.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> StartDownloadsAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "startDownloads", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Stops the downloads (<c>/toolbar/stopDownloads</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> on current JDownloader versions.</returns>
    /// <remarks>See also <see cref="MyJdownloader.DownloadController.DownloadControllerApi"/>.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> StopDownloadsAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "stopDownloads", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Starts an update check in the background, without a dialog (<c>/toolbar/triggerUpdate</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> on current JDownloader versions, as soon as the check is started.</returns>
    /// <remarks>The call does not wait for the check. See also <see cref="MyJdownloader.Update.UpdateApi"/>.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> TriggerUpdateAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "triggerUpdate", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the download speed limit on the device (<c>/toolbar/toggleDownloadSpeedLimit</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> ToggleDownloadSpeedLimitAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "toggleDownloadSpeedLimit", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the pause of the downloads on the device (<c>/toolbar/togglePauseDownloads</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> TogglePauseDownloadsAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "togglePauseDownloads", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the use of premium accounts on the device (<c>/toolbar/togglePremium</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> TogglePremiumAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "togglePremium", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the clipboard monitoring on the device (<c>/toolbar/toggleClipboardMonitoring</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> ToggleClipboardMonitoringAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "toggleClipboardMonitoring", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the automatic reconnect on the device (<c>/toolbar/toggleAutomaticReconnect</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> ToggleAutomaticReconnectAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "toggleAutomaticReconnect", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Flips the stop after the current download on the device (<c>/toolbar/toggleStopAfterCurrentDownload</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new state: <see langword="true"/> when the setting is now on.</returns>
    /// <remarks>The call is not idempotent: a replay after a timeout or a network error flips the setting back. Re-read the state with <see cref="GetStatusAsync"/> before retrying.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> ToggleStopAfterCurrentDownloadAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "toggleStopAfterCurrentDownload", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));
}
