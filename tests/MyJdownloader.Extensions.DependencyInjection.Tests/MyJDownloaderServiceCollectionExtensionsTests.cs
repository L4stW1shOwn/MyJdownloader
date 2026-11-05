namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

[TestFixture]
public sealed class MyJDownloaderServiceCollectionExtensionsTests
{
    [Test]
    public void AddMyJDownloader_NullServices_ThrowsArgumentNullException()
        => Assert.That(() => ((IServiceCollection)null!).AddMyJDownloader(_ => { }), Throws.ArgumentNullException);

    [Test]
    public void AddMyJDownloader_NullConfigure_ThrowsArgumentNullException()
        => Assert.That(() => new ServiceCollection().AddMyJDownloader(null!), Throws.ArgumentNullException);

    [Test]
    public void AddMyJDownloader_Configure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(o => o.AppKey = "MyApp");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MyJDownloaderOptions>>();

        Assert.That(options.Value.AppKey, Is.EqualTo("MyApp"));
    }

    [Test]
    public void AddMyJDownloader_Called_ReturnsBuilderForNamedClient()
    {
        var builder = new ServiceCollection().AddMyJDownloader(_ => { });

        Assert.That(builder.Name, Is.EqualTo(MyJDownloaderServiceCollectionExtensions.HttpClientName));
    }

    [Test]
    public void AddMyJDownloader_Called_DisablesHandlerRotation()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

        Assert.That(
            monitor.Get(MyJDownloaderServiceCollectionExtensions.HttpClientName).HandlerLifetime,
            Is.EqualTo(Timeout.InfiniteTimeSpan));
    }

    [Test]
    public void AddMyJDownloader_Called_UsesSocketsHandlerWithTwoMinutePooledLifetime()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler(MyJDownloaderServiceCollectionExtensions.HttpClientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler;
        }

        Assert.That(handler, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)handler!).PooledConnectionLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }
}
