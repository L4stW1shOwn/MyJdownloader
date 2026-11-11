namespace MyJdownloader.Device;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/device</c> namespace of one device (liveness check).</summary>
/// <remarks><c>/device/getDirectConnectionInfos</c> and <c>/device/getSessionPublicKey</c> are not part of this facade; they remain reachable through <see cref="IDeviceChannel"/>.</remarks>
public sealed class DeviceApi
{
    private const string Base = "/device/";

    private readonly IDeviceChannel _channel;

    internal DeviceApi(IDeviceChannel channel)
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
