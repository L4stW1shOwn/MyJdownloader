namespace MyJdownloader.Downloads;

using System.Text.Json;
using MyJdownloader.Errors;
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

    /// <summary>Moves packages after a given package (<c>/downloadsV2/movePackages</c>).</summary>
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

    /// <summary>Splits packages by hoster (<c>/downloadsV2/splitPackageByHoster</c>).</summary>
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

    /// <summary>Starts an online status check (<c>/downloadsV2/startOnlineStatusCheck</c>).</summary>
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

    /// <summary>Sets or clears the download password of links and packages (<c>/downloadsV2/setDownloadPassword</c>).</summary>
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

    /// <summary>Clears the skipped state of links and packages (<c>/downloadsV2/unskip</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to unskip.</param>
    /// <param name="packageIds">The identifiers of the packages to unskip.</param>
    /// <param name="reason">The skip reason to clear, or <see langword="null"/> for all reasons.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the change.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is <see cref="SkipReason.Unknown"/> or not a defined value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> UnskipAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, SkipReason? reason, CancellationToken cancellationToken = default)
    {
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        if (reason is { } value && !FacadeGuard.IsKnown(value))
        {
            throw new ArgumentException("The skip reason is not valid.", nameof(reason));
        }

        // JSON null: no typed null for the enum; the device reads it as "all reasons".
        var reasonArg = reason is { } known
            ? JsonSerializer.SerializeToElement(known, DownloadsJsonContext.Default.SkipReason)
            : JsonSerializer.SerializeToElement(null, FacadeJsonContext.Default.String);

        // The protocol sends package ids first; this order must not be normalized.
        var args = new[] { ids[1], ids[0], reasonArg };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "unskip", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Gets the download URLs of links and packages (<c>/downloadsV2/getDownloadUrls</c>).</summary>
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
