namespace MyJdownloader.LinkGrabber;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>A link in the link grabber list; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>
/// Do not destructure or serialize this record into logs. <see cref="object.ToString"/> masks
/// <see cref="DownloadPassword"/> as <c>***</c>, but <see cref="Url"/> may embed credentials.
/// </remarks>
public sealed record CrawledLink
{
    /// <summary>Gets the link identifier.</summary>
    [JsonPropertyName("uuid")]
    public long Uuid { get; init; }

    /// <summary>Gets the identifier of the package containing the link.</summary>
    [JsonPropertyName("packageUUID")]
    public long PackageUuid { get; init; }

    /// <summary>Gets the link name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the link URL.</summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Raw API value, may not be a valid URI.")]
    public string? Url { get; init; }

    /// <summary>Gets the host serving the link.</summary>
    public string? Host { get; init; }

    /// <summary>Gets the total size in bytes.</summary>
    public long? BytesTotal { get; init; }

    /// <summary>Gets the online availability.</summary>
    public AvailableLinkState? Availability { get; init; }

    /// <summary>Gets a value indicating whether the link is enabled.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets the priority.</summary>
    public Priority? Priority { get; init; }

    /// <summary>Gets the comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Gets the download password; only returned when requested by the query.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? DownloadPassword { get; init; }

    /// <summary>Gets a value indicating whether the link has variants.</summary>
    public bool? Variants { get; init; }

    /// <summary>Gets the selected variant.</summary>
    public LinkVariant? Variant { get; init; }

    /// <summary>Gets the human-readable status text; returned when the Status flag is set.</summary>
    /// <remarks>Not listed in the official MyJDownloader API documentation; observed in JDownloader storables, so the device may omit it.</remarks>
    public string? Status { get; init; }

    /// <summary>Gets the key of the status icon; returned when the Status flag is set.</summary>
    /// <remarks>Not listed in the official MyJDownloader API documentation; observed in JDownloader storables, so the device may omit it.</remarks>
    public string? StatusIconKey { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Uuid = ").Append(Uuid)
            .Append(", PackageUuid = ").Append(PackageUuid)
            .Append(", Name = ").Append(Name)
            .Append(", Url = ").Append(Url)
            .Append(", Host = ").Append(Host)
            .Append(", BytesTotal = ").Append(BytesTotal)
            .Append(", Availability = ").Append(Availability)
            .Append(", Enabled = ").Append(Enabled)
            .Append(", Priority = ").Append(Priority)
            .Append(", Comment = ").Append(Comment)
            .Append(", DownloadPassword = ").Append(DownloadPassword is null ? null : "***")
            .Append(", Variants = ").Append(Variants)
            .Append(", Variant = ").Append(Variant)
            .Append(", Status = ").Append(Status)
            .Append(", StatusIconKey = ").Append(StatusIconKey);
        return true;
    }
}
