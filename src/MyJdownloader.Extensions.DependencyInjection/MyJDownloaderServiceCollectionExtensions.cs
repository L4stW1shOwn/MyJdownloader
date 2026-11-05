namespace Microsoft.Extensions.DependencyInjection;

using MyJdownloader;

/// <summary>Dependency injection extensions for the MyJDownloader client.</summary>
public static class MyJDownloaderServiceCollectionExtensions
{
    /// <summary>The name of the <see cref="HttpClient"/> registered for MyJDownloader.</summary>
    public const string HttpClientName = "MyJdownloader";

    /// <summary>Registers the MyJDownloader options and its named <see cref="HttpClient"/>.</summary>
    /// <remarks>
    /// The client is registered as a named client rather than a typed client: a typed client captured by a
    /// singleton no longer follows handler rotation. Here the handler lifetime is infinite and connection
    /// recycling is delegated to <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>
    /// (two minutes). See docs/adr/ADR-003-injected-httpclient-and-named-di-client.md.
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configure">The delegate that configures <see cref="MyJDownloaderOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> of the named client, for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddMyJDownloader(this IServiceCollection services, Action<MyJDownloaderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services.AddHttpClient(HttpClientName)
            .UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(2))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }
}
