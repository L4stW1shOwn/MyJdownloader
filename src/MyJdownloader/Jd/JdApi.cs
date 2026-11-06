namespace MyJdownloader.Jd;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/jd</c> namespace of one device (uptime, version, core revision and plugin refresh).</summary>
/// <remarks>Restarting the device is <c>/system/restartJD</c>, which is not part of this facade.</remarks>
public sealed class JdApi
{
    private const string Base = "/jd/";

    private const long MaxUptimeMilliseconds = long.MaxValue / TimeSpan.TicksPerMillisecond;

    private readonly IDeviceChannel _channel;

    internal JdApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Gets the time elapsed since the device started (<c>/jd/uptime</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The uptime of the device.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<TimeSpan> GetUptimeAsync(CancellationToken cancellationToken = default)
        => ToUptimeAsync(FacadeResults.RequireAsync(this._channel.CallAsync(Base + "uptime", FacadeJsonContext.Default.NullableInt64, [], cancellationToken)));

    /// <summary>Gets the version reported by the device (<c>/jd/version</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The raw value reported by the device; its meaning is not documented by the API.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<long> GetVersionAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "version", FacadeJsonContext.Default.NullableInt64, [], cancellationToken));

    /// <summary>Gets the core revision of the device (<c>/jd/getCoreRevision</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The core revision number.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<int> GetCoreRevisionAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getCoreRevision", FacadeJsonContext.Default.NullableInt32, [], cancellationToken));

    /// <summary>Asks the device to refresh its plugins (<c>/jd/refreshPlugins</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the request.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> RefreshPluginsAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "refreshPlugins", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    private static async Task<TimeSpan> ToUptimeAsync(Task<long> call)
    {
        var milliseconds = await call.ConfigureAwait(false);
        if (milliseconds is < 0 or > MaxUptimeMilliseconds)
        {
            throw FacadeResults.Unreadable();
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
