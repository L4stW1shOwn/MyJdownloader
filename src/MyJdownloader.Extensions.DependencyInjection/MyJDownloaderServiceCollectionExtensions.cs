namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MyJdownloader;

/// <summary>Dependency injection extensions for the MyJDownloader client.</summary>
public static class MyJDownloaderServiceCollectionExtensions
{
    /// <summary>The name of the <see cref="HttpClient"/> registered for MyJDownloader. Reserved for this library.</summary>
    public const string HttpClientName = "MyJdownloader";

    /// <summary>Registers the MyJDownloader options, its named <see cref="HttpClient"/> and the <see cref="MyJDownloaderClient"/> singleton.</summary>
    /// <remarks>
    /// <para>
    /// Registered services: the <see cref="MyJDownloaderOptions"/> binding; the named client
    /// <see cref="HttpClientName"/> (two-minute <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>,
    /// redirects disabled, no default HTTP loggers, infinite handler lifetime); and a
    /// <see cref="MyJDownloaderClient"/> singleton added with <c>TryAdd</c>, using the registered
    /// <see cref="TimeProvider"/> when there is one.
    /// </para>
    /// <para>
    /// The client is a named client rather than a typed client: a typed client captured by a singleton no longer
    /// follows handler rotation. Connection recycling is delegated to
    /// <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>. See docs/adr/ADR-003-injected-httpclient-and-named-di-client.md.
    /// </para>
    /// <para>
    /// Security notes. Replacing or reconfiguring the primary handler through the returned builder must keep
    /// <see cref="SocketsHttpHandler.AllowAutoRedirect"/> set to <see langword="false"/>. A primary handler that is not a
    /// <see cref="SocketsHttpHandler"/> and was set by <c>ConfigureHttpClientDefaults</c> is replaced.
    /// Calling <c>AddLogger</c>, <c>AddDefaultLogger</c> or extended HTTP logging on the returned builder re-enables the
    /// logging of device-call URL paths, which carry the session token and the device id. The
    /// <c>System.Net.Http</c> EventSource (<c>pathAndQuery</c>) and the <c>Activity</c> tag <c>url.full</c> redact only
    /// the query, not the path: exclude this client's traces or add a redacting enricher.
    /// </para>
    /// <para>
    /// A <see cref="MyJDownloaderClient"/> registered before this call is kept and bypasses these protections. So does a
    /// typed client (<c>AddHttpClient&lt;MyJDownloaderClient&gt;</c>): its <see cref="HttpClient"/> does not use the
    /// <see cref="HttpClientName"/> configuration (redirects, loggers, pooled connection lifetime).
    /// <see cref="HttpClientName"/> is reserved for the library. Invalid options throw <see cref="ArgumentException"/>
    /// at the first resolution of the client.
    /// </para>
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
        var builder = services.AddHttpClient(HttpClientName)
            .UseSocketsHttpHandler((handler, _) =>
            {
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(2);
                handler.AllowAutoRedirect = false;
            })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
            .RemoveAllLoggers();

        services.TryAddSingleton(sp => new MyJDownloaderClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            sp.GetRequiredService<IOptions<MyJDownloaderOptions>>().Value,
            sp.GetService<TimeProvider>()));

        return builder;
    }
}
