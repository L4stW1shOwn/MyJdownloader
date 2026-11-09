namespace MyJdownloader.Log;

/// <summary>One log folder of a device, as listed by <c>/log/getAvailableLogs</c> and selected for <c>/log/sendLogFile</c>.</summary>
/// <remarks>All content returned by the device is untrusted. Dates are epoch milliseconds on the wire; a value of zero or less is read as <see langword="null"/>.</remarks>
public sealed record LogFolder
{
    /// <summary>Gets the creation date of the folder (the start of the session), or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>Gets a value indicating whether this folder is the log of the current session of the device.</summary>
    public bool Current { get; init; }

    /// <summary>Gets the last modification date of the folder, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? LastModified { get; init; }
}
