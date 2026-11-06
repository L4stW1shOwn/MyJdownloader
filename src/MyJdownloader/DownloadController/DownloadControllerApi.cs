namespace MyJdownloader.DownloadController;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/downloadcontroller</c> namespace of one device (start, stop, pause and state of the downloads).</summary>
public sealed class DownloadControllerApi
{
    private const string Base = "/downloadcontroller/";

    private readonly IDeviceChannel _channel;

    internal DownloadControllerApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Starts the downloads (<c>/downloadcontroller/start</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the request.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "start", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Stops the downloads (<c>/downloadcontroller/stop</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the request.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "stop", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Pauses or resumes the downloads (<c>/downloadcontroller/pause</c>).</summary>
    /// <param name="paused"><see langword="true"/> to pause the downloads, <see langword="false"/> to resume them.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the request.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> PauseAsync(bool paused, CancellationToken cancellationToken = default)
    {
        var args = new[] { JsonSerializer.SerializeToElement(paused, FacadeJsonContext.Default.Boolean) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "pause", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Gets the state of the download controller (<c>/downloadcontroller/getCurrentState</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The current <see cref="DownloadState"/>.</returns>
    /// <remarks>A state not recognized by this library is read as <see cref="DownloadState.Unknown"/>.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<DownloadState> GetCurrentStateAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getCurrentState", DownloadControllerJsonContext.Default.NullableDownloadState, [], cancellationToken));

    /// <summary>Gets the current download speed (<c>/downloadcontroller/getSpeedInBps</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The current download speed in bytes per second.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getSpeedInBps", FacadeJsonContext.Default.NullableInt32, [], cancellationToken));
}
