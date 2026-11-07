namespace MyJdownloader.Tests.Captcha;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class CaptchaEndToEndTests
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
    public async Task ListAsync_EndToEnd_DeserializesJob()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":[{{\"id\":42,\"link\":7,\"hoster\":\"example.com\",\"type\":\"RECAPTCHA_V2\"}}],\"rid\":{rid}}}")));

        var jobs = await device.Captcha.ListAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        var job = jobs.Single();
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/captcha/list"));
            Assert.That(job.Id, Is.EqualTo(42));
            Assert.That(job.LinkId, Is.EqualTo(7));
            Assert.That(job.Hoster, Is.EqualTo("example.com"));
        });
    }
}
