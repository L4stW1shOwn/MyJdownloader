namespace MyJdownloader.Tests.DownloadEvents;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class DownloadEventsEndToEndTests
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    [Test]
    public async Task SetStatusEventIntervalAsync_EndToEnd_SendsIdAndMillisecondsAndReadsResult()
    {
        var bodies = new List<string>();
#pragma warning disable CA2000 // The fake handler is owned by the HttpClient of the test
        var server = new FakeMyJdServer(request =>
        {
            bodies.Add(FakeMyJdServer.DecryptedBodyOf(request, Key(4)));
            return FakeMyJdServer.EncryptedJson(
                Key(4),
                string.Create(CultureInfo.InvariantCulture, $"{{\"data\":true,\"rid\":{FakeMyJdServer.BodyRidOf(request, Key(4))}}}"));
        });
        var client = new MyJDownloaderClient(new HttpClient(server));
#pragma warning restore CA2000
        client.RestoreSession(CreateSession());

        var result = await client.GetDevice("dev1").DownloadEvents.SetStatusEventIntervalAsync(7, TimeSpan.FromSeconds(1.5)).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/downloadevents/setStatusEventInterval"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo("[\"7\",\"1500\"]"));
            Assert.That(result, Is.True);
        });
    }
}
