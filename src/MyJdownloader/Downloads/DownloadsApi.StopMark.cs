namespace MyJdownloader.Downloads;

using System.Text.Json;
using MyJdownloader.Facades;

public sealed partial class DownloadsApi
{
    /// <summary>Gets the identifier of the stop-marked link (<c>/downloadsV2/getStopMark</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The identifier of the stop mark, or <see langword="null"/> when no stop mark is set (the device answers -1).</returns>
    /// <remarks>The exact semantics of the returned identifier are to be confirmed by live tests.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<long?> GetStopMarkAsync(CancellationToken cancellationToken = default)
        => MapStopMarkAsync(FacadeResults.RequireAsync(_channel.CallAsync(Base + "getStopMark", FacadeJsonContext.Default.NullableInt64, [], cancellationToken)));

    /// <summary>Gets the stop-marked link (<c>/downloadsV2/getStopMarkedLink</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The stop-marked link, or <see langword="null"/> when none is marked.</returns>
    /// <remarks>Links may embed credentials: do not log whole records.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<DownloadLink?> GetStopMarkedLinkAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "getStopMarkedLink", DownloadsJsonContext.Default.DownloadLink, [], cancellationToken);

    /// <summary>Sets the stop mark on a link (<c>/downloadsV2/setStopMark</c>).</summary>
    /// <param name="linkId">The identifier of the link.</param>
    /// <param name="packageId">The identifier of the package containing the link.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The exact semantics of the arguments are to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="linkId"/> or <paramref name="packageId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SetStopMarkAsync(long linkId, long packageId, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(linkId, nameof(linkId));
        FacadeGuard.ThrowIfNegative(packageId, nameof(packageId));
        var args = new[]
        {
            JsonSerializer.SerializeToElement(linkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(packageId, FacadeJsonContext.Default.Int64),
        };
        return _channel.CallAsync(Base + "setStopMark", args, cancellationToken);
    }

    /// <summary>Removes the stop mark (<c>/downloadsV2/removeStopMark</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RemoveStopMarkAsync(CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "removeStopMark", [], cancellationToken);

    /// <summary>Gets the structure change counter of the download list (<c>/downloadsV2/getStructureChangeCounter</c>).</summary>
    /// <param name="oldCounterValue">The counter value previously observed by the caller.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The current counter value.</returns>
    /// <remarks>The exact semantics of the counter are to be confirmed by live tests.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<long> GetStructureChangeCounterAsync(long oldCounterValue, CancellationToken cancellationToken = default)
    {
        var args = new[] { JsonSerializer.SerializeToElement(oldCounterValue, FacadeJsonContext.Default.Int64) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "getStructureChangeCounter", FacadeJsonContext.Default.NullableInt64, args, cancellationToken));
    }

    private static async Task<long?> MapStopMarkAsync(Task<long> answer)
    {
        var id = await answer.ConfigureAwait(false);
        return id == -1 ? null : id;
    }
}
