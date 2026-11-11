namespace MyJdownloader.LiveTests.Live;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.LinkGrabber;

[TestFixture]
[Category("Live")]
public sealed class LinkGrabberLiveTests
{
    private const string BasePath = "/linkgrabberv2/";
    private const string TestLink = "https://example.com/myjdownloader-live-test.bin";
    private const string FakePassword = "live-test-fake";

    private static LinkGrabberApi LinkGrabber => LiveSession.Device.LinkGrabber;

    // The server-side jobUUIDs filter is not trusted: only a link whose Url is exactly TestLink may be
    // inspected, modified or removed, so a user's own link can never be touched by this test.
    private static bool IsTestLink(CrawledLink link) => string.Equals(link.Url, TestLink, StringComparison.Ordinal);

    // The jobUUIDs filter is not trusted here either: keep only raw entries whose url is exactly TestLink.
    private static JsonElement KeepTestLinkEntries(JsonElement rawLinks)
    {
        var kept = new List<JsonElement>();
        if (rawLinks.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in rawLinks.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty("url", out var url)
                    && url.ValueKind == JsonValueKind.String
                    && string.Equals(url.GetString(), TestLink, StringComparison.Ordinal))
                {
                    kept.Add(entry);
                }
            }
        }

        return JsonSerializer.SerializeToElement(kept, LiveJsonContext.Default.ListJsonElement);
    }

    private static async Task<CrawledLink?> WaitForLinkAsync(long jobId, CancellationToken cancellationToken)
    {
        var query = new LinkQuery { JobUuids = [jobId] };
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var links = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(query, cancellationToken));
            var match = links.FirstOrDefault(IsTestLink);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    private static bool IsCleanupFailure(Exception ex) =>
        ex is MyJdException or OperationCanceledException or TimeoutException or HttpRequestException;

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
            // Best effort: a link appearing after this query may remain.
            var success = true;
            foreach (var link in late.Where(IsTestLink))
            {
                try
                {
                    await LinkGrabber.RemoveLinksAsync([link.Uuid], [], cleanup.Token);
                }
                catch (Exception ex) when (IsCleanupFailure(ex))
                {
                    success = false;
                }
            }

            if (!success)
            {
                await TestContext.Out.WriteLineAsync("WARNING: test link cleanup failed");
            }

            return success;
        }
        catch (Exception ex) when (IsCleanupFailure(ex))
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
        // A TimeoutException from AddLinksAsync leaves no job id to clean up: check for the test link manually.
        var job = await LiveGuard.RunAsync(() => LinkGrabber.AddLinksAsync(new AddLinksRequest { Links = [TestLink], Autostart = false, AssignJobId = true }, cancellationToken));
        if (job.Id <= 0)
        {
            await RemoveTestLinkAsync(job.Id, null);
            Assert.Inconclusive("Unexpected link collecting job id");
        }

        long? linkId = null;
        bool cleanedUp;
        try
        {
            var link = await WaitForLinkAsync(job.Id, cancellationToken);
            if (link is null)
            {
                Assert.Inconclusive("Test link did not appear");
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

            var jobArg = LiveGuard.Json("{\"jobUUIDs\":[" + job.Id.ToString(CultureInfo.InvariantCulture) + "],\"password\":true,\"url\":true}");
            var rawLinks = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "queryLinks", [jobArg], cancellationToken));
            LiveOutput.Dump("queryLinks after clearing the password", KeepTestLinkEntries(rawLinks));

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
            Assert.That(remaining.Where(IsTestLink), Is.Empty);
        }
    }
}
