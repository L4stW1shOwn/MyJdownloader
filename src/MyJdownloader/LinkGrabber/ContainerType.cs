namespace MyJdownloader.LinkGrabber;

/// <summary>The format of a container file that a device can import, as used by the JDownloader API.</summary>
/// <remarks><see cref="Unknown"/> and undefined values cannot be sent.</remarks>
public enum ContainerType
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>DLC container (<c>dlc</c>).</summary>
    Dlc,

    /// <summary>CryptLoad CCF container (<c>ccf</c>).</summary>
    Ccf,

    /// <summary>RSDF container (<c>rsdf</c>).</summary>
    Rsdf,

    /// <summary>NZB Usenet index (<c>nzb</c>).</summary>
    Nzb,

    /// <summary>JDownloader folder watch crawljob (<c>crawljob</c>).</summary>
    CrawlJob,

    /// <summary>SFT container (<c>sft</c>).</summary>
    Sft,

    /// <summary>SFDL container (<c>sfdl</c>).</summary>
    Sfdl,

    /// <summary>Metalink 3 document (<c>metalink</c>).</summary>
    Metalink,

    /// <summary>Metalink 4 document (<c>meta4</c>).</summary>
    Meta4,
}
