namespace MyJdownloader.LiveTests.Live;

[TestFixture]
[Category("Live")]
public sealed class SystemLiveTests
{
    [Test]
    [CancelAfter(60_000)]
    public async Task SystemInfos_Probe(CancellationToken cancellationToken)
    {
        var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync("/system/getSystemInfos", [], cancellationToken));

        LiveOutput.Dump("getSystemInfos", raw);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task StorageInfos_Probe(CancellationToken cancellationToken)
    {
        var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync("/system/getStorageInfos", [LiveGuard.Json("null")], cancellationToken));

        LiveOutput.Dump("getStorageInfos", raw);
    }
}
