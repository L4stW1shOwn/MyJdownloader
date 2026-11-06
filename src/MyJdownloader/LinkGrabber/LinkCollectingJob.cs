namespace MyJdownloader.LinkGrabber;

/// <summary>A link collecting job started by adding links.</summary>
public sealed record LinkCollectingJob
{
    /// <summary>Gets the job identifier.</summary>
    public long Id { get; init; }
}
