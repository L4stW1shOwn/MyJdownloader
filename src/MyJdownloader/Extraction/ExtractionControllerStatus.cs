namespace MyJdownloader.Extraction;

/// <summary>The state of the extraction controller assigned to an archive, as reported by the device.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>.</remarks>
public enum ExtractionControllerStatus
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>The extraction is running (<c>RUNNING</c>).</summary>
    Running,

    /// <summary>The extraction is queued (<c>QUEUED</c>).</summary>
    Queued,

    /// <summary>No controller is assigned to the archive (<c>NA</c>).</summary>
    Na,
}
