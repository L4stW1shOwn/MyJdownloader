namespace MyJdownloader.SystemControl;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/system</c> namespace of one device (system and storage information; restart or exit JDownloader, shut down, standby or hibernate the operating system).</summary>
/// <remarks>
/// The action methods (restart, exit, shut down, standby, hibernate) are destructive. The device usually goes offline before answering, so a <see cref="TimeoutException"/>,
/// an <see cref="HttpRequestException"/> or a <see cref="MyJdDeviceOfflineException"/> does not mean the action failed.
/// The library asks for no confirmation. The information methods are read-only; their content comes from the device and is untrusted.
/// </remarks>
public sealed class SystemApi
{
    private const string Base = "/system/";

    private readonly IDeviceChannel _channel;

    internal SystemApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Gets the system and JVM information of the device (<c>/system/getSystemInfos</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The system information.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(_channel.CallAsync(Base + "getSystemInfos", SystemJsonContext.Default.SystemInfo, [], cancellationToken));

    /// <summary>Gets the storage information of the device (<c>/system/getStorageInfos</c>).</summary>
    /// <param name="path">The path to look up; <see langword="null"/> lists every storage root of the device.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>One entry per storage root; an empty list when the device returns none.</returns>
    /// <remarks>The content comes from the device and is untrusted. A storage root that failed on the device carries its message in <see cref="StorageInfo.Error"/>.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> is blank, too long or contains a control character.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<StorageInfo>> GetStorageInfosAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateOptionalText(path, nameof(path), FacadeGuard.MaxPathLength);
        var arg = JsonSerializer.SerializeToElement(path, FacadeJsonContext.Default.String);
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "getStorageInfos", SystemJsonContext.Default.ListStorageInfo, [arg], cancellationToken));
    }

    /// <summary>Restarts JDownloader on the device (<c>/system/restartJD</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RestartAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "restartJD", [], cancellationToken);

    /// <summary>Exits JDownloader on the device (<c>/system/exitJD</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ExitAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "exitJD", [], cancellationToken);

    /// <summary>Shuts down the operating system of the device (<c>/system/shutdownOS</c>).</summary>
    /// <param name="force"><see langword="true"/> to force the shutdown even if applications block it; unsaved data may be lost.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ShutdownOsAsync(bool force, CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "shutdownOS", [JsonSerializer.SerializeToElement(force, FacadeJsonContext.Default.Boolean)], cancellationToken);

    /// <summary>Puts the operating system of the device on standby (<c>/system/standbyOS</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task StandbyOsAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "standbyOS", [], cancellationToken);

    /// <summary>Hibernates the operating system of the device (<c>/system/hibernateOS</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid; a <see cref="MyJdDeviceOfflineException"/> may mean the action was executed.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task HibernateOsAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "hibernateOS", [], cancellationToken);
}
