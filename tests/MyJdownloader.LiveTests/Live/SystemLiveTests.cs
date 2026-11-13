namespace MyJdownloader.LiveTests.Live;

[TestFixture]
[Category("Live")]
public sealed class SystemLiveTests
{
    [Test]
    [CancelAfter(60_000)]
    public async Task GetSystemInfoAsync_ReturnsInfo(CancellationToken cancellationToken)
    {
        var info = await LiveGuard.RunAsync(() => LiveSession.Device.System.GetSystemInfoAsync(cancellationToken));

        Assert.That(info.HeapMax, Is.GreaterThan(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task GetStorageInfosAsync_NullPath_ReturnsRoots(CancellationToken cancellationToken)
    {
        var storages = await LiveGuard.RunAsync(() => LiveSession.Device.System.GetStorageInfosAsync(cancellationToken: cancellationToken));

        Assert.That(storages, Is.Not.Null);
    }
}
