namespace MyJdownloader.Tests.Toolbar;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class ToolbarEndToEndTests
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    private static (JDownloaderDevice Device, List<string> Bodies) CreateRig(string data)
    {
        var bodies = new List<string>();
#pragma warning disable CA2000 // The fake handler is owned by the HttpClient of the test
        var server = new FakeMyJdServer(request =>
        {
            bodies.Add(FakeMyJdServer.DecryptedBodyOf(request, Key(4)));
            return FakeMyJdServer.EncryptedJson(
                Key(4),
                string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{data},\"rid\":{FakeMyJdServer.BodyRidOf(request, Key(4))}}}"));
        });
        var client = new MyJDownloaderClient(new HttpClient(server));
#pragma warning restore CA2000
        client.RestoreSession(CreateSession());
        return (client.GetDevice("dev1"), bodies);
    }

    [Test]
    public async Task GetStatusAsync_EndToEnd_SendsEmptyParamsAndReadsStatus()
    {
        var (device, bodies) = CreateRig(/*lang=json,strict*/ """{"state":"RUNNING","running":true,"limitspeed":5,"speed":10,"download_current":1,"download_complete":2}""");

        var status = await device.Toolbar.GetStatusAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/toolbar/getStatus"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo("[]"));
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Running, Is.True);
            Assert.That(status.SpeedLimit, Is.EqualTo(5));
            Assert.That(status.Speed, Is.EqualTo(10));
            Assert.That(status.BytesLoaded, Is.EqualTo(1));
            Assert.That(status.BytesTotal, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task TogglePauseDownloadsAsync_EndToEnd_SendsEmptyParamsAndReadsNewState()
    {
        var (device, bodies) = CreateRig("true");

        var state = await device.Toolbar.TogglePauseDownloadsAsync().WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/toolbar/togglePauseDownloads"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo("[]"));
            Assert.That(state, Is.True);
        });
    }
}
