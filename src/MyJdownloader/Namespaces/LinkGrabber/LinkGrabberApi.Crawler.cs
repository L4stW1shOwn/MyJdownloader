namespace MyJdownloader.LinkGrabber;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MyJdownloader.Errors;
using MyJdownloader.Facades;

public sealed partial class LinkGrabberApi
{
    /// <summary>Lists the link crawler jobs (<c>/linkgrabberv2/queryLinkCrawlerJobs</c>).</summary>
    /// <param name="query">The jobs to return, or <see langword="null"/> for all jobs.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The jobs; empty when the device answers nothing.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="query"/> contains a negative job identifier.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<LinkCrawlerJob>> QueryLinkCrawlerJobsAsync(LinkCrawlerJobQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new LinkCrawlerJobQuery();
        var copy = FacadeGuard.OptionalIds(query.JobIds, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query with { JobIds = copy }, LinkGrabberJsonContext.Default.LinkCrawlerJobQuery) };
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "queryLinkCrawlerJobs", LinkGrabberJsonContext.Default.ListLinkCrawlerJob, args, cancellationToken));
    }

    /// <summary>Aborts the link crawler jobs (<c>/linkgrabberv2/abort</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value reported by the device.</returns>
    /// <remarks>Stops every running crawl job (to be confirmed by live tests). Applied without confirmation.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    [SuppressMessage("ApiDesign", "RS0026", Justification = "The overloads cannot be ambiguous: they differ by the mandatory jobId parameter")]
    public Task<bool> AbortAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(_channel.CallAsync(Base + "abort", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));

    /// <summary>Aborts one link crawler job (<c>/linkgrabberv2/abort</c>).</summary>
    /// <param name="jobId">The identifier of the job to abort.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value reported by the device.</returns>
    /// <remarks>Applied without confirmation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="jobId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    [SuppressMessage("ApiDesign", "RS0026", Justification = "The overloads cannot be ambiguous: they differ by the mandatory jobId parameter")]
    public Task<bool> AbortAsync(long jobId, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(jobId, nameof(jobId));
        var args = new[] { JsonSerializer.SerializeToElement(jobId, FacadeJsonContext.Default.Int64) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "abort", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Gets the children change counter of the link grabber (<c>/linkgrabberv2/getChildrenChanged</c>).</summary>
    /// <param name="structureWatermark">The last structure watermark known by the caller.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value reported by the device.</returns>
    /// <remarks>The exact semantics of the watermark and of the result are to be confirmed by live tests.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="structureWatermark"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<long> GetChildrenChangedAsync(long structureWatermark, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(structureWatermark, nameof(structureWatermark));
        var args = new[] { JsonSerializer.SerializeToElement(structureWatermark, FacadeJsonContext.Default.Int64) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "getChildrenChanged", FacadeJsonContext.Default.NullableInt64, args, cancellationToken));
    }

    /// <summary>Lists the download folder history (<c>/linkgrabberv2/getDownloadFolderHistorySelectionBase</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The folders; empty when the device answers nothing.</returns>
    /// <remarks>Folder entries are untrusted device paths that may contain user names or share credentials: do not log them.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<string>> GetDownloadFolderHistoryAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(_channel.CallAsync(Base + "getDownloadFolderHistorySelectionBase", LinkGrabberJsonContext.Default.ListString, [], cancellationToken));
}
