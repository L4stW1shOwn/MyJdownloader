namespace MyJdownloader;

/// <summary>Online availability of a link, as reported by the JDownloader API.</summary>
/// <remarks>Unrecognized values returned by a device, including the API's own <c>UNKNOWN</c>, are read as <see cref="Unknown"/>.</remarks>
public enum AvailableLinkState
{
    /// <summary>The state is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>The link is online (<c>ONLINE</c>).</summary>
    Online,

    /// <summary>The link is offline (<c>OFFLINE</c>).</summary>
    Offline,

    /// <summary>The state is temporarily unknown (<c>TEMP_UNKNOWN</c>).</summary>
    TempUnknown,
}
