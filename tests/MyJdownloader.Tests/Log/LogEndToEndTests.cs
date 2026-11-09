namespace MyJdownloader.Tests.Log;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Log;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class LogEndToEndTests
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
    public async Task GetAvailableLogsAsync_EndToEnd_ReadsFolders()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"created\":1700000000000,\"current\":true,\"lastModified\":1700000500000}}],\"rid\":{rid}}}")));

        var result = await device.Log.GetAvailableLogsAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/log/getAvailableLogs"));
            Assert.That(result.Single().Current, Is.True);
            Assert.That(result.Single().Created, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000)));
        });
    }

    [Test]
    public async Task SendLogFileAsync_EndToEnd_ReturnsId()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":\"logid42\",\"rid\":{rid}}}")));
        var folder = new LogFolder
        {
            Created = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            Current = true,
            LastModified = DateTimeOffset.FromUnixTimeMilliseconds(1700000500000),
        };

        var id = await device.Log.SendLogFileAsync([folder]).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/log/sendLogFile"));
            Assert.That(document.RootElement.GetProperty("params")[0].GetString(), Does.Contain("1700000500000"));
            Assert.That(id, Is.EqualTo("logid42"));
        });
    }
}
