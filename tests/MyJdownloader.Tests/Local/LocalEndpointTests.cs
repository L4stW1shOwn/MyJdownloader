namespace MyJdownloader.Tests.Local;

using MyJdownloader.Local;

[TestFixture]
public sealed class LocalEndpointTests
{
    [Test]
    public void Create_Default_AcceptsLoopbackHttp()
    {
        var endpoint = LocalEndpoint.Create(new LocalJDownloaderOptions());

        Assert.That(endpoint.BaseAddress.AbsoluteUri, Is.EqualTo("http://127.0.0.1:3128/"));
    }

    [TestCase("http://LOCALHOST:3128")]
    [TestCase("http://127.8.9.10:1")]
    [TestCase("http://[::1]:3128")]
    [TestCase("http://[::ffff:127.0.0.1]:3128")]
    public void Create_LoopbackHttp_IsAccepted(string address)
    {
        var options = new LocalJDownloaderOptions { BaseAddress = new Uri(address) };

        Assert.That(() => LocalEndpoint.Create(options), Throws.Nothing);
    }

    [TestCase("http://192.0.2.10:3129", "192.0.2.10")]
    [TestCase("http://jd.lan", "jd.lan")]
    [TestCase("http://localhost.example.com", "localhost.example.com")]
    [TestCase("http://127.0.0.1.nip.io", "127.0.0.1.nip.io")]
    [TestCase("http://localhost.", "localhost.")]
    [TestCase("http://[::2]", "::2")]
    public void Create_RemoteHttp_Throws(string address, string host)
    {
        var options = new LocalJDownloaderOptions { BaseAddress = new Uri(address) };

        var exception = Assert.Throws<ArgumentException>(() => LocalEndpoint.Create(options));

        Assert.That(exception!.ParamName, Is.EqualTo("options"));
        Assert.That(exception.Message, Does.Not.Contain(host));
        Assert.That(exception.Message, Does.StartWith("BaseAddress uses http with a host that is not a loopback address"));
    }

    [Test]
    public void Create_RemoteHttpWithOptIn_IsAccepted()
    {
        var options = new LocalJDownloaderOptions
        {
            BaseAddress = new Uri("http://192.0.2.10:3129"),
            AllowUnencryptedRemoteHost = true,
        };

        Assert.That(() => LocalEndpoint.Create(options), Throws.Nothing);
    }

    [Test]
    public void Create_RemoteHttps_IsAccepted()
    {
        var options = new LocalJDownloaderOptions { BaseAddress = new Uri("https://jd.lan:3128") };

        Assert.That(() => LocalEndpoint.Create(options), Throws.Nothing);
    }

    [TestCase(null)]
    [TestCase("/x")]
    [TestCase("ftp://127.0.0.1")]
    [TestCase("http://user:pw@127.0.0.1")]
    [TestCase("http://@127.0.0.1")]
    [TestCase("http://127.0.0.1/?a=1")]
    [TestCase("http://127.0.0.1/#f")]
    public void Create_InvalidAddress_Throws(string? address)
    {
        var options = new LocalJDownloaderOptions
        {
            BaseAddress = address is null ? null! : new Uri(address, UriKind.RelativeOrAbsolute),
        };

        var exception = Assert.Throws<ArgumentException>(() => LocalEndpoint.Create(options));

        Assert.That(exception!.ParamName, Is.EqualTo("options"));
        Assert.That(
            exception.Message,
            Does.StartWith("BaseAddress must be an absolute http or https URI without user info, query or fragment."));
    }

    [Test]
    public void Create_MaxResponseBodyLengthBelowMinimum_Throws()
    {
        var options = new LocalJDownloaderOptions { MaxResponseBodyLength = 1023 };

        var exception = Assert.Throws<ArgumentException>(() => LocalEndpoint.Create(options));

        Assert.That(exception!.ParamName, Is.EqualTo("options"));
        Assert.That(exception.Message, Does.StartWith("MaxResponseBodyLength must be greater than or equal to 1024."));
    }

    [Test]
    public void Create_MaxResponseBodyLengthAtMinimum_IsAccepted()
    {
        var options = new LocalJDownloaderOptions { MaxResponseBodyLength = 1024 };

        Assert.That(LocalEndpoint.Create(options).MaxResponseBodyLength, Is.EqualTo(1024));
    }

    [Test]
    public void Create_SnapshotsTheOptions()
    {
        var options = new LocalJDownloaderOptions();
        var endpoint = LocalEndpoint.Create(options);

        options.BaseAddress = new Uri("https://other.example:1");
        options.MaxResponseBodyLength = 5000;

        Assert.That(endpoint.BaseAddress.AbsoluteUri, Is.EqualTo("http://127.0.0.1:3128/"));
        Assert.That(endpoint.MaxResponseBodyLength, Is.EqualTo(1_048_576));
    }

    [TestCase("http://127.0.0.1:3128", "/jd/version", "http://127.0.0.1:3128/jd/version")]
    [TestCase("http://127.0.0.1:8080/jd/", "/downloadsV2/queryPackages", "http://127.0.0.1:8080/jd/downloadsV2/queryPackages")]
    public void BuildUri_AppendsThePath(string baseAddress, string path, string expected)
    {
        var endpoint = LocalEndpoint.Create(new LocalJDownloaderOptions { BaseAddress = new Uri(baseAddress) });

        Assert.That(endpoint.BuildUri(path).AbsoluteUri, Is.EqualTo(expected));
    }

    [TestCase("http://127.0.0.1:3128/other", true)]
    [TestCase("http://127.0.0.2:3128/", false)]
    [TestCase("http://127.0.0.1:3129/", false)]
    [TestCase("https://127.0.0.1:3128/", false)]
    [TestCase(null, false)]
    public void IsSameOrigin_ComparesSchemeHostAndPort(string? other, bool expected)
    {
        var endpoint = LocalEndpoint.Create(new LocalJDownloaderOptions());

        Assert.That(endpoint.IsSameOrigin(other is null ? null : new Uri(other)), Is.EqualTo(expected));
    }
}
