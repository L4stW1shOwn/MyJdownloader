namespace MyJdownloader;

/// <summary>Selects which links a cleanup removes, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum CleanupAction
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>All links (<c>DELETE_ALL</c>).</summary>
    DeleteAll,

    /// <summary>Disabled links (<c>DELETE_DISABLED</c>).</summary>
    DeleteDisabled,

    /// <summary>Failed links (<c>DELETE_FAILED</c>).</summary>
    DeleteFailed,

    /// <summary>Finished links (<c>DELETE_FINISHED</c>).</summary>
    DeleteFinished,

    /// <summary>Offline links (<c>DELETE_OFFLINE</c>).</summary>
    DeleteOffline,

    /// <summary>Duplicate links (<c>DELETE_DUPE</c>).</summary>
    DeleteDupe,

    /// <summary>Links matching the mode-specific rule (<c>DELETE_MODE</c>).</summary>
    DeleteMode,
}
