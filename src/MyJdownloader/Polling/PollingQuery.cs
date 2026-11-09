namespace MyJdownloader.Polling;

/// <summary>Selects the events returned by <c>/polling/poll</c>; every event is requested by default, and at least one must stay enabled.</summary>
public sealed record PollingQuery
{
    /// <summary>Gets a value indicating whether the <c>downloadProgress</c> event is requested; its <c>data</c> lists the running packages with their speed and their running links.</summary>
    public bool DownloadProgress { get; init; } = true;

    /// <summary>Gets a value indicating whether the <c>jdState</c> event is requested; its <c>data</c> is the download state label, for example <c>RUNNING</c>, <c>IDLE</c>, <c>PAUSE</c> or <c>STOPPING</c>.</summary>
    public bool JdState { get; init; } = true;

    /// <summary>Gets a value indicating whether the <c>linkGrabberState</c> event is requested; its <c>data</c> is the link checker state, <c>RUNNING</c> or <c>IDLE</c>.</summary>
    public bool LinkGrabberState { get; init; } = true;

    /// <summary>Gets a value indicating whether the <c>captchasWaiting</c> event is requested; its <c>data</c> lists the waiting captcha jobs, as returned by <c>/captcha/list</c>.</summary>
    public bool CaptchasWaiting { get; init; } = true;

    /// <summary>Gets a value indicating whether the <c>aggregatedNumbers</c> event is requested; its <c>data</c> holds the aggregated download and crawler numbers.</summary>
    public bool AggregatedNumbers { get; init; } = true;

    /// <summary>Gets the UUIDs of the packages whose running links are included in the <c>downloadProgress</c> event; <see langword="null"/> includes the links of every running package.</summary>
    /// <remarks>Only meaningful when <see cref="DownloadProgress"/> is <see langword="true"/>; other running packages are then listed without links.</remarks>
    public IReadOnlyList<long>? DownloadProgressPackageUuids { get; init; }
}
