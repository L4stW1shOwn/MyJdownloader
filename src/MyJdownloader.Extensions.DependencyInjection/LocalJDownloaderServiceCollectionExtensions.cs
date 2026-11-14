namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using MyJdownloader.Local;

/// <summary>Dependency injection extensions for the local JDownloader API client.</summary>
public static class LocalJDownloaderServiceCollectionExtensions
{
    /// <summary>The name of the <see cref="HttpClient"/> registered for the local JDownloader API. Reserved for this library.</summary>
    public const string HttpClientName = "MyJdownloader.Local";

    /// <summary>Registers the local API options, its named <see cref="HttpClient"/> and the <see cref="LocalJDownloaderClient"/> singleton.</summary>
    /// <remarks>
    /// <para>
    /// Registered services: the <see cref="LocalJDownloaderOptions"/> binding; the named client
    /// <see cref="HttpClientName"/> (two-minute <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>,
    /// redirects disabled, no default HTTP loggers, infinite handler lifetime); and a
    /// <see cref="LocalJDownloaderClient"/> singleton added with <c>TryAdd</c>. The device is not registered: use
    /// <see cref="LocalJDownloaderClient.Device"/>, because the relay client produces devices too and a registration
    /// would be ambiguous. This method can be combined with <c>AddMyJDownloader</c> in the same container.
    /// </para>
    /// <para>
    /// The client is a named client rather than a typed client: a typed client captured by a singleton no longer
    /// follows handler rotation. Connection recycling is delegated to
    /// <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>. See docs/adr/ADR-003-injected-httpclient-and-named-di-client.md
    /// and docs/adr/ADR-012-local-api-transport.md.
    /// </para>
    /// <para>
    /// Security notes. The local API has no authentication and no encryption. A followed redirect would send the
    /// cleartext request body, passwords included, to another host before the client rejects the answer. Redirects are
    /// therefore disabled again after every handler configuration of this client, whatever the registration order: a
    /// <see cref="SocketsHttpHandler"/> or <see cref="HttpClientHandler"/> primary handler always ends with
    /// <c>AllowAutoRedirect</c> set to <see langword="false"/>, even when it was replaced or reconfigured through the
    /// returned builder. A primary handler of another type is used as it is and must not follow redirects; so must a
    /// handler changed by a later <c>PostConfigure</c> of the client factory options or by an
    /// <c>IHttpMessageHandlerBuilderFilter</c>. A primary handler that is not a
    /// <see cref="SocketsHttpHandler"/> and was set by <c>ConfigureHttpClientDefaults</c> is replaced.
    /// Calling <c>AddLogger</c>, <c>AddDefaultLogger</c> or extended HTTP logging on the returned builder re-enables the
    /// logging of the requests to the device.
    /// </para>
    /// <para>
    /// A <see cref="LocalJDownloaderClient"/> registered before this call is kept and bypasses these protections. So does a
    /// typed client (<c>AddHttpClient&lt;LocalJDownloaderClient&gt;</c>), whatever the call order.
    /// <see cref="HttpClientName"/> is reserved for the library. Invalid options throw <see cref="ArgumentException"/>
    /// at the first resolution of the client.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configure">The delegate that configures <see cref="LocalJDownloaderOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> of the named client, for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddLocalJDownloader(this IServiceCollection services, Action<LocalJDownloaderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        var builder = services.AddHttpClient(HttpClientName)
            .UseSocketsHttpHandler((handler, _) =>
            {
                handler.PooledConnectionLifetime = MyJDownloaderServiceCollectionExtensions.PooledConnectionLifetime;
                handler.AllowAutoRedirect = false;
            })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
            .RemoveAllLoggers();

        // Runs after every Configure action of the client, whatever the registration order, so a later handler
        // configuration cannot re-enable redirects.
        services.PostConfigure<HttpClientFactoryOptions>(HttpClientName, options => options.HttpMessageHandlerBuilderActions.Add(DisableAutoRedirect));

        services.TryAddSingleton(sp => new LocalJDownloaderClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            sp.GetRequiredService<IOptions<LocalJDownloaderOptions>>().Value));

        return builder;
    }

    private static void DisableAutoRedirect(HttpMessageHandlerBuilder builder)
    {
        if (builder.PrimaryHandler is SocketsHttpHandler socketsHandler)
        {
            socketsHandler.AllowAutoRedirect = false;
        }
        else if (builder.PrimaryHandler is HttpClientHandler clientHandler)
        {
            clientHandler.AllowAutoRedirect = false;
        }
    }
}
