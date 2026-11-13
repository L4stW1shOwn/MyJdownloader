namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Selects the fields returned by <c>/linkgrabberv2/queryPackages</c>; every field is included by default.</summary>
public sealed record PackageQuery
{
    /// <summary>Gets a value indicating whether the offline link count is returned.</summary>
    public bool AvailableOfflineCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the online link count is returned.</summary>
    public bool AvailableOnlineCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the temporarily unknown link count is returned.</summary>
    public bool AvailableTempUnknownCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the unknown link count is returned.</summary>
    public bool AvailableUnknownCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the total size is returned.</summary>
    public bool BytesTotal { get; init; } = true;

    /// <summary>Gets a value indicating whether the child count is returned.</summary>
    public bool ChildCount { get; init; } = true;

    /// <summary>Gets a value indicating whether the comment is returned.</summary>
    public bool Comment { get; init; } = true;

    /// <summary>Gets a value indicating whether the enabled flag is returned.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets a value indicating whether the hosts are returned.</summary>
    public bool Hosts { get; init; } = true;

    /// <summary>Gets a value indicating whether the priority is returned.</summary>
    public bool Priority { get; init; } = true;

    /// <summary>Gets a value indicating whether the save folder is returned.</summary>
    public bool SaveTo { get; init; } = true;

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
