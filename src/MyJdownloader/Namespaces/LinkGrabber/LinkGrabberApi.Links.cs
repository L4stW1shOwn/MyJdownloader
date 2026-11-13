namespace MyJdownloader.LinkGrabber;

using System.Text.Json;
using MyJdownloader.Facades;

public sealed partial class LinkGrabberApi
{
    /// <summary>Cleans up links and packages of the link grabber list (<c>/linkgrabberv2/cleanup</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to clean up.</param>
    /// <param name="packageIds">The identifiers of the packages to clean up.</param>
    /// <param name="action">What to clean up; <see cref="CleanupAction.Unknown"/> is rejected.</param>
    /// <param name="mode">What to do with the links and files; <see cref="CleanupMode.Unknown"/> is rejected.</param>
    /// <param name="selectionType">Which of the given links and packages are targeted; <see cref="SelectionType.Unknown"/> is rejected.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// Applied without confirmation. Destructive: with <see cref="CleanupMode.RemoveLinksAndDeleteFiles"/> the files are deleted on the device, with <see cref="CleanupMode.RemoveLinksAndRecycleFiles"/> they are moved to the recycle bin.
    /// The library adds no confirmation step: callers are responsible for confirming with the user.
    /// With <see cref="SelectionType.All"/> the identifiers are ignored, and with <see cref="SelectionType.Unselected"/> every item except the given ones is targeted: combined with <see cref="CleanupMode.RemoveLinksAndDeleteFiles"/> this can delete every downloaded file on the device.
    /// Targeting every item requires <see cref="SelectionType.All"/>: <see cref="SelectionType.Unselected"/> with both identifier lists empty is rejected.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="action"/>, <paramref name="mode"/> or <paramref name="selectionType"/> is <c>Unknown</c> or not a defined value, or <paramref name="selectionType"/> is <see cref="SelectionType.Unselected"/> with both identifier lists empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task CleanupAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CleanupAction action, CleanupMode mode, SelectionType selectionType, CancellationToken cancellationToken = default)
    {
        if (!FacadeGuard.IsKnown(action))
        {
            throw new ArgumentException("The cleanup action must be a known value.", nameof(action));
        }

        if (!FacadeGuard.IsKnown(mode))
        {
            throw new ArgumentException("The cleanup mode must be a known value.", nameof(mode));
        }

        if (!FacadeGuard.IsKnown(selectionType))
        {
            throw new ArgumentException("The selection type must be a known value.", nameof(selectionType));
        }

        var ids = FacadeGuard.IdArgs(linkIds, packageIds);

        if (selectionType == SelectionType.Unselected && ids[0].GetArrayLength() == 0 && ids[1].GetArrayLength() == 0)
        {
            throw new ArgumentException("Unselected with no identifier targets every item; use SelectionType.All explicitly.", nameof(selectionType));
        }

        var args = new[]
        {
            ids[0],
            ids[1],
            JsonSerializer.SerializeToElement(action, LinkGrabberJsonContext.Default.CleanupAction),
            JsonSerializer.SerializeToElement(mode, LinkGrabberJsonContext.Default.CleanupMode),
            JsonSerializer.SerializeToElement(selectionType, LinkGrabberJsonContext.Default.SelectionType),
        };
        return _channel.CallAsync(Base + "cleanup", args, cancellationToken);
    }

    /// <summary>Moves links after a given link, into a destination package (<c>/linkgrabberv2/moveLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to move; must contain at least one.</param>
    /// <param name="afterLinkId">The identifier of the link after which the links are placed.</param>
    /// <param name="destinationPackageId">The identifier of the destination package.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The meaning of <c>0</c> for <paramref name="afterLinkId"/> is to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="linkIds"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterLinkId"/> or <paramref name="destinationPackageId"/> is negative, or an identifier in <paramref name="linkIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MoveLinksAsync(IReadOnlyList<long> linkIds, long afterLinkId, long destinationPackageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(linkIds);
        FacadeGuard.RequireNotEmpty(linkIds, nameof(linkIds));
        FacadeGuard.ThrowIfNegative(afterLinkId, nameof(afterLinkId));
        FacadeGuard.ThrowIfNegative(destinationPackageId, nameof(destinationPackageId));
        var args = new[]
        {
            FacadeGuard.Ids(linkIds, nameof(linkIds)),
            JsonSerializer.SerializeToElement(afterLinkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(destinationPackageId, FacadeJsonContext.Default.Int64),
        };
        return _channel.CallAsync(Base + "moveLinks", args, cancellationToken);
    }

    /// <summary>Moves packages after a given package (<c>/linkgrabberv2/movePackages</c>).</summary>
    /// <param name="packageIds">The identifiers of the packages to move; must contain at least one.</param>
    /// <param name="afterPackageId">The identifier of the package after which the packages are placed.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The meaning of <c>0</c> for <paramref name="afterPackageId"/> is to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="packageIds"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterPackageId"/> is negative, or an identifier in <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MovePackagesAsync(IReadOnlyList<long> packageIds, long afterPackageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageIds);
        FacadeGuard.RequireNotEmpty(packageIds, nameof(packageIds));
        FacadeGuard.ThrowIfNegative(afterPackageId, nameof(afterPackageId));
        var args = new[]
        {
            FacadeGuard.Ids(packageIds, nameof(packageIds)),
            JsonSerializer.SerializeToElement(afterPackageId, FacadeJsonContext.Default.Int64),
        };
        return _channel.CallAsync(Base + "movePackages", args, cancellationToken);
    }

    /// <summary>Moves links and packages to a new package (<c>/linkgrabberv2/movetoNewPackage</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to move.</param>
    /// <param name="packageIds">The identifiers of the packages to move.</param>
    /// <param name="newPackageName">The name of the new package.</param>
    /// <param name="downloadPath">The download directory of the new package, a path on the device.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The package name and the download path are not filtered: callers handling untrusted input must allow-list them.</remarks>
    /// <exception cref="ArgumentNullException">An identifier list, <paramref name="newPackageName"/> or <paramref name="downloadPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newPackageName"/> or <paramref name="downloadPath"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MoveToNewPackageAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, string newPackageName, string downloadPath, CancellationToken cancellationToken = default)
    {
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        FacadeGuard.ValidateText(newPackageName, nameof(newPackageName), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateText(downloadPath, nameof(downloadPath), FacadeGuard.MaxPathLength);
        var args = new[]
        {
            ids[0],
            ids[1],
            JsonSerializer.SerializeToElement(newPackageName, FacadeJsonContext.Default.String),
            JsonSerializer.SerializeToElement(downloadPath, FacadeJsonContext.Default.String),
        };
        return _channel.CallAsync(Base + "movetoNewPackage", args, cancellationToken);
    }

    /// <summary>Splits packages by hoster (<c>/linkgrabberv2/splitPackageByHoster</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to select.</param>
    /// <param name="packageIds">The identifiers of the packages to select.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SplitPackageByHosterAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "splitPackageByHoster", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Sets the download directory of packages (<c>/linkgrabberv2/setDownloadDirectory</c>).</summary>
    /// <param name="directory">The destination directory, a path on the device.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. The path is a path on the device and is not filtered: callers handling untrusted input must allow-list it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SetDownloadDirectoryAsync(string directory, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(directory, nameof(directory), FacadeGuard.MaxPathLength);
        var args = new[]
        {
            JsonSerializer.SerializeToElement(directory, FacadeJsonContext.Default.String),
            FacadeGuard.Ids(packageIds, nameof(packageIds)),
        };
        return _channel.CallAsync(Base + "setDownloadDirectory", args, cancellationToken);
    }

    /// <summary>Starts an online status check (<c>/linkgrabberv2/startOnlineStatusCheck</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to check.</param>
    /// <param name="packageIds">The identifiers of the packages to check.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task StartOnlineStatusCheckAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => _channel.CallAsync(Base + "startOnlineStatusCheck", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Sets or clears the download password of links and packages (<c>/linkgrabberv2/setDownloadPassword</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to change.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="password">The new password, or <see langword="null"/> to clear it. A blank password is rejected, so only <see langword="null"/> clears.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the change.</returns>
    /// <remarks>
    /// Applied without confirmation. The effect of <see langword="null"/> (clear) is to be confirmed by live tests.
    /// The password is a secret: it is sent as a call argument, never retained or logged by the library; do not log it either.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="password"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetDownloadPasswordAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, string? password, CancellationToken cancellationToken = default)
    {
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        if (password is not null)
        {
            FacadeGuard.ValidateText(password, nameof(password), FacadeGuard.MaxLongTextLength);
        }

        var args = new[] { ids[0], ids[1], JsonSerializer.SerializeToElement(password, FacadeJsonContext.Default.String) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "setDownloadPassword", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Gets the download URLs of links and packages (<c>/linkgrabberv2/getDownloadUrls</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to select.</param>
    /// <param name="packageIds">The identifiers of the packages to select.</param>
    /// <param name="urlDisplayTypes">The kinds of URL to return; must contain at least one, without duplicates.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The link identifiers by URL; empty when the device answers nothing.</returns>
    /// <remarks>
    /// The keys are untrusted device URLs that may embed credentials: never log them.
    /// The accepted <see cref="UrlDisplayType"/> values are to be confirmed by live tests.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/>, <paramref name="packageIds"/> or <paramref name="urlDisplayTypes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="urlDisplayTypes"/> is empty, contains a duplicate, <see cref="UrlDisplayType.Unknown"/> or an undefined value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyDictionary<string, IReadOnlyList<long>>> GetDownloadUrlsAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, IReadOnlyList<UrlDisplayType> urlDisplayTypes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(linkIds);
        ArgumentNullException.ThrowIfNull(packageIds);
        var types = FacadeGuard.UrlDisplayTypeArg(urlDisplayTypes, nameof(urlDisplayTypes));
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        var args = new[] { ids[0], ids[1], types };
        return FacadeResults.UrlMapAsync(_channel.CallAsync(Base + "getDownloadUrls", FacadeJsonContext.Default.DictionaryStringListInt64, args, cancellationToken));
    }
}
