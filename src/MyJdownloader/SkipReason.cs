namespace MyJdownloader;

/// <summary>Why a download link is skipped, as reported by the JDownloader API.</summary>
/// <remarks>Unrecognized values returned by a device are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum SkipReason
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>No connection available (<c>CONNECTION_UNAVAILABLE</c>).</summary>
    ConnectionUnavailable,

    /// <summary>Too many retries (<c>TOO_MANY_RETRIES</c>).</summary>
    TooManyRetries,

    /// <summary>A captcha is required (<c>CAPTCHA</c>).</summary>
    Captcha,

    /// <summary>Skipped manually (<c>MANUAL</c>).</summary>
    Manual,

    /// <summary>The disk is full (<c>DISK_FULL</c>).</summary>
    DiskFull,

    /// <summary>No account available (<c>NO_ACCOUNT</c>).</summary>
    NoAccount,

    /// <summary>Invalid destination (<c>INVALID_DESTINATION</c>).</summary>
    InvalidDestination,

    /// <summary>The file already exists (<c>FILE_EXISTS</c>).</summary>
    FileExists,

    /// <summary>An update restart is required (<c>UPDATE_RESTART_REQUIRED</c>).</summary>
    UpdateRestartRequired,

    /// <summary>FFmpeg is missing (<c>FFMPEG_MISSING</c>).</summary>
    FfmpegMissing,

    /// <summary>FFprobe is missing (<c>FFPROBE_MISSING</c>).</summary>
    FfprobeMissing,
}
