namespace MyJdownloader.Tests.LinkGrabber;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.LinkGrabber;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class LinkGrabberEndToEndTests
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
    public async Task AddLinksAsync_EndToEnd_SendsEncryptedBody()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"id\":7}},\"rid\":{rid}}}")));

        var job = await device.LinkGrabber.AddLinksAsync(new AddLinksRequest { Links = ["https://h/a", "https://h/b"] }).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        var root = document.RootElement;
        using var query = JsonDocument.Parse(root.GetProperty("params")[0].GetString()!);
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("url").GetString(), Is.EqualTo("/linkgrabberv2/addLinks"));
            Assert.That(query.RootElement.GetProperty("links").GetString(), Is.EqualTo("https://h/a\nhttps://h/b"));
            Assert.That(job.Id, Is.EqualTo(7L));
        });
    }

    [Test]
    public async Task AddContainerAsync_EndToEnd_SendsExtensionAndDataUrl()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"id\":9}},\"rid\":{rid}}}")));
        byte[] bytes = [1, 2, 3, 250];

        var job = await device.LinkGrabber.AddContainerAsync(ContainerType.Dlc, bytes).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        var root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("url").GetString(), Is.EqualTo("/linkgrabberv2/addContainer"));
            Assert.That(root.GetProperty("params")[0].GetString(), Is.EqualTo("dlc"));
            Assert.That(root.GetProperty("params")[1].GetString(), Is.EqualTo("data:application/octet-stream;base64," + Convert.ToBase64String(bytes)));
            Assert.That(job!.Id, Is.EqualTo(9L));
        });
    }

    [Test]
    public async Task QueryLinksAsync_ResponseOverOneMebibyte_ThrowsProtocol()
    {
        // A valid answer whose encrypted (Base64) body is 1_048_600 characters, 24 above the cap:
        // without the cap it would decrypt and deserialize into one link.
        var (device, _) = CreateRig(rid =>
        {
            var head = string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"uuid\":1,\"packageUUID\":2,\"name\":\"");
            var tail = string.Create(CultureInfo.InvariantCulture, $"\"}}],\"rid\":{rid}}}");
            var json = head + new string('a', 786_440 - head.Length - tail.Length) + tail;
            return FakeMyJdServer.EncryptedJson(Key(4), json);
        });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => device.LinkGrabber.QueryLinksAsync().WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        Assert.That(thrown.Message, Is.EqualTo("The MyJD device response exceeds the configured size limit."));
    }
}
