namespace MyJdownloader;

/// <summary>Which URL a download link displays, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values returned by a device are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent. The values are to be confirmed by live tests.</remarks>
public enum UrlDisplayType
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>Custom URL (<c>CUSTOM</c>).</summary>
    Custom,

    /// <summary>Referrer URL (<c>REFERRER</c>).</summary>
    Referrer,

    /// <summary>Origin URL (<c>ORIGIN</c>).</summary>
    Origin,

    /// <summary>Container URL (<c>CONTAINER</c>).</summary>
    Container,

    /// <summary>Content URL (<c>CONTENT</c>).</summary>
    Content,
}
