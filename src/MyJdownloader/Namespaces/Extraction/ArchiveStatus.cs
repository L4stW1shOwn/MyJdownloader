namespace MyJdownloader.Extraction;

/// <summary>The extraction status of an archive on the device.</summary>
/// <remarks>Text fields come from the device and must be treated as untrusted.</remarks>
public sealed record ArchiveStatus
{
    /// <summary>Gets the archive identifier.</summary>
    public string? ArchiveId { get; init; }

    /// <summary>Gets the archive name.</summary>
    public string? ArchiveName { get; init; }

    /// <summary>Gets the extraction controller identifier, <c>-1</c> when no controller is assigned, or <see langword="null"/> when not returned.</summary>
    public long? ControllerId { get; init; }

    /// <summary>Gets the extraction controller status.</summary>
    public ExtractionControllerStatus ControllerStatus { get; init; }

    /// <summary>Gets the status of each part file keyed by file name, or <see langword="null"/> when not returned.</summary>
    public IReadOnlyDictionary<string, ArchiveFileStatus>? States { get; init; }

    /// <summary>Gets the archive type reported by the device (for example <c>RAR_MULTI</c>), or <see langword="null"/> when not returned.</summary>
    public string? Type { get; init; }
}
