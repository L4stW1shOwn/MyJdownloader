namespace MyJdownloader.LinkCrawler;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/linkcrawler</c> namespace of one device (link crawler activity).</summary>
public sealed class LinkCrawlerApi
{
    private const string Base = "/linkcrawler/";

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="LinkCrawlerApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public LinkCrawlerApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Tells whether the link crawler of the device is running (<c>/linkcrawler/isCrawling</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> while the device link crawler is running.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsCrawlingAsync(CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(_channel.CallAsync(Base + "isCrawling", FacadeJsonContext.Default.NullableBoolean, [], cancellationToken));
}
