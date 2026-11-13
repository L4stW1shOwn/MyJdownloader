namespace MyJdownloader.Downloads;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>A link in the download list; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>
/// Do not destructure or serialize this record into logs. <see cref="object.ToString"/> masks
/// <see cref="DownloadPassword"/> as <c>***</c>, but <see cref="Url"/> may embed credentials.
/// </remarks>
public sealed record DownloadLink
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

    /// <summary>Gets the comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Gets the download password, when requested.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? DownloadPassword { get; init; }

    /// <summary>Gets the downloaded size in bytes.</summary>
    public long? BytesLoaded { get; init; }

    /// <summary>Gets the total size in bytes.</summary>
    public long? BytesTotal { get; init; }

    /// <summary>Gets the current speed in bytes per second.</summary>
    public long? Speed { get; init; }

    /// <summary>Gets the estimated time remaining, or <see langword="null"/> when unknown.</summary>
    public TimeSpan? Eta { get; init; }

    /// <summary>Gets a value indicating whether the link is enabled.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets a value indicating whether the download is finished.</summary>
    public bool? Finished { get; init; }

    /// <summary>Gets a value indicating whether the link is downloading.</summary>
    public bool? Running { get; init; }

    /// <summary>Gets a value indicating whether the link was skipped.</summary>
    public bool? Skipped { get; init; }

    /// <summary>Gets the priority.</summary>
    public Priority? Priority { get; init; }

    /// <summary>Gets the human-readable status text.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the key of the status icon.</summary>
    public string? StatusIconKey { get; init; }

    /// <summary>Gets the archive extraction status; its set of values is not documented.</summary>
    public string? ExtractionStatus { get; init; }

    /// <summary>Gets the date the link was added, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? AddedDate { get; init; }

    /// <summary>Gets the date the download finished, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? FinishedDate { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Uuid = ").Append(Uuid)
            .Append(", PackageUuid = ").Append(PackageUuid)
            .Append(", Name = ").Append(Name)
            .Append(", Url = ").Append(Url)
            .Append(", Host = ").Append(Host)
            .Append(", Comment = ").Append(Comment)
            .Append(", DownloadPassword = ").Append(DownloadPassword is null ? null : "***")
            .Append(", BytesLoaded = ").Append(BytesLoaded)
            .Append(", BytesTotal = ").Append(BytesTotal)
            .Append(", Speed = ").Append(Speed)
            .Append(", Eta = ").Append(Eta)
            .Append(", Enabled = ").Append(Enabled)
            .Append(", Finished = ").Append(Finished)
            .Append(", Running = ").Append(Running)
            .Append(", Skipped = ").Append(Skipped)
            .Append(", Priority = ").Append(Priority)
            .Append(", Status = ").Append(Status)
            .Append(", StatusIconKey = ").Append(StatusIconKey)
            .Append(", ExtractionStatus = ").Append(ExtractionStatus)
            .Append(", AddedDate = ").Append(AddedDate)
            .Append(", FinishedDate = ").Append(FinishedDate);
        return true;
    }
}
