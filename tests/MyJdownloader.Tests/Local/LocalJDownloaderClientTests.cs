namespace MyJdownloader.Tests.Local;

using System.Diagnostics.CodeAnalysis;
using MyJdownloader.Local;

[TestFixture]
[SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a handler disposed by the test")]
public sealed class LocalJDownloaderClientTests
{
    private const string SystemInfoBody = /*lang=json,strict*/ """{"data":{"archFamily":"ARM","archString":"aarch64","hardware":"RaspberryPI","heapCommitted":295174144,"heapMax":1879048192,"heapUsed":146194192,"javaName":"OpenJDK 64-Bit Server VM","javaVendor":"IcedTea","javaVersion":18492009,"javaVersionString":"1.8.0_492-b09","operatingSystem":"ALPINE","osFamily":"LINUX","osString":"Linux","startupTimeStamp":1794684858631,"arch64Bit":true,"docker":true,"headless":false,"jvm64Bit":true}}""";
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];

    private static LocalJDownloaderClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: false));

    [Test]
    public void Constructor_NullHttpClient_Throws() =>
        Assert.That(() => new LocalJDownloaderClient(null!), Throws.TypeOf<ArgumentNullException>());

    [Test]
    public void Constructor_RemoteHttpWithoutOptIn_ThrowsForOptions()
    {
        using var httpClient = new HttpClient();
        var options = new LocalJDownloaderOptions { BaseAddress = new Uri("http://192.0.2.10:3129") };

        var thrown = Assert.Throws<ArgumentException>(() => new LocalJDownloaderClient(httpClient, options));

        Assert.That(thrown!.ParamName, Is.EqualTo("options"));
    }

    [Test]
    public void Device_HasLocalId()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json("{}"));

        Assert.That(Client(server).Device.Id, Is.EqualTo("local"));
    }

    [Test]
    public async Task Device_System_GetSystemInfoAsync_ThroughFakeLocalApi()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(SystemInfoBody));

        var info = await Client(server).Device.System.GetSystemInfoAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(server.Requests.Single().Uri, Is.EqualTo("http://127.0.0.1:3128/system/getSystemInfos"));
            Assert.That(info.ArchString, Is.EqualTo("aarch64"));
            Assert.That(info.Hardware, Is.EqualTo("RaspberryPI"));
            Assert.That(info.HeapMax, Is.EqualTo(1879048192L));
            Assert.That(info.Docker, Is.True);
            Assert.That(info.Headless, Is.False);
        });
    }

    [Test]
    public async Task Device_ContentV2_GetIconAsync_ThroughFakeLocalApi_ReturnsPng()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Png(_png));

        var icon = await Client(server).Device.ContentV2.GetIconAsync("download", 32).WaitAsync(_waitTimeout);

        Assert.That(icon, Is.EqualTo(_png));
    }

    [Test]
    public async Task Device_Downloads_QueryPackagesAsync_SendsTheQueryAsAnObject()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(/*lang=json,strict*/ """{"data":[]}"""));

        await Client(server).Device.Downloads.QueryPackagesAsync().WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Body, Does.StartWith("""{"params":[{"""));
    }
}
