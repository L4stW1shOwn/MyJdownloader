namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Selects the fields returned by <c>/linkgrabberv2/queryLinks</c>; every field is included by default except the password.</summary>
public sealed record LinkQuery
{
    /// <summary>Gets a value indicating whether the availability is returned.</summary>
    public bool Availability { get; init; } = true;

    /// <summary>Gets a value indicating whether the total size is returned.</summary>
    public bool BytesTotal { get; init; } = true;

    /// <summary>Gets a value indicating whether the comment is returned.</summary>
    public bool Comment { get; init; } = true;

    /// <summary>Gets a value indicating whether the enabled flag is returned.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets a value indicating whether the host is returned.</summary>
    public bool Host { get; init; } = true;

    /// <summary>Gets a value indicating whether the priority is returned.</summary>
    public bool Priority { get; init; } = true;

    /// <summary>Gets a value indicating whether the status is returned.</summary>
    public bool Status { get; init; } = true;

    /// <summary>Gets a value indicating whether the URL is returned.</summary>
    public bool Url { get; init; } = true;

    /// <summary>Gets a value indicating whether the variant identifier is returned.</summary>
    [JsonPropertyName("variantID")]
    public bool VariantId { get; init; } = true;

    /// <summary>Gets a value indicating whether the variant icon is returned.</summary>
    public bool VariantIcon { get; init; } = true;

    /// <summary>Gets a value indicating whether the variant name is returned.</summary>
    public bool VariantName { get; init; } = true;

    /// <summary>Gets a value indicating whether the variants flag is returned.</summary>
    public bool Variants { get; init; } = true;

    /// <summary>Gets a value indicating whether the download password is returned; <see langword="false"/> by default.</summary>
    public bool Password { get; init; }

    /// <summary>Gets the identifiers of the packages to restrict the query to, or <see langword="null"/> for all.</summary>
    [JsonPropertyName("packageUUIDs")]
    public IReadOnlyList<long>? PackageUuids { get; init; }

    /// <summary>Gets the identifiers of the collecting jobs to restrict the query to, or <see langword="null"/> for all.</summary>
    [JsonPropertyName("jobUUIDs")]
    public IReadOnlyList<long>? JobUuids { get; init; }

    /// <summary>Gets the maximum number of results; -1 for no limit.</summary>
    public int MaxResults { get; init; } = -1;

    /// <summary>Gets the index of the first result.</summary>
    public int StartAt { get; init; }
}
