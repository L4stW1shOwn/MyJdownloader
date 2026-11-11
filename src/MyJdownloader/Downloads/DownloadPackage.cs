namespace MyJdownloader.Downloads;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>A package in the download list; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>
/// Do not destructure or serialize this record into logs. <see cref="object.ToString"/> masks
/// <see cref="DownloadPassword"/> as <c>***</c>.
/// <c>QueryPackagesAsync</c> may return <see cref="DownloadPassword"/> because the API has no flag to opt out of it.
/// </remarks>
public sealed record DownloadPackage
{
    /// <summary>Gets the package identifier.</summary>
    [JsonPropertyName("uuid")]
    public long Uuid { get; init; }

    /// <summary>Gets the package name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the task currently running on the package; its set of values is not documented.</summary>
    public string? ActiveTask { get; init; }

    /// <summary>Gets the downloaded size in bytes.</summary>
    public long? BytesLoaded { get; init; }

    /// <summary>Gets the total size in bytes.</summary>
    public long? BytesTotal { get; init; }

    /// <summary>Gets the current speed in bytes per second.</summary>
    public long? Speed { get; init; }

    /// <summary>Gets the estimated time remaining, or <see langword="null"/> when unknown.</summary>
    public TimeSpan? Eta { get; init; }

    /// <summary>Gets the number of links in the package.</summary>
    public int? ChildCount { get; init; }

    /// <summary>Gets the comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Gets the download password; the API returns it without an opt-out flag.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? DownloadPassword { get; init; }

    /// <summary>Gets a value indicating whether the package is enabled.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets a value indicating whether the package is finished.</summary>
    public bool? Finished { get; init; }

    /// <summary>Gets a value indicating whether the package is downloading.</summary>
    public bool? Running { get; init; }

    /// <summary>Gets the hosts serving the package links.</summary>
    public IReadOnlyList<string>? Hosts { get; init; }

    /// <summary>Gets the priority.</summary>
    public Priority? Priority { get; init; }

    /// <summary>Gets the folder the package is saved to.</summary>
    public string? SaveTo { get; init; }

    /// <summary>Gets the human-readable status text.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the key of the status icon.</summary>
    public string? StatusIconKey { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Uuid = ").Append(Uuid)
            .Append(", Name = ").Append(Name)
            .Append(", ActiveTask = ").Append(ActiveTask)
            .Append(", BytesLoaded = ").Append(BytesLoaded)
            .Append(", BytesTotal = ").Append(BytesTotal)
            .Append(", Speed = ").Append(Speed)
            .Append(", Eta = ").Append(Eta)
            .Append(", ChildCount = ").Append(ChildCount)
            .Append(", Comment = ").Append(Comment)
            .Append(", DownloadPassword = ").Append(DownloadPassword is null ? null : "***")
            .Append(", Enabled = ").Append(Enabled)
            .Append(", Finished = ").Append(Finished)
            .Append(", Running = ").Append(Running)
            .Append(", Hosts = ").Append(Hosts is null ? null : string.Join(',', Hosts))
            .Append(", Priority = ").Append(Priority)
            .Append(", SaveTo = ").Append(SaveTo)
            .Append(", Status = ").Append(Status)
            .Append(", StatusIconKey = ").Append(StatusIconKey);
        return true;
    }
}
