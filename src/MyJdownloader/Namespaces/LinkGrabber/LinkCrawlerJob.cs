namespace MyJdownloader.LinkGrabber;

/// <summary>The state of a link crawler job, as reported by <c>/linkgrabberv2/queryLinkCrawlerJobs</c>.</summary>
public sealed record LinkCrawlerJob
{
    /// <summary>Gets the identifier of the collecting job.</summary>
    public long? JobId { get; init; }

    /// <summary>Gets the identifier of the crawler.</summary>
    public long? CrawlerId { get; init; }

    /// <summary>Gets a value indicating whether the job is still crawling.</summary>
    public bool? Crawling { get; init; }

    /// <summary>Gets a value indicating whether the job is checking the online status.</summary>
    public bool? Checking { get; init; }

    /// <summary>Gets the number of crawled links.</summary>
    public int? Crawled { get; init; }

    /// <summary>Gets the number of broken links.</summary>
    public int? Broken { get; init; }

    /// <summary>Gets the number of filtered links.</summary>
    public int? Filtered { get; init; }

    /// <summary>Gets the number of unhandled links.</summary>
    public int? Unhandled { get; init; }
}
