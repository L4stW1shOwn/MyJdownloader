namespace MyJdownloader.Downloads;

using System.Text.Json.Serialization;

/// <summary>Selects the fields returned by <c>/downloadsV2/queryPackages</c>; every field is included by default.</summary>
public sealed record DownloadPackageQuery
{
    /// <summary>Gets a value indicating whether the loaded size is returned.</summary>
    public bool BytesLoaded { get; init; } = true;

    /// <summary>Gets a value indicating whether the total size is returned.</summary>
    public bool BytesTotal { get; init; } = true;

    /// <summary>Gets a value indicating whether the child count is returned.</summary>
    public bool ChildCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the comment is returned.</summary>
    public bool Comment { get; init; } = true;

    /// <summary>Gets a value indicating whether the enabled flag is returned.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets a value indicating whether the estimated time remaining is returned.</summary>
    public bool Eta { get; init; } = true;

    /// <summary>Gets a value indicating whether the finished flag is returned.</summary>
    public bool Finished { get; init; } = true;

    /// <summary>Gets a value indicating whether the hosts is returned.</summary>
    public bool Hosts { get; init; } = true;

    /// <summary>Gets a value indicating whether the priority is returned.</summary>
    public bool Priority { get; init; } = true;

    /// <summary>Gets a value indicating whether the running flag is returned.</summary>
    public bool Running { get; init; } = true;

    /// <summary>Gets a value indicating whether the save folder is returned.</summary>
    public bool SaveTo { get; init; } = true;

    /// <summary>Gets a value indicating whether the speed is returned.</summary>
    public bool Speed { get; init; } = true;

    /// <summary>Gets a value indicating whether the status is returned.</summary>
    public bool Status { get; init; } = true;

    /// <summary>Gets the identifiers of the packages to restrict the query to, or <see langword="null"/> for all.</summary>
    [JsonPropertyName("packageUUIDs")]
    public IReadOnlyList<long>? PackageUuids { get; init; }

    /// <summary>Gets the maximum number of results; -1 for no limit.</summary>
    public int MaxResults { get; init; } = -1;

    /// <summary>Gets the index of the first result.</summary>
    public int StartAt { get; init; }
}
