namespace MyJdownloader.Local;

/// <summary>Client for the local API served by a JDownloader device itself, without the MyJDownloader account or relay.</summary>
/// <remarks>
/// <para>
/// The local API has no authentication and no encryption: there is no account, session, connect or disconnect, and anyone who
/// can reach the address can drive the device. A cleartext <c>http</c> address is accepted only for a loopback host unless
/// <see cref="LocalJDownloaderOptions.AllowUnencryptedRemoteHost"/> is set. Calls go to the configured address only;
/// <see cref="HttpClient.BaseAddress"/> is ignored.
/// </para>
/// <para>
/// <see cref="Device"/> exposes the same typed facades as a device obtained from a <c>MyJDownloaderClient</c>, and the calls
/// throw the same exceptions. Configure the handler of the <see cref="HttpClient"/> with <c>AllowAutoRedirect = false</c>: a
/// redirect to another origin is rejected after the fact, but the request has then already been sent. The
/// <see cref="HttpClient.Timeout"/> bounds each call, body included.
/// </para>
/// <para>The client keeps no state and is safe for concurrent use as long as the <see cref="HttpClient"/> is.</para>
/// </remarks>
public sealed class LocalJDownloaderClient
{
    /// <summary>Initializes a new instance of the <see cref="LocalJDownloaderClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client used for every call; its lifetime stays with the caller.</param>
    /// <param name="options">The options, validated here; <see langword="null"/> uses the defaults (<c>http://127.0.0.1:3128</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> is not valid (the parameter name is <c>options</c>).</exception>
    public LocalJDownloaderClient(HttpClient httpClient, LocalJDownloaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        var endpoint = LocalEndpoint.Create(options ?? new LocalJDownloaderOptions());
        Device = new JDownloaderDevice("local", new LocalDeviceChannel(httpClient, endpoint));
    }

    /// <summary>Gets the device served by the local API, with its typed facades; its <see cref="JDownloaderDevice.Id"/> is <c>local</c>.</summary>
    public JDownloaderDevice Device { get; }
}
