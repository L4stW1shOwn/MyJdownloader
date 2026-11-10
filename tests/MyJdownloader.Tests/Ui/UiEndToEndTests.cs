namespace MyJdownloader.Tests.Ui;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Tests.Transport;
using MyJdownloader.Ui;

[TestFixture]
public sealed class UiEndToEndTests
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
    public async Task GetMenuAsync_EndToEnd_ReadsMenu()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"children\":[{{\"children\":null,\"icon\":\"play\",\"id\":\"forcedls\",\"name\":\"Force\",\"type\":\"ACTION\"}}]}},\"rid\":{rid}}}")));

        var menu = await device.Ui.GetMenuAsync(MenuContext.Dlc).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/ui/getMenu"));
            Assert.That(document.RootElement.GetProperty("params")[0].GetString(), Is.EqualTo("DLC"));
            Assert.That(menu.Children.Single().Id, Is.EqualTo("forcedls"));
            Assert.That(menu.Children.Single().Type, Is.EqualTo(MenuItemType.Action));
            Assert.That(menu.Children.Single().Children, Is.Empty);
        });
    }

    [Test]
    public async Task InvokeActionAsync_EndToEnd_ReturnsNullElement()
    {
        var (device, bodies) = CreateRig(rid => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"rid\":{rid}}}")));

        var result = await device.Ui.InvokeActionAsync(MenuContext.Dlc, "forcedls", [5], [6]).WaitAsync(_waitTimeout);

        using var document = JsonDocument.Parse(bodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo("/ui/invokeAction"));
            Assert.That(document.RootElement.GetProperty("params").GetRawText(), Is.EqualTo(/*lang=json,strict*/ """["DLC","forcedls","[5]","[6]"]"""));
            Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }
}
