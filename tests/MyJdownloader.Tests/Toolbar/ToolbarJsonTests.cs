namespace MyJdownloader.Tests.Toolbar;

using System.Text.Json;
using MyJdownloader.Toolbar;

[TestFixture]
public sealed class ToolbarJsonTests
{
    [Test]
    public void ToolbarStatus_EveryWireName_Maps()
    {
        const string payload = /*lang=json,strict*/ """
            {"state":"RUNNING","running":true,"limit":true,"limitspeed":1024,"reconnect":true,"clipboard":true,"stopafter":true,"premium":true,"speed":2048,"pause":true,"download_current":300,"download_complete":900}
            """;

        var status = JsonSerializer.Deserialize(payload, ToolbarJsonContext.Default.ToolbarStatus)!;

        Assert.Multiple(() =>
        {
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Running, Is.True);
            Assert.That(status.SpeedLimitEnabled, Is.True);
            Assert.That(status.SpeedLimit, Is.EqualTo(1024));
            Assert.That(status.AutoReconnectEnabled, Is.True);
            Assert.That(status.ClipboardMonitored, Is.True);
            Assert.That(status.StopAfterCurrentDownload, Is.True);
            Assert.That(status.PremiumEnabled, Is.True);
            Assert.That(status.Speed, Is.EqualTo(2048));
            Assert.That(status.Paused, Is.True);
            Assert.That(status.BytesLoaded, Is.EqualTo(300));
            Assert.That(status.BytesTotal, Is.EqualTo(900));
        });
    }

    [Test]
    public void ToolbarStatus_UnknownMembers_AreIgnored()
    {
        var status = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"running":true,"future":{"a":1},"other":[1,2]}""", ToolbarJsonContext.Default.ToolbarStatus)!;

        Assert.Multiple(() =>
        {
            Assert.That(status.Running, Is.True);
            Assert.That(status.State, Is.Null);
            Assert.That(status.Speed, Is.Zero);
        });
    }
}
