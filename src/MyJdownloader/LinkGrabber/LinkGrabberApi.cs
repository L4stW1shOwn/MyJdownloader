namespace MyJdownloader.LinkGrabber;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/linkgrabberv2</c> namespace of one device (links collected but not yet in the download list).</summary>
/// <remarks>
/// <para>
/// Arguments are validated synchronously, before any request is sent; exception messages never contain argument values.
/// </para>
/// <para>
/// Links and URLs may embed credentials: do not log whole <see cref="AddLinksRequest"/>, <see cref="CrawledLink"/> or <see cref="CrawledPackage"/> values.
/// The library does not filter URL schemes nor destination paths: the device fetches the links from its own network and
/// <see cref="AddLinksRequest.DestinationFolder"/> is a path on the device, so callers handling untrusted input must allow-list both.
/// </para>
/// </remarks>
public sealed class LinkGrabberApi
{
    private const string Base = "/linkgrabberv2/";

    private readonly IDeviceChannel _channel;

    internal LinkGrabberApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Adds links to the link grabber (<c>/linkgrabberv2/addLinks</c>).</summary>
    /// <param name="request">The links and options; links are sent joined with line feeds.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The link collecting job.</returns>
    /// <remarks>Links and <see cref="AddLinksRequest.SourceUrl"/> may embed credentials. The library does not filter URL schemes nor destination paths: allow-list them when they come from untrusted input.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/>, its links or one of its links is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There is no link, a link is blank or contains whitespace, control characters or lone surrogates, a text option is blank or contains control characters, or the priority is <see cref="Priority.Unknown"/> or not a defined value.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<LinkCollectingJob> AddLinksAsync(AddLinksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Links, nameof(request));
        var links = request.Links.ToArray();
        if (links.Length == 0)
        {
            throw new ArgumentException("At least one link is required.", nameof(request));
        }

        foreach (var link in links)
        {
            ValidateLink(link, nameof(request));
        }

        FacadeGuard.ValidateOptionalText(request.PackageName, nameof(request));
        FacadeGuard.ValidateOptionalText(request.DestinationFolder, nameof(request));
        FacadeGuard.ValidateOptionalText(request.SourceUrl, nameof(request));
        if (request.Priority is { } requested && !FacadeGuard.IsKnown(requested))
        {
            throw new ArgumentException("The priority must be a known value.", nameof(request));
        }

        var args = new[] { JsonSerializer.SerializeToElement(AddLinksQueryDto.From(request, links), LinkGrabberJsonContext.Default.AddLinksQueryDto) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "addLinks", LinkGrabberJsonContext.Default.LinkCollectingJob, args, cancellationToken));
    }

    /// <summary>Lists the collected links (<c>/linkgrabberv2/queryLinks</c>).</summary>
    /// <param name="query">The fields and range to return, or <see langword="null"/> for all fields except the password and all links.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The links; empty when the device returns no data.</returns>
    /// <remarks>
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): on a large list, page with <see cref="LinkQuery.StartAt"/> and <see cref="LinkQuery.MaxResults"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>. Links may embed credentials: do not log whole records.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="LinkQuery.MaxResults"/> is below -1 or <see cref="LinkQuery.StartAt"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or over 1 MiB.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<CrawledLink>> QueryLinksAsync(LinkQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new LinkQuery();
        FacadeGuard.ValidateRange(query.MaxResults, query.StartAt, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, LinkGrabberJsonContext.Default.LinkQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "queryLinks", LinkGrabberJsonContext.Default.ListCrawledLink, args, cancellationToken));
    }

    /// <summary>Lists the collected packages (<c>/linkgrabberv2/queryPackages</c>).</summary>
    /// <param name="query">The fields and range to return, or <see langword="null"/> for all fields and all packages.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The packages; empty when the device returns no data.</returns>
    /// <remarks>
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): on a large list, page with <see cref="PackageQuery.StartAt"/> and <see cref="PackageQuery.MaxResults"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>. Do not log whole records: the device may return secrets such as the download password without an opt-out flag.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="PackageQuery.MaxResults"/> is below -1 or <see cref="PackageQuery.StartAt"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or over 1 MiB.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<CrawledPackage>> QueryPackagesAsync(PackageQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PackageQuery();
        FacadeGuard.ValidateRange(query.MaxResults, query.StartAt, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, LinkGrabberJsonContext.Default.PackageQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "queryPackages", LinkGrabberJsonContext.Default.ListCrawledPackage, args, cancellationToken), static package => package.Hosts?.Contains(null!) == true);
    }

    /// <summary>Moves links and packages to the download list (<c>/linkgrabberv2/moveToDownloadlist</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to move.</param>
    /// <param name="packageIds">The identifiers of the packages to move.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task MoveToDownloadListAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "moveToDownloadlist", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Removes links and packages from the link grabber (<c>/linkgrabberv2/removeLinks</c>).</summary>
    /// <param name="linkIds">The identifiers of the links to remove.</param>
    /// <param name="packageIds">The identifiers of the packages to remove.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RemoveLinksAsync(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "removeLinks", FacadeGuard.IdArgs(linkIds, packageIds), cancellationToken);

    /// <summary>Clears the whole link grabber list (<c>/linkgrabberv2/clearList</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value returned by the device.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> ClearListAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "clearList", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Tells whether the link grabber is still collecting links (<c>/linkgrabberv2/isCollecting</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> while links are being collected.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsCollectingAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "isCollecting", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Gets the number of packages in the link grabber (<c>/linkgrabberv2/getPackageCount</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The package count.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<int> GetPackageCountAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getPackageCount", FacadeJsonContext.Default.NullableInt32, [], cancellationToken));

    /// <summary>Renames a link (<c>/linkgrabberv2/renameLink</c>).</summary>
    /// <param name="linkId">The identifier of the link.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The new name becomes a file name on the device. JDownloader sanitizes it, but callers handling untrusted input should restrict it to an allow-list of characters.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newName"/> is blank or contains control characters.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RenameLinkAsync(long linkId, string newName, CancellationToken cancellationToken = default)
        => this.RenameAsync("renameLink", linkId, newName, cancellationToken);

    /// <summary>Renames a package (<c>/linkgrabberv2/renamePackage</c>).</summary>
    /// <param name="packageId">The identifier of the package.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>The new name becomes a package name on the device. JDownloader sanitizes it, but callers handling untrusted input should restrict it to an allow-list of characters.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="newName"/> is blank or contains control characters.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RenamePackageAsync(long packageId, string newName, CancellationToken cancellationToken = default)
        => this.RenameAsync("renamePackage", packageId, newName, cancellationToken);

    /// <summary>Enables or disables links and packages (<c>/linkgrabberv2/setEnabled</c>).</summary>
    /// <param name="enabled">The new enabled state.</param>
    /// <param name="linkIds">The identifiers of the links to change.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
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

    /// <summary>Sets the priority of links and packages (<c>/linkgrabberv2/setPriority</c>).</summary>
    /// <param name="priority">The new priority; <see cref="Priority.Unknown"/> is rejected.</param>
    /// <param name="linkIds">The identifiers of the links to change.</param>
    /// <param name="packageIds">The identifiers of the packages to change.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException">An identifier list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="priority"/> is <see cref="Priority.Unknown"/> or not a defined value.</exception>
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
        var args = new[] { JsonSerializer.SerializeToElement(priority, LinkGrabberJsonContext.Default.Priority), ids[0], ids[1] };
        return this._channel.CallAsync(Base + "setPriority", args, cancellationToken);
    }

    private static void ValidateLink(string link, string paramName)
    {
        ArgumentNullException.ThrowIfNull(link, paramName);
        if (link.Length == 0)
        {
            throw new ArgumentException("A link must not be blank.", paramName);
        }

        for (var i = 0; i < link.Length; i++)
        {
            var c = link[i];
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                throw new ArgumentException("A link must not contain whitespace or control characters.", paramName);
            }

            if (char.IsHighSurrogate(c) && i + 1 < link.Length && char.IsLowSurrogate(link[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                throw new ArgumentException("A link must not contain invalid surrogate characters.", paramName);
            }
        }
    }

    private Task RenameAsync(string action, long id, string newName, CancellationToken cancellationToken)
    {
        FacadeGuard.ValidateText(newName, nameof(newName));
        var args = new[]
        {
            JsonSerializer.SerializeToElement(id, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(newName, FacadeJsonContext.Default.String),
        };
        return this._channel.CallAsync(Base + action, args, cancellationToken);
    }
}
