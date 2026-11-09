namespace MyJdownloader.Tests.Device;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class DeviceEndToEndTests
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
    public async Task PingAsync_EndToEnd_ReadsTrue()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":true,\"rid\":{rid}}}")));

        var result = await device.Device.PingAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/device/ping"));
            Assert.That(result, Is.True);
        });
    }
}
