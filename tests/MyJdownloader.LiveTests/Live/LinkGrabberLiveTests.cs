namespace MyJdownloader.LiveTests.Live;

using System.Globalization;
using MyJdownloader.LinkGrabber;

[TestFixture]
[Category("Live")]
public sealed class LinkGrabberLiveTests
{
    private const string BasePath = "/linkgrabberv2/";
    private const string TestLink = "https://example.com/myjdownloader-live-test.bin";
    private const string FakePassword = "live-test-fake";

    private static LinkGrabberApi LinkGrabber => LiveSession.Device.LinkGrabber;

    private static async Task<CrawledLink?> WaitForLinkAsync(long jobId, CancellationToken cancellationToken)
    {
        var query = new LinkQuery { JobUuids = [jobId] };
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var links = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(query, cancellationToken));
            if (links.Count > 0)
            {
                return links[0];
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    /// <summary>Removes only the link created by the test; never lets a cleanup failure replace the test outcome.</summary>
    private static async Task<bool> RemoveTestLinkAsync(long jobId, long? linkId)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            if (linkId is { } known)
            {
                await LinkGrabber.RemoveLinksAsync([known], [], cleanup.Token);
                return true;
            }

            // The link was not seen in time: a late-arriving link of this job must not be left behind.
            var late = await LinkGrabber.QueryLinksAsync(new LinkQuery { JobUuids = [jobId] }, cleanup.Token);
            foreach (var link in late)
            {
                await LinkGrabber.RemoveLinksAsync([link.Uuid], [], cleanup.Token);
            }

            return true;
        }
        catch (Exception ex) when (ex is MyJdException or OperationCanceledException)
        {
            await TestContext.Out.WriteLineAsync("WARNING: test link cleanup failed");
            return false;
        }
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task QueryLinks_Answers(CancellationToken cancellationToken)
    {
        var links = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(cancellationToken: cancellationToken));

        Assert.That(links, Is.Not.Null);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task QueryPackages_Answers(CancellationToken cancellationToken)
    {
        var packages = await LiveGuard.RunAsync(() => LinkGrabber.QueryPackagesAsync(cancellationToken: cancellationToken));

        Assert.That(packages, Is.Not.Null);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task IsCollecting_Answers(CancellationToken cancellationToken)
    {
        var collecting = await LiveGuard.RunAsync(() => LinkGrabber.IsCollectingAsync(cancellationToken));

        await TestContext.Out.WriteLineAsync($"Collecting: {collecting}");
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task CrawlerJobs_EmptyQuery_Probe(CancellationToken cancellationToken)
    {
        var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "queryLinkCrawlerJobs", [LiveGuard.Json("{}")], cancellationToken));

        LiveOutput.Dump("queryLinkCrawlerJobs", raw);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task FolderHistory_Answers(CancellationToken cancellationToken)
    {
        var history = await LiveGuard.RunAsync(() => LinkGrabber.GetDownloadFolderHistoryAsync(cancellationToken));

        await TestContext.Out.WriteLineAsync($"Folder history entries: {history.Count}");
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task ChildrenChanged_Probe(CancellationToken cancellationToken)
    {
        var raw = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getChildrenChanged", [LiveGuard.Json("0")], cancellationToken));

        LiveOutput.Dump("getChildrenChanged", raw);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task TestLink_AddInspectRemove(CancellationToken cancellationToken)
    {
        var job = await LiveGuard.RunAsync(() => LinkGrabber.AddLinksAsync(new AddLinksRequest { Links = [TestLink], Autostart = false, AssignJobId = true }, cancellationToken));
        long? linkId = null;
        var cleanedUp = false;
        try
        {
            var link = await WaitForLinkAsync(job.Id, cancellationToken);
            if (link is null)
            {
                Assert.Inconclusive("Test link did not appear");
                return;
            }

            linkId = link.Uuid;
            var uuid = link.Uuid.ToString(CultureInfo.InvariantCulture);

            var variants = await LiveGuard.RunAsync(() => LinkGrabber.GetVariantsAsync(link.Uuid, cancellationToken));
            foreach (var variant in variants)
            {
                await TestContext.Out.WriteLineAsync($"Variant id length: {variant.Id?.Length ?? 0}");
            }

            var rawVariants = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getVariants", [LiveGuard.Json(uuid)], cancellationToken));
            LiveOutput.Dump("getVariants", rawVariants);

            await LiveGuard.RunAsync(() => LinkGrabber.SetDownloadPasswordAsync([link.Uuid], [], FakePassword, cancellationToken));
            await LiveGuard.RunAsync(() => LinkGrabber.SetDownloadPasswordAsync([link.Uuid], [], null, cancellationToken));

            var jobArg = LiveGuard.Json("{\"jobUUIDs\":[" + job.Id.ToString(CultureInfo.InvariantCulture) + "],\"password\":true}");
            var rawLinks = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "queryLinks", [jobArg], cancellationToken));
            LiveOutput.Dump("queryLinks after clearing the password", rawLinks);

            var urls = await LiveGuard.RunAsync(() => LinkGrabber.GetDownloadUrlsAsync([link.Uuid], [], [UrlDisplayType.Content], cancellationToken));
            Assert.That(urls.ContainsKey(TestLink), Is.True);

            await LiveGuard.RunAsync(() => LinkGrabber.StartOnlineStatusCheckAsync([link.Uuid], [], cancellationToken));
        }
        finally
        {
            cleanedUp = await RemoveTestLinkAsync(job.Id, linkId);
        }

        if (cleanedUp)
        {
            var remaining = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(new LinkQuery { JobUuids = [job.Id] }, cancellationToken));
            Assert.That(remaining, Is.Empty);
        }
    }
}
