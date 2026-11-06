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
        builder.Append("Uuid = ").Append(this.Uuid)
            .Append(", Name = ").Append(this.Name)
            .Append(", ActiveTask = ").Append(this.ActiveTask)
            .Append(", BytesLoaded = ").Append(this.BytesLoaded)
            .Append(", BytesTotal = ").Append(this.BytesTotal)
            .Append(", Speed = ").Append(this.Speed)
            .Append(", Eta = ").Append(this.Eta)
            .Append(", ChildCount = ").Append(this.ChildCount)
            .Append(", Comment = ").Append(this.Comment)
            .Append(", DownloadPassword = ").Append(this.DownloadPassword is null ? null : "***")
            .Append(", Enabled = ").Append(this.Enabled)
            .Append(", Finished = ").Append(this.Finished)
            .Append(", Running = ").Append(this.Running)
            .Append(", Hosts = ").Append(this.Hosts is null ? null : string.Join(',', this.Hosts))
            .Append(", Priority = ").Append(this.Priority)
            .Append(", SaveTo = ").Append(this.SaveTo)
            .Append(", Status = ").Append(this.Status)
            .Append(", StatusIconKey = ").Append(this.StatusIconKey);
        return true;
    }
}
