namespace MyJdownloader.Downloads;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/downloadsV2</c> namespace of one device (the download list).</summary>
/// <remarks>
/// <para>
/// Arguments are validated synchronously, before any request is sent; exception messages never contain argument values.
/// </para>
/// <para>
/// Links may embed credentials and packages carry a download password: do not log whole <see cref="DownloadLink"/> or <see cref="DownloadPackage"/> values.
/// Destination paths are paths on the device and are not filtered: callers handling untrusted input must allow-list them.
/// <see cref="CleanupAsync"/> can delete files on the device and the library adds no confirmation step.
/// </para>
/// </remarks>
public sealed partial class DownloadsApi
{
    private const string Base = "/downloadsV2/";

    private readonly IDeviceChannel _channel;

    internal DownloadsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Lists the downloads (<c>/downloadsV2/queryLinks</c>).</summary>
    /// <param name="query">The fields and range to return, or <see langword="null"/> for all fields except the password and all links.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The links; empty when the device returns no data.</returns>
    /// <remarks>
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): on a large list, page with <see cref="DownloadLinkQuery.StartAt"/> and <see cref="DownloadLinkQuery.MaxResults"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>. Links may embed credentials: do not log whole records.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="DownloadLinkQuery.MaxResults"/> is below -1 or <see cref="DownloadLinkQuery.StartAt"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<DownloadLink>> QueryLinksAsync(DownloadLinkQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new DownloadLinkQuery();
        FacadeGuard.ValidateRange(query.MaxResults, query.StartAt, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, DownloadsJsonContext.Default.DownloadLinkQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "queryLinks", DownloadsJsonContext.Default.ListDownloadLink, args, cancellationToken));
    }

    /// <summary>Lists the download packages (<c>/downloadsV2/queryPackages</c>).</summary>
    /// <param name="query">The fields and range to return, or <see langword="null"/> for all fields and all packages.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The packages; empty when the device returns no data.</returns>
    /// <remarks>
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): on a large list, page with <see cref="DownloadPackageQuery.StartAt"/> and <see cref="DownloadPackageQuery.MaxResults"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>. Do not log whole records: the device may return secrets such as the download password without an opt-out flag.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="DownloadPackageQuery.MaxResults"/> is below -1 or <see cref="DownloadPackageQuery.StartAt"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<DownloadPackage>> QueryPackagesAsync(DownloadPackageQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new DownloadPackageQuery();
        FacadeGuard.ValidateRange(query.MaxResults, query.StartAt, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, DownloadsJsonContext.Default.DownloadPackageQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "queryPackages", DownloadsJsonContext.Default.ListDownloadPackage, args, cancellationToken), static package => package.Hosts?.Contains(null!) == true);
    }

    /// <summary>Gets the number of packages in the download list (<c>/downloadsV2/packageCount</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The package count.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<int> GetPackageCountAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "packageCount", FacadeJsonContext.Default.NullableInt32, [], cancellationToken));

    /// <summary>Removes links and packages from the download list (<c>/downloadsV2/removeLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to remove.</param>
    /// <param name="packageIds">The identifiers of the packages to remove.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Only the list entries are removed; downloaded files are kept. To delete files, see <see cref="CleanupAsync"/>.</remarks>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RemoveLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "removeLinks", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Cleans up links and packages of the download list (<c>/downloadsV2/cleanup</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to clean up.</param>
    /// <param name="packageIds">The identifiers of the packages to clean up.</param>
    /// <param name="action">What to clean up; <see cref="CleanupAction.Unknown"/> is rejected.</param>
    /// <param name="mode">What to do with the links and files; <see cref="CleanupMode.Unknown"/> is rejected.</param>
    /// <param name="selectionType">Which of the given links and packages are targeted; <see cref="SelectionType.Unknown"/> is rejected.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// Destructive: with <see cref="CleanupMode.RemoveLinksAndDeleteFiles"/> the files are deleted on the device, with <see cref="CleanupMode.RemoveLinksAndRecycleFiles"/> they are moved to the recycle bin.
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

        if (selectionType == SelectionType.Unselected && linkIds.Count == 0 && packageIds.Count == 0)
        {
            throw new ArgumentException("Unselected with no identifier targets every item; use SelectionType.All explicitly.", nameof(selectionType));
        }

        var args = new[]
        {
            ids[0],
            ids[1],
            JsonSerializer.SerializeToElement(action, DownloadsJsonContext.Default.CleanupAction),
            JsonSerializer.SerializeToElement(mode, DownloadsJsonContext.Default.CleanupMode),
            JsonSerializer.SerializeToElement(selectionType, DownloadsJsonContext.Default.SelectionType),
        };
        return this._channel.CallAsync(Base + "cleanup", args, cancellationToken);
    }

    /// <summary>Enables or disables links and packages (<c>/downloadsV2/setEnabled</c>).</summary>
    /// <param name="enabled">The new enabled state.</param>
    /// <param name="linkIds">The identifiers of the links to change.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SetEnabledAsync(bool enabled, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        var args = new[] { JsonSerializer.SerializeToElement(enabled, FacadeJsonContext.Default.Boolean), ids[0], ids[1] };
        return this._channel.CallAsync(Base + "setEnabled", args, cancellationToken);
    }

    /// <summary>Sets the priority of links and packages (<c>/downloadsV2/setPriority</c>).</summary>
    /// <param name="priority">The new priority; <see cref="Priority.Unknown"/> is rejected.</param>
    /// <param name="linkIds">The identifiers of the links to change.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="priority"/> is <see cref="Priority.Unknown"/> or not a defined value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SetPriorityAsync(Priority priority, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        if (!FacadeGuard.IsKnown(priority))
        {
            throw new ArgumentException("The priority must be a known value.", nameof(priority));
        }

        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        var args = new[] { JsonSerializer.SerializeToElement(priority, DownloadsJsonContext.Default.Priority), ids[0], ids[1] };
        return this._channel.CallAsync(Base + "setPriority", args, cancellationToken);
    }

    /// <summary>Renames a link (<c>/downloadsV2/renameLink</c>).</summary>
    /// <param name="linkId">The identifier of the link.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The new name becomes a file name on the device. JDownloader sanitizes it, but callers handling untrusted input should restrict it to an allow-list of characters.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newName"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RenameLinkAsync(long linkId, string newName, CancellationToken cancellationToken = default)
        => this.RenameAsync("renameLink", linkId, newName, cancellationToken);

    /// <summary>Renames a package (<c>/downloadsV2/renamePackage</c>).</summary>
    /// <param name="packageId">The identifier of the package.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The new name becomes a package name on the device. JDownloader sanitizes it, but callers handling untrusted input should restrict it to an allow-list of characters.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newName"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RenamePackageAsync(long packageId, string newName, CancellationToken cancellationToken = default)
        => this.RenameAsync("renamePackage", packageId, newName, cancellationToken);

    /// <summary>Forces the download of links and packages (<c>/downloadsV2/forceDownload</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to force.</param>
    /// <param name="packageIds">The identifiers of the packages to force.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The success flag returned by the device; JDownloader answers <see langword="true"/> once the request is accepted.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> ForceDownloadAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "forceDownload", FacadeJsonContext.Default.NullableBoolean, FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken));

    /// <summary>Resets links and packages (downloaded data is discarded) (<c>/downloadsV2/resetLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to reset.</param>
    /// <param name="packageIds">The identifiers of the packages to reset.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ResetLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "resetLinks", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Resumes links and packages (<c>/downloadsV2/resumeLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to resume.</param>
    /// <param name="packageIds">The identifiers of the packages to resume.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task ResumeLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "resumeLinks", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Sets the download directory of packages (<c>/downloadsV2/setDownloadDirectory</c>).</summary>
    /// <param name="directory">The destination directory, a path on the device.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The path is a path on the device and is not filtered: callers handling untrusted input must allow-list it.</remarks>
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
        return this._channel.CallAsync(Base + "setDownloadDirectory", args, cancellationToken);
    }

    /// <summary>Moves links and packages to a new package (<c>/downloadsV2/movetoNewPackage</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to move.</param>
    /// <param name="packageIds">The identifiers of the packages to move.</param>
    /// <param name="newPackageName">The name of the new package.</param>
    /// <param name="downloadPath">The download directory of the new package, a path on the device.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The package name and the download path are not filtered: callers handling untrusted input must allow-list them.</remarks>
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
        return this._channel.CallAsync(Base + "movetoNewPackage", args, cancellationToken);
    }

    private Task RenameAsync(string action, long id, string newName, CancellationToken cancellationToken)
    {
        FacadeGuard.ValidateText(newName, nameof(newName), FacadeGuard.MaxIdentifierLength);
        var args = new[]
        {
            JsonSerializer.SerializeToElement(id, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(newName, FacadeJsonContext.Default.String),
        };
        return this._channel.CallAsync(Base + action, args, cancellationToken);
    }
}
