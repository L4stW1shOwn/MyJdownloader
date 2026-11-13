namespace MyJdownloader.Captcha;

using System.Diagnostics.CodeAnalysis;

/// <summary>The scope of a captcha skip request, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum CaptchaSkipRequest
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>Skips this captcha only (<c>SINGLE</c>).</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Name mirrors the API value SINGLE.")]
    Single,

    /// <summary>Skips all captchas of the hoster (<c>BLOCK_HOSTER</c>).</summary>
    BlockHoster,

    /// <summary>Skips all captchas (<c>BLOCK_ALL_CAPTCHAS</c>).</summary>
    BlockAllCaptchas,

    /// <summary>Skips all captchas of the package (<c>BLOCK_PACKAGE</c>).</summary>
    BlockPackage,

    /// <summary>Requests a new captcha (<c>REFRESH</c>).</summary>
    Refresh,

    /// <summary>Stops the current action (<c>STOP_CURRENT_ACTION</c>).</summary>
    StopCurrentAction,

    /// <summary>Skips the captcha as timed out (<c>TIMEOUT</c>).</summary>
    Timeout,
}
