namespace MyJdownloader.Tests.Extraction;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Extraction;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class ExtractionEndToEndTests
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
    public async Task GetQueueAsync_EndToEnd_DeserializesArchiveStatus()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"archiveId\":\"a1\",\"archiveName\":\"x.rar\",\"controllerId\":-1,\"controllerStatus\":\"NA\",\"states\":{{\"x.part1.rar\":\"COMPLETE\"}},\"type\":\"RAR_MULTI\"}}],\"rid\":{rid}}}")));

        var queue = await device.Extraction.GetQueueAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        var status = queue.Single();
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/extraction/getQueue"));
            Assert.That(status.ArchiveId, Is.EqualTo("a1"));
            Assert.That(status.ControllerId, Is.EqualTo(-1L));
            Assert.That(status.ControllerStatus, Is.EqualTo(ExtractionControllerStatus.Na));
            Assert.That(status.States!["x.part1.rar"], Is.EqualTo(ArchiveFileStatus.Complete));
            Assert.That(status.Type, Is.EqualTo("RAR_MULTI"));
        });
    }
}
