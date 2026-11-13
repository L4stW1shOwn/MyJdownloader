namespace MyJdownloader.LinkGrabber;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>A package in the link grabber list; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>
/// Do not destructure or serialize this record into logs. <see cref="object.ToString"/> masks
/// <see cref="DownloadPassword"/> as <c>***</c>. <c>QueryPackagesAsync</c> may return
/// <see cref="DownloadPassword"/> because the API has no flag to opt out of it.
/// </remarks>
public sealed record CrawledPackage
{
    /// <summary>Gets the package identifier.</summary>
    [JsonPropertyName("uuid")]
    public long Uuid { get; init; }

    /// <summary>Gets the package name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the total size in bytes.</summary>
    public long? BytesTotal { get; init; }

    /// <summary>Gets the number of links in the package.</summary>
    public int? ChildCount { get; init; }

    /// <summary>Gets the comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Gets the download password, when the device returns it.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? DownloadPassword { get; init; }

    /// <summary>Gets a value indicating whether the package is enabled.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets the hosts serving the package links.</summary>
    public IReadOnlyList<string>? Hosts { get; init; }

    /// <summary>Gets the number of online links.</summary>
    public int? OnlineCount { get; init; }

    /// <summary>Gets the number of offline links.</summary>
    public int? OfflineCount { get; init; }

    /// <summary>Gets the number of temporarily unknown links.</summary>
    public int? TempUnknownCount { get; init; }

    /// <summary>Gets the number of links with an unknown state.</summary>
    public int? UnknownCount { get; init; }

    /// <summary>Gets the priority.</summary>
    public Priority? Priority { get; init; }

    /// <summary>Gets the folder the package is saved to.</summary>
    public string? SaveTo { get; init; }

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
            .Append(", Name = ").Append(Name)
            .Append(", BytesTotal = ").Append(BytesTotal)
            .Append(", ChildCount = ").Append(ChildCount)
            .Append(", Comment = ").Append(Comment)
            .Append(", DownloadPassword = ").Append(DownloadPassword is null ? null : "***")
            .Append(", Enabled = ").Append(Enabled)
            .Append(", Hosts = ").Append(Hosts is null ? null : string.Join(',', Hosts))
            .Append(", OnlineCount = ").Append(OnlineCount)
            .Append(", OfflineCount = ").Append(OfflineCount)
            .Append(", TempUnknownCount = ").Append(TempUnknownCount)
            .Append(", UnknownCount = ").Append(UnknownCount)
            .Append(", Priority = ").Append(Priority)
            .Append(", SaveTo = ").Append(SaveTo)
            .Append(", Status = ").Append(Status)
            .Append(", StatusIconKey = ").Append(StatusIconKey);
        return true;
    }
}
