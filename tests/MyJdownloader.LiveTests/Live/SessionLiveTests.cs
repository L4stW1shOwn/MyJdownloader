namespace MyJdownloader.LiveTests.Live;

[TestFixture]
[Category("Live")]
public sealed class SessionLiveTests
{
    [Test]
    [CancelAfter(60_000)]
    public async Task ExportRestore_DeviceCallSucceeds(CancellationToken cancellationToken)
    {
        var session = LiveSession.Client.ExportSession();
        Assert.That(session, Is.Not.Null);

        LiveSession.Client.RestoreSession(session!);

        var ping = await LiveGuard.RunAsync(() => LiveSession.Device.Device.PingAsync(cancellationToken));
        Assert.That(ping, Is.True);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Reconnect_DeviceCallSucceeds(CancellationToken cancellationToken)
    {
        await LiveSession.Client.ReconnectAsync(cancellationToken);

        var ping = await LiveGuard.RunAsync(() => LiveSession.Device.Device.PingAsync(cancellationToken));
        Assert.That(ping, Is.True);
    }
}
