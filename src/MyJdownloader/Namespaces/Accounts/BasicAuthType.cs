namespace MyJdownloader.Accounts;

/// <summary>The protocol a basic authentication entry applies to, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum BasicAuthType
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>FTP (<c>FTP</c>).</summary>
    Ftp,

    /// <summary>HTTP (<c>HTTP</c>).</summary>
    Http,
}
