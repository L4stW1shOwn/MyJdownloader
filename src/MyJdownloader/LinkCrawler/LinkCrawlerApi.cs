namespace MyJdownloader.LinkCrawler;

using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/linkcrawler</c> namespace of one device (link crawler activity).</summary>
public sealed class LinkCrawlerApi
{
    private const string Base = "/linkcrawler/";

    private readonly IDeviceChannel _channel;

    internal LinkCrawlerApi(IDeviceChannel channel)
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
