namespace MyJdownloader.LiveTests.Local;

using MyJdownloader.Downloads;
using MyJdownloader.Errors;
using MyJdownloader.Local;

/// <summary>
/// Opt-in read-only tests against a real JDownloader local API. They run only when MYJD_LOCAL_URL is set
/// and live outside the Live namespace so the MyJD credentials fixture does not ignore them.
/// </summary>
[TestFixture]
[Category("Live")]
public sealed class LocalLiveTests : IDisposable
{
    private HttpClient? _httpClient;
    private LocalJDownloaderClient _client = null!;

    [OneTimeSetUp]
    public void CreateClient()
    {
        var url = Environment.GetEnvironmentVariable("MYJD_LOCAL_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            Assert.Ignore("MYJD_LOCAL_URL not set");
        }

#pragma warning disable CA2000 // The handler is owned and disposed by the HttpClient.
        _httpClient = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
#pragma warning restore CA2000
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        _client = new LocalJDownloaderClient(
            _httpClient,
            new LocalJDownloaderOptions { BaseAddress = new Uri(url!), AllowUnencryptedRemoteHost = true });
    }

    [OneTimeTearDown]
    public void DisposeClient() => Dispose();

    /// <inheritdoc/>
    public void Dispose() => _httpClient?.Dispose();

    [Test]
    [CancelAfter(60_000)]
    public async Task Jd_GetVersion_ReturnsAPositiveRevision(CancellationToken cancellationToken)
    {
        var version = await _client.Device.Channel.CallAsync("/jd/version", LiveJsonContext.Default.Int64, [], cancellationToken);

        Assert.That(version, Is.GreaterThan(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task System_GetSystemInfoAsync_ReturnsAnOperatingSystem(CancellationToken cancellationToken)
    {
        var info = await _client.Device.System.GetSystemInfoAsync(cancellationToken);

        Assert.That(info.HeapMax, Is.GreaterThan(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Downloads_QueryPackagesAsync_Completes(CancellationToken cancellationToken)
    {
        var packages = await _client.Device.Downloads.QueryPackagesAsync(new DownloadPackageQuery { MaxResults = 1 }, cancellationToken);

        Assert.That(packages, Is.Not.Null);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task ContentV2_GetIconAsync_ReturnsAPng(CancellationToken cancellationToken)
    {
        var icon = await _client.Device.ContentV2.GetIconAsync("download", 32, cancellationToken);

        Assert.That(icon.AsSpan(0, 4).ToArray(), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Channel_UnknownAction_ThrowsApiCommandNotFound(CancellationToken cancellationToken)
    {
        var exception = await Assert.ThrowsAsync<MyJdApiException>(
            () => _client.Device.Channel.CallAsync("/jd/nothing", LiveJsonContext.Default.JsonElement, [], cancellationToken));

        Assert.That(exception!.ErrorType, Is.EqualTo("API_COMMAND_NOT_FOUND"));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Channel_UnknownNamespace_ThrowsProtocol(CancellationToken cancellationToken)
        => await Assert.ThrowsAsync<MyJdProtocolException>(
            () => _client.Device.Channel.CallAsync("/nope/nothing", LiveJsonContext.Default.JsonElement, [], cancellationToken));
}
