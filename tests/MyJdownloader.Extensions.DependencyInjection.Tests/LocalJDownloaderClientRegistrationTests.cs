namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using MyJdownloader.Errors;
using MyJdownloader.Local;

[TestFixture]
public sealed class LocalJDownloaderClientRegistrationTests
{
    private const string Name = LocalJDownloaderServiceCollectionExtensions.HttpClientName;

    [Test]
    public void AddLocalJDownloader_Called_RegistersClientSingleton()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<LocalJDownloaderClient>();
        var second = provider.GetRequiredService<LocalJDownloaderClient>();

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void AddLocalJDownloader_CalledTwice_RegistersOneClientAndAppliesBoth()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(o => o.MaxResponseBodyLength = 2048);
        services.AddLocalJDownloader(o => o.AllowUnencryptedRemoteHost = true);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LocalJDownloaderOptions>>().Value;

        Assert.That(services.Count(d => d.ServiceType == typeof(LocalJDownloaderClient)), Is.EqualTo(1));
        Assert.That(options.MaxResponseBodyLength, Is.EqualTo(2048));
        Assert.That(options.AllowUnencryptedRemoteHost, Is.True);
    }

    [Test]
    public void AddLocalJDownloader_ResolvedClient_UsesNamedClient()
    {
        using var capture = new CapturingHandler();
        var services = new ServiceCollection();
        services.AddLocalJDownloader(o => o.BaseAddress = new Uri("http://127.0.0.1:3128"))
            .AddHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LocalJDownloaderClient>();

        Assert.That(
            async () => await client.Device.System.GetSystemInfoAsync(),
            Throws.TypeOf<MyJdProtocolException>());

        Assert.That(capture.Requests, Has.Count.EqualTo(1));
        Assert.That(capture.Requests[0].Host, Is.EqualTo("127.0.0.1"));
        Assert.That(capture.Requests[0].Port, Is.EqualTo(3128));
    }

    [Test]
    public void AddLocalJDownloader_Called_DisablesAutoRedirect()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
        Assert.That(((SocketsHttpHandler)primary).PooledConnectionLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    [Test]
    public void AddLocalJDownloader_CallerConfiguresSocketsHandlerOnBuilder_KeepsRedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { })
            .UseSocketsHttpHandler((handler, _) => handler.ConnectTimeout = TimeSpan.FromSeconds(5));
        using var provider = services.BuildServiceProvider();

        var primary = (SocketsHttpHandler)PrimaryHandler(provider);

        Assert.That(primary.ConnectTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(primary.AllowAutoRedirect, Is.False);
        Assert.That(primary.PooledConnectionLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    [Test]
    public void AddLocalJDownloader_CallerEnablesRedirectOnBuilder_StillDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { })
            .UseSocketsHttpHandler((handler, _) => handler.AllowAutoRedirect = true);
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddLocalJDownloader_CallerReplacesPrimaryHandlerOnBuilder_RedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = true });
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddLocalJDownloader_CallerReplacesPrimaryHandlerThenConfiguresSocketsHandler_RedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
            .UseSocketsHttpHandler((handler, _) => handler.ConnectTimeout = TimeSpan.FromSeconds(5));
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<SocketsHttpHandler>());
        Assert.That(((SocketsHttpHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddLocalJDownloader_ConfigureAllSetsRedirectingHandlerAfter_RedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        services.ConfigureAll<HttpClientFactoryOptions>(o =>
            o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = new HttpClientHandler { AllowAutoRedirect = true }));
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<HttpClientHandler>());
        Assert.That(((HttpClientHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddLocalJDownloader_ReservedNameRegisteredAfterWithRedirectingHandler_RedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        services.AddHttpClient(Name)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });
        using var provider = services.BuildServiceProvider();

        var primary = PrimaryHandler(provider);

        Assert.That(primary, Is.TypeOf<HttpClientHandler>());
        Assert.That(((HttpClientHandler)primary).AllowAutoRedirect, Is.False);
    }

    [Test]
    public void AddLocalJDownloader_CallerSetsCustomPrimaryHandler_ClientUsesIt()
    {
        using var capture = new CapturingHandler();
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { }).ConfigurePrimaryHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LocalJDownloaderClient>();

        Assert.That(
            async () => await client.Device.System.GetSystemInfoAsync(),
            Throws.TypeOf<MyJdProtocolException>());

        Assert.That(capture.Requests, Has.Count.EqualTo(1));
    }

    [TestCase(true)]
    // The defaultsBefore=false case relies on the framework applying ConfigureHttpClientDefaults before named-client actions regardless of registration order.
    [TestCase(false)]
    public void AddLocalJDownloader_DefaultsEnableRedirect_StillDisabled(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.UseSocketsHttpHandler((h, _) => h.AllowAutoRedirect = true));
        }

        services.AddLocalJDownloader(_ => { });
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
    public void AddLocalJDownloader_DefaultsSetNonSocketsPrimaryHandler_ReplacedWithRedirectDisabled(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true }));
        }

        services.AddLocalJDownloader(_ => { });
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
    public void AddLocalJDownloader_WithLogging_HasNoLoggingHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(HandlerTypeNames(provider), Has.None.Contains("Logging"));
    }

    [Test]
    public void AddLocalJDownloader_CalledTwice_KeepsNoLoggers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalJDownloader(_ => { }).AddDefaultLogger();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(HandlerTypeNames(provider), Has.None.Contains("Logging"));
    }

    [TestCase(true)]
    [TestCase(false)]
    // The defaultsBefore=false case relies on the framework applying ConfigureHttpClientDefaults before named-client actions regardless of registration order.
    public void AddLocalJDownloader_DefaultsAddLogger_HasNoLoggingHandler(bool defaultsBefore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (defaultsBefore)
        {
            services.ConfigureHttpClientDefaults(b => b.AddDefaultLogger());
        }

        services.AddLocalJDownloader(_ => { });
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
    public void AddLocalJDownloader_InvalidOptions_ThrowsOnResolution()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(o => o.BaseAddress = new Uri("http://192.0.2.10:3128"));
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<LocalJDownloaderClient>, Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AddLocalJDownloader_ClientAlreadyRegistered_KeepsExisting()
    {
        using var http = new HttpClient();
        var existing = new LocalJDownloaderClient(http);
        var services = new ServiceCollection();
        services.AddSingleton(existing);
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<LocalJDownloaderClient>(), Is.SameAs(existing));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AddLocalJDownloader_TypedClientRegistered_TypedClientWinsInEitherOrder(bool typedClientBefore)
    {
        var services = new ServiceCollection();
        if (typedClientBefore)
        {
            services.AddHttpClient<LocalJDownloaderClient>();
        }

        services.AddLocalJDownloader(_ => { });
        if (!typedClientBefore)
        {
            services.AddHttpClient<LocalJDownloaderClient>();
        }

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<LocalJDownloaderClient>();
        var second = provider.GetRequiredService<LocalJDownloaderClient>();

        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    public void AddLocalJDownloader_Called_DoesNotRegisterDevice()
    {
        var services = new ServiceCollection();
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<JDownloaderDevice>(), Is.Null);
    }

    [Test]
    public void AddLocalJDownloader_WithRelayRegistration_BothClientsResolveWithRedirectDisabled()
    {
        var services = new ServiceCollection();
        services.AddMyJDownloader(_ => { });
        services.AddLocalJDownloader(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<MyJDownloaderClient>(), Is.Not.Null);
        Assert.That(provider.GetService<LocalJDownloaderClient>(), Is.Not.Null);
        Assert.That(
            ((SocketsHttpHandler)PrimaryHandler(provider, MyJDownloaderServiceCollectionExtensions.HttpClientName)).AllowAutoRedirect,
            Is.False);
        Assert.That(((SocketsHttpHandler)PrimaryHandler(provider)).AllowAutoRedirect, Is.False);
    }

    private static HttpMessageHandler PrimaryHandler(IServiceProvider provider, string name = Name)
    {
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
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
