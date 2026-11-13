namespace MyJdownloader.Tests.SystemControl;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class SystemEndToEndTests
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    private static (JDownloaderDevice Device, List<string> Bodies) CreateRig(Func<long, HttpResponseMessage> reply)
    {
        var bodies = new List<string>();
#pragma warning disable CA2000 // The fake handler is owned by the HttpClient of the test
        var server = new FakeMyJdServer(request =>
        {
            bodies.Add(FakeMyJdServer.DecryptedBodyOf(request, Key(4)));
            return reply(FakeMyJdServer.BodyRidOf(request, Key(4)));
        });
        var client = new MyJDownloaderClient(new HttpClient(server));
#pragma warning restore CA2000
        client.RestoreSession(CreateSession());
        return (client.GetDevice("dev1"), bodies);
    }

    [Test]
    public async Task RestartAsync_EndToEnd_SendsEncryptedBody()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"rid\":{rid}}}")));

        await device.System.RestartAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/system/restartJD"));
    }

    [Test]
    public async Task GetSystemInfoAsync_EndToEnd_ReturnsInfo()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"osString\":\"Linux 6.1\",\"heapMax\":1073741824,\"startupTimeStamp\":1762000000000,\"os64Bit\":true}},\"rid\":{rid}}}")));

        var info = await device.System.GetSystemInfoAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/system/getSystemInfos"));
            Assert.That(info.OsString, Is.EqualTo("Linux 6.1"));
            Assert.That(info.HeapMax, Is.EqualTo(1073741824));
            Assert.That(info.StartupTime, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1762000000000)));
            Assert.That(info.Os64Bit, Is.True);
        });
    }

    [Test]
    public async Task GetStorageInfosAsync_EndToEnd_ReturnsStorages()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"path\":\"/\",\"size\":100,\"free\":40,\"error\":null}}],\"rid\":{rid}}}")));

        var storages = await device.System.GetStorageInfosAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/system/getStorageInfos"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo(/*lang=json,strict*/ """["null"]"""));
            Assert.That(storages.Single().Path, Is.EqualTo("/"));
            Assert.That(storages.Single().Size, Is.EqualTo(100));
            Assert.That(storages.Single().Error, Is.Null);
        });
    }
}
