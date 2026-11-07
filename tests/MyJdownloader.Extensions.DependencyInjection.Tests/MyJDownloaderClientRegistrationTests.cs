namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[TestFixture]
public sealed class MyJDownloaderClientRegistrationTests
{
    private const string Name = MyJDownloaderServiceCollectionExtensions.HttpClientName;

    [Test]
    public void AddMyJDownloader_Called_RegistersClientSingleton()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<MyJDownloaderClient>();
        var second = provider.GetRequiredService<MyJDownloaderClient>();

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void AddMyJDownloader_CalledTwice_RegistersOneClientAndAppliesBoth()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(o => o.AppKey = "A");
        services.AddMyJDownloader(o => o.ApiVersion = 2);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MyJDownloaderOptions>>().Value;

        Assert.That(services.Count(d => d.ServiceType == typeof(MyJDownloaderClient)), Is.EqualTo(1));
        Assert.That(options.AppKey, Is.EqualTo("A"));
        Assert.That(options.ApiVersion, Is.EqualTo(2));
    }

    [Test]
    public void AddMyJDownloader_ResolvedClient_UsesNamedClient()
    {
        using var capture = new CapturingHandler();
        var services = new ServiceCollection();
        services.AddMyJDownloader(o =>
        {
            o.BaseAddress = new Uri("https://relay.test/");
            o.AppKey = "MyApp";
        }).AddHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<MyJDownloaderClient>();

        Assert.That(
            async () => await client.ConnectAsync("user@example.com", "pw"),
            Throws.InstanceOf<MyJdApiException>());

        Assert.That(capture.Requests, Has.Count.EqualTo(1));
        Assert.That(capture.Requests[0].Host, Is.EqualTo("relay.test"));
        Assert.That(capture.Requests[0].Query, Does.Contain("appkey=MyApp"));
    }

    [Test]
    public void AddMyJDownloader_Called_DisablesAutoRedirect()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
        Assert.That(((SocketsHttpHandler)primary).PooledConnectionLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    [TestCase(true)]
    // The defaultsBefore=false case relies on the framework applying ConfigureHttpClientDefaults before named-client actions regardless of registration order.
    [TestCase(false)]
    public void AddMyJDownloader_DefaultsEnableRedirect_StillDisabled(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.UseSocketsHttpHandler((h, _) => h.AllowAutoRedirect = true));
        }

        services.AddMyJDownloader(_ => { });
        if (!defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.UseSocketsHttpHandler((h, _) => h.AllowAutoRedirect = true));
        }

        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
    }

    [TestCase(true)]
    // The defaultsBefore=false case relies on the framework applying ConfigureHttpClientDefaults before named-client actions regardless of registration order.
    [TestCase(false)]
    public void AddMyJDownloader_DefaultsSetNonSocketsPrimaryHandler_ReplacedWithRedirectDisabled(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true }));
        }

        services.AddMyJDownloader(_ => { });
        if (!defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true }));
        }

        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddMyJDownloader_WithLogging_HasNoLoggingHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(HandlerTypeNames(provider), Has.None.Contains("Logging"));
    }

    [Test]
    public void AddMyJDownloader_CalledTwice_KeepsNoLoggers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMyJDownloader(_ => { }).AddDefaultLogger();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(HandlerTypeNames(provider), Has.None.Contains("Logging"));
    }

    [TestCase(true)]
    [TestCase(false)]
    // The defaultsBefore=false case relies on the framework applying ConfigureHttpClientDefaults before named-client actions regardless of registration order.
    public void AddMyJDownloader_DefaultsAddLogger_HasNoLoggingHandler(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.AddDefaultLogger());
        }

        services.AddMyJDownloader(_ => { });
        if (!defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.AddDefaultLogger());
        }

        using var provider = services.BuildServiceProvider();

        var names = HandlerTypeNames(provider);

        Assert.That(names, Has.None.EqualTo("LoggingHttpMessageHandler"));
        Assert.That(names, Has.None.EqualTo("LoggingScopeHttpMessageHandler"));
    }

    [Test]
    public void AddMyJDownloader_TimeProviderRegistered_UsesIt()
    {
        var now = new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
        using var capture = new CapturingHandler();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddMyJDownloader(_ => { }).AddHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<MyJDownloaderClient>();

        Assert.That(
            async () => await client.ConnectAsync("user@example.com", "pw"),
            Throws.InstanceOf<MyJdApiException>());

        Assert.That(capture.Requests, Has.Count.EqualTo(1));
        Assert.That(capture.Requests[0].Query, Does.Contain($"rid={now.ToUnixTimeMilliseconds()}"));
    }

    [Test]
    public void AddMyJDownloader_NoTimeProvider_ResolvesClient()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<MyJDownloaderClient>(), Is.Not.Null);
    }

    [Test]
    public void AddMyJDownloader_InvalidOptions_ThrowsOnResolution()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(o => o.BaseAddress = new Uri("http://relay.test/"));
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<MyJDownloaderClient>, Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddMyJDownloader_ClientAlreadyRegistered_KeepsExisting()
    {
        using var http = new HttpClient();
        var existing = new MyJDownloaderClient(http);
        var services = new ServiceCollection();
        services.AddSingleton(existing);
        services.AddMyJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<MyJDownloaderClient>(), Is.SameAs(existing));
    }

    private static HttpMessageHandler PrimaryHandler(IServiceProvider provider)
    {
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(Name);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler;
        }

        return handler!;
    }

    private static List<string> HandlerTypeNames(IServiceProvider provider)
    {
        var names = new List<string>();
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(Name);
        while (handler is not null)
        {
            names.Add(handler.GetType().Name);
            handler = (handler as DelegatingHandler)?.InnerHandler;
        }

        return names;
    }
}
