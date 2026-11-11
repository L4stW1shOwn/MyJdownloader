namespace MyJdownloader.LiveTests.Live;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Downloads;

[TestFixture]
[Category("Live")]
public sealed class DownloadsLiveTests
{
    private const string BasePath = "/downloadsV2/";

    private static DownloadsApi Downloads => LiveSession.Device.Downloads;

    private static async Task<DownloadLink> FirstLinkOrIgnoreAsync(CancellationToken cancellationToken)
    {
        var links = await LiveGuard.RunAsync(() => Downloads.QueryLinksAsync(cancellationToken: cancellationToken));
        if (links.Count == 0)
        {
            Assert.Ignore("Download list is empty");
        }

        return links[0];
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task QueryLinks_Answers(CancellationToken cancellationToken)
    {
        var links = await LiveGuard.RunAsync(() => Downloads.QueryLinksAsync(cancellationToken: cancellationToken));

        Assert.That(links, Is.Not.Null);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task QueryPackages_Answers(CancellationToken cancellationToken)
    {
        var packages = await LiveGuard.RunAsync(() => Downloads.QueryPackagesAsync(cancellationToken: cancellationToken));

        Assert.That(packages, Is.Not.Null);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task PackageCount_IsNotNegative(CancellationToken cancellationToken)
    {
        var count = await LiveGuard.RunAsync(() => Downloads.GetPackageCountAsync(cancellationToken));

        Assert.That(count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task StructureChangeCounter_Probe(CancellationToken cancellationToken)
    {
        var first = await LiveGuard.RunAsync(() => Downloads.GetStructureChangeCounterAsync(0, cancellationToken));
        var value = first.ToString(CultureInfo.InvariantCulture);

        var firstRaw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getStructureChangeCounter", [LiveGuard.Json(value)], cancellationToken));
        LiveOutput.Dump("getStructureChangeCounter (first)", firstRaw);

        var secondRaw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getStructureChangeCounter", [LiveGuard.Json(value)], cancellationToken));
        LiveOutput.Dump("getStructureChangeCounter (second)", secondRaw);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task StopMark_Probe(CancellationToken cancellationToken)
    {
        var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getStopMark", [], cancellationToken));
        LiveOutput.Dump("getStopMark", raw);

        await Assert.DoesNotThrowAsync(async () => await Downloads.GetStopMarkAsync(cancellationToken));
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task StopMarkedLink_Answers(CancellationToken cancellationToken)
    {
        var link = await LiveGuard.RunAsync(() => Downloads.GetStopMarkedLinkAsync(cancellationToken));

        await TestContext.Out.WriteLineAsync($"Stop marked link present: {link is not null}");
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task DownloadUrls_EachDisplayType_Probe(CancellationToken cancellationToken)
    {
        var link = await FirstLinkOrIgnoreAsync(cancellationToken);
        var linkIds = LiveGuard.Json("[" + link.Uuid.ToString(CultureInfo.InvariantCulture) + "]");
        var packageIds = LiveGuard.Json("[]");

        foreach (var type in Enum.GetValues<UrlDisplayType>().Where(t => t != UrlDisplayType.Unknown))
        {
            var wireName = JsonNamingPolicy.SnakeCaseUpper.ConvertName(type.ToString());
            var types = LiveGuard.Json("[\"" + wireName + "\"]");
            await TestContext.Out.WriteLineAsync($"Display type: {type}");
            try
            {
                var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getDownloadUrls", [linkIds, packageIds, types], cancellationToken));
                LiveOutput.Dump("getDownloadUrls", LiveGuard.RedactUrlKeys(raw));
            }
            catch (MyJdException ex)
            {
                await TestContext.Out.WriteLineAsync("getDownloadUrls failed with " + ex.GetType().Name);
            }
        }
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task StopMark_SetThenRemove_RestoresNone(CancellationToken cancellationToken)
    {
        var link = await FirstLinkOrIgnoreAsync(cancellationToken);
        var existing = await LiveGuard.RunAsync(() => Downloads.GetStopMarkAsync(cancellationToken));
        if (existing is not null)
        {
            Assert.Ignore("A stop mark already exists");
        }

        // Race accepted for an opt-in test: a stop mark set by the user between the check above and the Set
        // below would be removed by the finally block.
        try
        {
            await LiveGuard.RunAsync(() => Downloads.SetStopMarkAsync(link.Uuid, link.PackageUuid, cancellationToken));
            var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getStopMark", [], cancellationToken));
            LiveOutput.Dump("getStopMark after set", raw);
        }
        finally
        {
            try
            {
                await Downloads.RemoveStopMarkAsync(CancellationToken.None);
            }
            catch (MyJdException)
            {
                await TestContext.Out.WriteLineAsync("WARNING: stop mark cleanup failed");
            }
        }

        var after = await LiveGuard.RunAsync(() => Downloads.GetStopMarkAsync(cancellationToken));
        Assert.That(after, Is.Null);
    }
}
