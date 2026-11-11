namespace MyJdownloader.Downloads;

using System.Text.Json;
using MyJdownloader.Facades;

public sealed partial class DownloadsApi
{
    /// <summary>Moves links after a given link, into a destination package (<c>/downloadsV2/moveLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to move; must contain at least one.</param>
    /// <param name="afterLinkId">The identifier of the link after which the links are placed.</param>
    /// <param name="destinationPackageId">The identifier of the destination package.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The meaning of <c>0</c> for <paramref name="afterLinkId"/> is to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="linkIds"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterLinkId"/> or <paramref name="destinationPackageId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MoveLinksAsync(IReadOnlyList<long> linkIds, long afterLinkId, long destinationPackageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(linkIds);
        RequireNotEmpty(linkIds, nameof(linkIds));
        ArgumentOutOfRangeException.ThrowIfNegative(afterLinkId);
        ArgumentOutOfRangeException.ThrowIfNegative(destinationPackageId);
        var args = new[]
        {
            FacadeGuard.Ids(linkIds, nameof(linkIds)),
            JsonSerializer.SerializeToElement(afterLinkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(destinationPackageId, FacadeJsonContext.Default.Int64),
        };
        return this._channel.CallAsync(Base + "moveLinks", args, cancellationToken);
    }

    /// <summary>Moves packages after a given package (<c>/downloadsV2/movePackages</c>).</summary>
    /// <param name="packageIds">The identifiers of the packages to move; must contain at least one.</param>
    /// <param name="afterPackageId">The identifier of the package after which the packages are placed.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The meaning of <c>0</c> for <paramref name="afterPackageId"/> is to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="packageIds"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterPackageId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MovePackagesAsync(IReadOnlyList<long> packageIds, long afterPackageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageIds);
        RequireNotEmpty(packageIds, nameof(packageIds));
        ArgumentOutOfRangeException.ThrowIfNegative(afterPackageId);
        var args = new[]
        {
            FacadeGuard.Ids(packageIds, nameof(packageIds)),
            JsonSerializer.SerializeToElement(afterPackageId, FacadeJsonContext.Default.Int64),
        };
        return this._channel.CallAsync(Base + "movePackages", args, cancellationToken);
    }

    /// <summary>Splits packages by hoster (<c>/downloadsV2/splitPackageByHoster</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to select.</param>
    /// <param name="packageIds">The identifiers of the packages to select.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SplitPackageByHosterAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "splitPackageByHoster", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Starts an online status check (<c>/downloadsV2/startOnlineStatusCheck</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to check.</param>
    /// <param name="packageIds">The identifiers of the packages to check.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task StartOnlineStatusCheckAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "startOnlineStatusCheck", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    private static void RequireNotEmpty(IReadOnlyList<long> ids, string paramName)
    {
        if (ids.Count == 0)
        {
            throw new ArgumentException("The list must contain at least one identifier.", paramName);
        }
    }
}
