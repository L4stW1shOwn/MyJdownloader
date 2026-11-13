namespace MyJdownloader.Tests.ContentV2;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class ContentV2EndToEndTests
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

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
    public async Task GetFileIconAsync_ThroughFakeRelay_ReturnsPng()
    {
        var (device, bodies) = CreateRig(_ => FakeMyJdServer.Png(_png));

        var bytes = await device.ContentV2.GetFileIconAsync("movie.mkv").WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/contentV2/getFileIcon"));
            Assert.That(document.RootElement.GetProperty("params")[0].GetString(), Is.EqualTo("movie.mkv"));
            Assert.That(bytes, Is.EqualTo(_png));
        });
    }

    [Test]
    public async Task GetIconDescriptionAsync_ThroughFakeRelay_ReturnsDescriptor()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"cls\":\"ImageIcon\",\"key\":\"kc.abc\"}},\"rid\":{rid}}}")));

        var result = await device.ContentV2.GetIconDescriptionAsync("kc.abc").WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/contentV2/getIconDescription"));
            Assert.That(result?.Cls, Is.EqualTo("ImageIcon"));
            Assert.That(result?.Key, Is.EqualTo("kc.abc"));
        });
    }
}
