namespace MyJdownloader.Tests.Polling;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Polling;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class PollingEndToEndTests
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
    public async Task PollAsync_EndToEnd_ReadsEvents()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"eventName\":\"jdState\",\"eventData\":{{\"data\":\"IDLE\"}}}}],\"rid\":{rid}}}")));

        var result = await device.Polling.PollAsync(new PollingQuery { DownloadProgress = false }).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/polling/poll"));
            Assert.That(document.RootElement.GetProperty("params")[0].GetString(), Does.Contain("jdState"));
            Assert.That(result.Single().EventName, Is.EqualTo("jdState"));
            Assert.That(result.Single().EventData!.Value.GetProperty("data").GetString(), Is.EqualTo("IDLE"));
        });
    }
}
