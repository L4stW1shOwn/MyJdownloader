namespace MyJdownloader.DownloadController;

/// <summary>State of the device download controller, as reported by the JDownloader download watchdog.</summary>
/// <remarks>Unrecognized values returned by a device are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum DownloadState
{
    /// <summary>The state is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>No download is running (<c>IDLE</c>).</summary>
    Idle,

    /// <summary>Downloads are running (<c>RUNNING</c>).</summary>
    Running,

    /// <summary>Downloads are paused (<c>PAUSE</c>).</summary>
    Pause,

    /// <summary>Downloads are stopping (<c>STOPPING</c>).</summary>
    Stopping,

    /// <summary>Downloads are stopped (<c>STOPPED</c>).</summary>
    Stopped,
}
