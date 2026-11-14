namespace MyJdownloader.Local;

/// <summary>Configuration options for the local JDownloader API client.</summary>
/// <remarks>
/// <para>
/// The local API is served by the JDownloader device itself: it has no authentication and no encryption, so anyone who can
/// reach the address can drive the device, and the traffic can be read and altered on the way unless the address uses
/// <c>https</c>.
/// </para>
/// <para>
/// The setters do not validate: the values are validated once, when the client is constructed, and an invalid value throws an
/// <see cref="ArgumentException"/> from that constructor. The client works on a copy, so later changes to this instance have no
/// effect on it.
/// </para>
/// </remarks>
public sealed class LocalJDownloaderOptions
{
    /// <summary>Gets or sets the base address of the local JDownloader API.</summary>
    /// <remarks>
    /// Validated at client construction: it must not be null and must be an absolute <c>http</c> or <c>https</c> URI without
    /// user info, query or fragment. The API paths are appended to this address. An <c>http</c> address is accepted only for a
    /// loopback host (<c>localhost</c> or a loopback IP address), unless <see cref="AllowUnencryptedRemoteHost"/> is set. The
    /// host is never resolved: no DNS lookup is made to validate it.
    /// </remarks>
    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:3128");

    /// <summary>Gets or sets a value indicating whether an <c>http</c> address with a non-loopback host is accepted.</summary>
    /// <remarks>
    /// When <see langword="true"/>, every call and answer travels in clear text to a remote host, with no authentication:
    /// anyone on the network path can read, alter or send commands to the device. Enable it only on a network you trust.
    /// </remarks>
    public bool AllowUnencryptedRemoteHost { get; set; }

    /// <summary>Gets or sets the maximum size, in bytes, of an HTTP response body.</summary>
    /// <remarks>
    /// Validated at client construction: it must be at least <c>1024</c>; no upper bound is enforced. A larger body is
    /// rejected. Each in-flight call can hold several times this value in memory, so raising it lets the device make the client
    /// allocate that much per call.
    /// </remarks>
    public int MaxResponseBodyLength { get; set; } = 1_048_576;
}
