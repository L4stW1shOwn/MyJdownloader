namespace MyJdownloader;

/// <summary>Selects what a cleanup does with the links and their files, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum CleanupMode
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>Removes the links and deletes their files (<c>REMOVE_LINKS_AND_DELETE_FILES</c>).</summary>
    /// <remarks>Destructive: this deletes files on the device.</remarks>
    RemoveLinksAndDeleteFiles,

    /// <summary>Removes the links and moves their files to the recycle bin (<c>REMOVE_LINKS_AND_RECYCLE_FILES</c>).</summary>
    RemoveLinksAndRecycleFiles,

    /// <summary>Removes the links and keeps their files (<c>REMOVE_LINKS_ONLY</c>).</summary>
    RemoveLinksOnly,
}
