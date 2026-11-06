namespace MyJdownloader.Tests.Downloads;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class DownloadsEndToEndTests
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
    public async Task QueryLinksAsync_EndToEnd_SendsEncryptedBody()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"uuid\":7,\"addedDate\":1730900000000}}],\"rid\":{rid}}}")));

        var links = await device.Downloads.QueryLinksAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        var root = document.RootElement;
        var link = links.Single();
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("url").GetString(), Is.EqualTo("/downloadsV2/queryLinks"));
            Assert.That(root.GetProperty("params")[0].GetString(), Does.Contain("\"password\":false"));
            Assert.That(link.Uuid, Is.EqualTo(7L));
            Assert.That(link.AddedDate, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1730900000000)));
        });
    }
}
