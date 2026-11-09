namespace MyJdownloader.Reconnect;

/// <summary>Typed facade over the <c>/reconnect</c> namespace of one device (internet connection reconnect).</summary>
public sealed class ReconnectApi
{
    private const string Base = "/reconnect/";

    private readonly IDeviceChannel _channel;

    internal ReconnectApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Asks the device to reconnect its internet connection (<c>/reconnect/doReconnect</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// The device requests the reconnect immediately (<c>DownloadWatchDog.requestReconnect</c>), without any confirmation. Running downloads are interrupted and the device may be briefly unreachable, so this call or the following ones may fail with <see cref="MyJdDeviceOfflineException"/>, a timeout or a network error even though the reconnect happened.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task DoReconnectAsync(CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "doReconnect", [], cancellationToken);
}
