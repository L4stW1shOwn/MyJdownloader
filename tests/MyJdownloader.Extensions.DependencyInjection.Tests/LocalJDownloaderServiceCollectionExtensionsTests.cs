namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using MyJdownloader.Local;

[TestFixture]
public sealed class LocalJDownloaderServiceCollectionExtensionsTests
{
    [Test]
    public void AddLocalJDownloader_NullServices_ThrowsArgumentNullException()
        => Assert.That(() => ((IServiceCollection)null!).AddLocalJDownloader(_ => { }), Throws.ArgumentNullException);

    [Test]
    public void AddLocalJDownloader_NullConfigure_ThrowsArgumentNullException()
        => Assert.That(() => new ServiceCollection().AddLocalJDownloader(null!), Throws.ArgumentNullException);

    [Test]
    public void AddLocalJDownloader_Configure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(o => o.MaxResponseBodyLength = 2048);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LocalJDownloaderOptions>>();

        Assert.That(options.Value.MaxResponseBodyLength, Is.EqualTo(2048));
    }

    [Test]
    public void AddLocalJDownloader_Called_ReturnsBuilderForNamedClient()
    {
        var builder = new ServiceCollection().AddLocalJDownloader(_ => { });

        Assert.That(builder.Name, Is.EqualTo(LocalJDownloaderServiceCollectionExtensions.HttpClientName));
    }

    [Test]
    public void HttpClientName_Local_DiffersFromRelayName()
        => Assert.That(
            LocalJDownloaderServiceCollectionExtensions.HttpClientName,
            Is.Not.EqualTo(MyJDownloaderServiceCollectionExtensions.HttpClientName));

    [Test]
    public void AddLocalJDownloader_Called_DisablesHandlerRotation()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

        Assert.That(
            monitor.Get(LocalJDownloaderServiceCollectionExtensions.HttpClientName).HandlerLifetime,
            Is.EqualTo(Timeout.InfiniteTimeSpan));
    }

    [Test]
    public void AddLocalJDownloader_Called_UsesSocketsHandlerWithTwoMinutePooledLifetime()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler(LocalJDownloaderServiceCollectionExtensions.HttpClientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler;
        }

        Assert.That(handler, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)handler!).PooledConnectionLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }
}
