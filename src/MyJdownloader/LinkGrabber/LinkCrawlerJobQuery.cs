namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Selects the jobs returned by <c>/linkgrabberv2/queryLinkCrawlerJobs</c>.</summary>
public sealed record LinkCrawlerJobQuery
{
    /// <summary>Gets the identifiers of the jobs to restrict the query to, or <see langword="null"/> for all.</summary>
    public IReadOnlyList<long>? JobIds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the device is asked to fill the collector counters of each job
    /// (<c>Crawled</c>, <c>Broken</c>, <c>Filtered</c>, <c>Unhandled</c>; expected, to be confirmed by live tests).
    /// <see langword="false"/> is omitted from the request, which assumes the device default is <see langword="false"/> (to be confirmed by live tests).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CollectorInfo { get; init; }
}
