namespace MyJdownloader.Local;

using System.Net;

/// <summary>Immutable, validated snapshot of a <see cref="LocalJDownloaderOptions"/>.</summary>
internal sealed class LocalEndpoint
{
    /// <summary>Initializes a new instance of the <see cref="LocalEndpoint"/> class from values already validated by <see cref="Create"/>.</summary>
    /// <param name="baseAddress">The validated base address of the local API.</param>
    /// <param name="maxResponseBodyLength">The validated maximum size of a response body, in bytes.</param>
    private LocalEndpoint(Uri baseAddress, int maxResponseBodyLength)
    {
        BaseAddress = baseAddress;
        MaxResponseBodyLength = maxResponseBodyLength;
    }

    /// <summary>Gets the base address of the local API: absolute, <c>http</c> or <c>https</c>, without user info, query or fragment.</summary>
    internal Uri BaseAddress { get; }

    /// <summary>Gets the maximum size of a response body, in bytes (at least 1 024).</summary>
    internal int MaxResponseBodyLength { get; }

    /// <summary>Validates the options and returns their immutable snapshot; later changes to <paramref name="options"/> have no effect on it.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validated endpoint.</returns>
    /// <remarks>
    /// Plain <c>http</c> is accepted only for a loopback host unless <see cref="LocalJDownloaderOptions.AllowUnencryptedRemoteHost"/> is set.
    /// The check uses no name resolution, and no message carries the address.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The base address is missing, relative, neither <c>http</c> nor <c>https</c>, or has user info, a query or a fragment;
    /// or it uses <c>http</c> with a host that is not loopback without the opt-in;
    /// or <see cref="LocalJDownloaderOptions.MaxResponseBodyLength"/> is below 1 024. The parameter name is <c>options</c>.
    /// </exception>
    internal static LocalEndpoint Create(LocalJDownloaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var address = options.BaseAddress;
        if (address is null
            || !address.IsAbsoluteUri
            || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
            || address.UserInfo.Length > 0
            || address.GetLeftPart(UriPartial.Authority).Contains('@', StringComparison.Ordinal)
            || address.Query.Length > 0
            || address.Fragment.Length > 0)
        {
            throw new ArgumentException(
                "BaseAddress must be an absolute http or https URI without user info, query or fragment.",
                nameof(options));
        }

        if (address.Scheme == Uri.UriSchemeHttp && !options.AllowUnencryptedRemoteHost && !IsLoopback(address))
        {
            throw new ArgumentException(
                "BaseAddress uses http with a host that is not a loopback address: use https or set AllowUnencryptedRemoteHost.",
                nameof(options));
        }

        if (options.MaxResponseBodyLength < 1_024)
        {
            throw new ArgumentException("MaxResponseBodyLength must be greater than or equal to 1024.", nameof(options));
        }

        return new LocalEndpoint(address, options.MaxResponseBodyLength);
    }

    /// <summary>Builds the absolute URI of a device action under the base address, keeping a path prefix of the base address.</summary>
    /// <param name="path">The action path, already validated by <see cref="Transport.DeviceCallGuard.ValidatePath"/> (it starts with a slash).</param>
    /// <returns>The absolute request URI.</returns>
    internal Uri BuildUri(string path) => new(BaseAddress.AbsoluteUri.TrimEnd('/') + path);

    /// <summary>Tells whether a URI has the scheme, host and port of the base address.</summary>
    /// <param name="uri">The URI to compare, typically the final request URI of a response; <see langword="null"/> is never the same origin.</param>
    /// <returns><see langword="true"/> when the scheme, the host (case-insensitive) and the port match.</returns>
    /// <remarks>An IPv6 scope identifier is not part of the comparison.</remarks>
    internal bool IsSameOrigin(Uri? uri) =>
        uri is { IsAbsoluteUri: true }
        && string.Equals(uri.Scheme, BaseAddress.Scheme, StringComparison.Ordinal)
        && string.Equals(uri.Host, BaseAddress.Host, StringComparison.OrdinalIgnoreCase)
        && uri.Port == BaseAddress.Port;

    /// <summary>Tells, without name resolution, whether the host of an address is a loopback host.</summary>
    /// <param name="address">The absolute address to inspect.</param>
    /// <returns>
    /// <see langword="true"/> for the exact host name <c>localhost</c> (case-insensitive) and for an IPv4 or IPv6 literal that is a loopback address
    /// (<c>127.0.0.0/8</c>, <c>::1</c> and their IPv4-mapped forms); <see langword="false"/> for any other host, look-alike names included.
    /// </returns>
    private static bool IsLoopback(Uri address)
    {
        if (address.HostNameType == UriHostNameType.Dns)
        {
            return string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        }

        return address.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            && IPAddress.TryParse(address.IdnHost, out var ip)
            && IPAddress.IsLoopback(ip);
    }
}
