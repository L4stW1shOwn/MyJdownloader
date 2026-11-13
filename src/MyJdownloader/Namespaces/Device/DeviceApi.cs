namespace MyJdownloader.Device;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/device</c> namespace of one device (liveness check).</summary>
/// <remarks><c>/device/getDirectConnectionInfos</c> and <c>/device/getSessionPublicKey</c> are not part of this facade; they remain reachable through <see cref="IDeviceChannel"/>.</remarks>
public sealed class DeviceApi
{
    private const string Base = "/device/";

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="DeviceApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public DeviceApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Checks that the device answers (<c>/device/ping</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device answered the ping.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> PingAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(_channel.CallAsync(Base + "ping", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));
}
