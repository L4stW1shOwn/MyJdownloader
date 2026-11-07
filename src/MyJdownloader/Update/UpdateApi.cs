namespace MyJdownloader.Update;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/update</c> namespace of one device (check for and install JDownloader updates).</summary>
/// <remarks><see cref="RestartAndUpdateAsync"/> is destructive: it restarts JDownloader without confirmation.</remarks>
public sealed class UpdateApi
{
    private const string Base = "/update/";

    private readonly IDeviceChannel _channel;

    internal UpdateApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Tells whether an update is available (<c>/update/isUpdateAvailable</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when an update is available on the device.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsUpdateAvailableAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "isUpdateAvailable", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Starts an update check on the device (<c>/update/runUpdateCheck</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The check runs on the device and may return before it finishes: call <see cref="IsUpdateAvailableAsync"/> afterwards to read the outcome.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task RunUpdateCheckAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "runUpdateCheck", [], cancellationToken);

    /// <summary>Restarts JDownloader on the device to install pending updates (<c>/update/restartAndUpdate</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// JDownloader restarts immediately and without confirmation. The device usually goes offline before answering, so a
    /// <see cref="TimeoutException"/>, an <see cref="HttpRequestException"/> or a <see cref="MyJdDeviceOfflineException"/> does not mean the action failed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RestartAndUpdateAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "restartAndUpdate", [], cancellationToken);
}
