namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Selects the jobs returned by <c>/linkgrabberv2/queryLinkCrawlerJobs</c>.</summary>
public sealed record LinkCrawlerJobQuery
{
    /// <summary>Gets the identifiers of the jobs to restrict the query to, or <see langword="null"/> for all.</summary>
    public IReadOnlyList<long>? JobIds { get; init; }

    /// <summary>Gets a value indicating whether the collector information is returned; omitted from the request when <see langword="false"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CollectorInfo { get; init; }
}
