namespace MyJdownloader.Extraction;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>The extraction settings of an archive, used for reading and updating.</summary>
/// <remarks>
/// The device returns passwords in clear. Do not log or serialize this record: <see cref="object.ToString"/> masks
/// the secrets, but value equality includes them. Members left <see langword="null"/> are not sent, so the device keeps its default.
/// </remarks>
public sealed record ArchiveSettings
{
    /// <summary>Gets the archive identifier.</summary>
    /// <remarks>When sent through <see cref="ExtractionApi.SetArchiveSettingsAsync"/>, it must be <see langword="null"/> or equal to the archive identifier argument.</remarks>
    public string? ArchiveId { get; init; }

    /// <summary>Gets a value indicating whether the archive is extracted automatically.</summary>
    public bool? AutoExtract { get; init; }

    /// <summary>Gets the extraction target path, a path on the device; do not pass untrusted input.</summary>
    public string? ExtractPath { get; init; }

    /// <summary>Gets the final password, returned in clear by the device; never log it.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? FinalPassword { get; init; }

    /// <summary>Gets the candidate passwords, returned in clear by the device; never log them.</summary>
    /// <remarks>An empty list is sent as is and may clear the candidate passwords on the device; leave <see langword="null"/> to keep them.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IReadOnlyList<string>? Passwords { get; init; }

    /// <summary>Gets a value indicating whether the download links are removed after extraction.</summary>
    /// <remarks><see langword="true"/> removes the links from the device download list after extraction, which cannot be undone.</remarks>
    public bool? RemoveDownloadLinksAfterExtraction { get; init; }

    /// <summary>Gets a value indicating whether the archive files are deleted after extraction.</summary>
    /// <remarks><see langword="true"/> deletes the archive files on the device after extraction, which cannot be undone.</remarks>
    public bool? RemoveFilesAfterExtraction { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("ArchiveId = ").Append(ArchiveId)
            .Append(", AutoExtract = ").Append(AutoExtract)
            .Append(", ExtractPath = ").Append(ExtractPath)
            .Append(", FinalPassword = ").Append(FinalPassword is null ? null : "***")
            .Append(", Passwords = ").Append(Passwords?.Count)
            .Append(", RemoveDownloadLinksAfterExtraction = ").Append(RemoveDownloadLinksAfterExtraction)
            .Append(", RemoveFilesAfterExtraction = ").Append(RemoveFilesAfterExtraction);
        return true;
    }
}
