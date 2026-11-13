namespace MyJdownloader.Tests.CaptchaForward;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class CaptchaForwardEndToEndTests
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
    public async Task CreateJobRecaptchaV2Async_EndToEnd_SendsParamsAndReadsJobId()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":123,\"rid\":{rid}}}")));

        var id = await device.CaptchaForward.CreateJobRecaptchaV2Async("key", "example.com", reason: "why").WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/captchaforward/createJobRecaptchaV2"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo(/*lang=json,strict*/ """["key","","example.com","why"]"""));
            Assert.That(id, Is.EqualTo(123));
        });
    }

    [Test]
    public async Task GetResultAsync_EndToEnd_SendsJobIdAndReadsToken()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":\"tok\",\"rid\":{rid}}}")));

        var token = await device.CaptchaForward.GetResultAsync(123).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/captchaforward/getResult"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo(/*lang=json,strict*/ """["123"]"""));
            Assert.That(token, Is.EqualTo("tok"));
        });
    }
}
