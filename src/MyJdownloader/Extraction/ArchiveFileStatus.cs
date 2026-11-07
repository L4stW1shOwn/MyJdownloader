namespace MyJdownloader.Extraction;

/// <summary>The availability of one part file of an archive, as reported by the device.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>.</remarks>
public enum ArchiveFileStatus
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>The part file is completely downloaded (<c>COMPLETE</c>).</summary>
    Complete,

    /// <summary>The part file is not completely downloaded (<c>INCOMPLETE</c>).</summary>
    Incomplete,

    /// <summary>The part file is missing (<c>MISSING</c>).</summary>
    Missing,
}
