namespace MyJdownloader.LiveTests.Live;

[TestFixture]
[Category("Live")]
public sealed class DeviceLiveTests
{
    [Test]
    [CancelAfter(60_000)]
    public async Task Ping_DeviceAnswersTrue(CancellationToken cancellationToken)
    {
        var ping = await LiveGuard.RunAsync(() => LiveSession.Device.Device.PingAsync(cancellationToken));

        Assert.That(ping, Is.True);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Version_IsPositive(CancellationToken cancellationToken)
    {
        var version = await LiveGuard.RunAsync(() => LiveSession.Device.Jd.GetVersionAsync(cancellationToken));

        Assert.That(version, Is.GreaterThan(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Uptime_IsPositive(CancellationToken cancellationToken)
    {
        var uptime = await LiveGuard.RunAsync(() => LiveSession.Device.Jd.GetUptimeAsync(cancellationToken));

        Assert.That(uptime, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task CoreRevision_IsPositive(CancellationToken cancellationToken)
    {
        var revision = await LiveGuard.RunAsync(() => LiveSession.Device.Jd.GetCoreRevisionAsync(cancellationToken));

        Assert.That(revision, Is.GreaterThan(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task LinkCrawler_IsCrawling_Answers(CancellationToken cancellationToken)
    {
        var crawling = await LiveGuard.RunAsync(() => LiveSession.Device.LinkCrawler.IsCrawlingAsync(cancellationToken));

        await TestContext.Out.WriteLineAsync($"IsCrawling: {crawling}");
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Update_IsUpdateAvailable_Answers(CancellationToken cancellationToken)
    {
        var available = await LiveGuard.RunAsync(() => LiveSession.Device.Update.IsUpdateAvailableAsync(cancellationToken));

        await TestContext.Out.WriteLineAsync($"IsUpdateAvailable: {available}");
    }
}
