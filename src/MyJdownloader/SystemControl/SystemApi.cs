namespace MyJdownloader.SystemControl;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/system</c> namespace of one device (restart or exit JDownloader, shut down, standby or hibernate the operating system).</summary>
/// <remarks>
/// These actions are destructive. The device usually goes offline before answering, so a <see cref="TimeoutException"/>,
/// an <see cref="HttpRequestException"/> or a <see cref="MyJdDeviceOfflineException"/> does not mean the action failed.
/// The library asks for no confirmation. System and storage information (<c>getSystemInfos</c>, <c>getStorageInfos</c>) is not typed yet:
/// use <see cref="IDeviceChannel.CallAsync(string, IReadOnlyList{JsonElement}, CancellationToken)"/> through <see cref="JDownloaderDevice.Channel"/>.
/// </remarks>
public sealed class SystemApi
{
    private const string Base = "/system/";

    private readonly IDeviceChannel _channel;

    internal SystemApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Restarts JDownloader on the device (<c>/system/restartJD</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RestartAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "restartJD", [], cancellationToken);

    /// <summary>Exits JDownloader on the device (<c>/system/exitJD</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ExitAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "exitJD", [], cancellationToken);

    /// <summary>Shuts down the operating system of the device (<c>/system/shutdownOS</c>).</summary>
    /// <param name="force">true to force the shutdown even if applications block it</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ShutdownOsAsync(bool force, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "shutdownOS", [JsonSerializer.SerializeToElement(force, FacadeJsonContext.Default.Boolean)], cancellationToken);

    /// <summary>Puts the operating system of the device on standby (<c>/system/standbyOS</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task StandbyOsAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "standbyOS", [], cancellationToken);

    /// <summary>Hibernates the operating system of the device (<c>/system/hibernateOS</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task HibernateOsAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "hibernateOS", [], cancellationToken);
}
