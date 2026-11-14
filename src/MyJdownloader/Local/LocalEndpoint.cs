namespace MyJdownloader.Local;

using System.Net;

/// <summary>Immutable, validated snapshot of a <see cref="LocalJDownloaderOptions"/>.</summary>
internal sealed class LocalEndpoint
{
    private LocalEndpoint(Uri baseAddress, int maxResponseBodyLength)
    {
        BaseAddress = baseAddress;
        MaxResponseBodyLength = maxResponseBodyLength;
    }

    internal Uri BaseAddress { get; }

    internal int MaxResponseBodyLength { get; }

    internal static LocalEndpoint Create(LocalJDownloaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var address = options.BaseAddress;
        if (address is null
            || !address.IsAbsoluteUri
            || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
            || address.UserInfo.Length > 0
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

    internal Uri BuildUri(string path) => new(BaseAddress.AbsoluteUri.TrimEnd('/') + path);

    internal bool IsSameOrigin(Uri? uri) =>
        uri is { IsAbsoluteUri: true }
        && string.Equals(uri.Scheme, BaseAddress.Scheme, StringComparison.Ordinal)
        && string.Equals(uri.Host, BaseAddress.Host, StringComparison.OrdinalIgnoreCase)
        && uri.Port == BaseAddress.Port;

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
