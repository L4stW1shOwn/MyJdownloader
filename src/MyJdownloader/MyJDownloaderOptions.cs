namespace MyJdownloader;

/// <summary>Configuration options for the MyJDownloader client.</summary>
/// <remarks>
/// <para>
/// The setters do not validate: the options are usually filled in field by field (object initializer or configuration
/// binding), so an intermediate state is not meaningful. The values are validated once, when a
/// <see cref="MyJDownloaderClient"/> is constructed, and an invalid value throws an <see cref="ArgumentException"/> from that
/// constructor (with dependency injection, at the first resolution of the client). The client works on a copy, so later
/// changes to this instance have no effect on it.
/// </para>
/// </remarks>
public sealed class MyJDownloaderOptions
{
    /// <summary>Gets or sets the application identifier sent to the <c>/my/connect</c> endpoint.</summary>
    /// <remarks>
    /// Validated at client construction: it must not be null, empty or white space, must be at most 256 characters and must
    /// not contain control characters (Unicode category Cc). Leading and trailing spaces are kept as they are.
    /// </remarks>
    public string AppKey { get; set; } = "MyJdownloader.NET";

    /// <summary>Gets or sets the base address of the MyJDownloader relay server.</summary>
    /// <remarks>
    /// Validated at client construction: it must not be null and must be an absolute <c>https</c> URI without user info, query
    /// or fragment. HTTPS is required because device-call URL paths carry the session token, and the relay paths are appended
    /// to this address, so anything after the path would corrupt the request URLs.
    /// </remarks>
    public Uri BaseAddress { get; set; } = new("https://api.jdownloader.org");

    /// <summary>Gets or sets the <c>apiVer</c> value used for device calls.</summary>
    /// <remarks>
    /// Validated at client construction: it must be at least <c>1</c>; no upper bound is enforced. The value is written into the
    /// encrypted body of every device call.
    /// </remarks>
    public int ApiVersion { get; set; } = 1;
}
