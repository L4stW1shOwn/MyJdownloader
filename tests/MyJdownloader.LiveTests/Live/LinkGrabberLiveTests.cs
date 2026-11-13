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

    // A site crawl (a video page for example) is slower than a direct link: wait up to 90 s.
    private const int VariantWaitAttempts = 180;

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

    private static Task<CrawledLink?> WaitForLinkAsync(long jobId, CancellationToken cancellationToken)
        => WaitForLinkAsync(jobId, IsTestLink, 40, cancellationToken);

    private static async Task<CrawledLink?> WaitForLinkAsync(long jobId, Func<CrawledLink, bool> isOwned, int attempts, CancellationToken cancellationToken)
    {
        var query = new LinkQuery { JobUuids = [jobId] };
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var links = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(query, cancellationToken));
            var match = links.FirstOrDefault(isOwned);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    private static bool IsVariantsUnsupported(MyJdApiException ex) =>
        ex.ErrorSource == MyJdErrorSource.Device && ex.ErrorType == "INTERNAL_SERVER_ERROR";

    private static bool IsCleanupFailure(Exception ex) =>
        ex is MyJdException or OperationCanceledException or TimeoutException or HttpRequestException;

    /// <summary>Removes only the link created by the test; never lets a cleanup failure replace the test outcome.</summary>
    private static Task<bool> RemoveTestLinkAsync(long jobId, long? linkId)
        => RemoveTestLinkAsync(jobId, linkId, IsTestLink);

    /// <summary>Removes only the links of the job accepted by <paramref name="isOwned"/>; never lets a cleanup failure replace the test outcome.</summary>
    private static async Task<bool> RemoveTestLinkAsync(long jobId, long? linkId, Func<CrawledLink, bool> isOwned)
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
            foreach (var link in late.Where(isOwned))
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

        var jobs = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinkCrawlerJobsAsync(cancellationToken: cancellationToken));
        Assert.That(jobs, Is.Not.Null);
        await TestContext.Out.WriteLineAsync($"Crawler jobs: {jobs.Count}");
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

        var changed = await LiveGuard.RunAsync(() => LinkGrabber.GetChildrenChangedAsync(0, cancellationToken));
        await TestContext.Out.WriteLineAsync($"Children changed: {changed}");
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task TestLink_AddInspectRemove(CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("MYJD_LIVE_WRITES") != "1")
        {
            Assert.Ignore("Live writes not enabled (set MYJD_LIVE_WRITES=1)");
        }

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

            try
            {
                var variants = await LiveGuard.RunAsync(() => LinkGrabber.GetVariantsAsync(link.Uuid, cancellationToken));
                foreach (var variant in variants)
                {
                    await TestContext.Out.WriteLineAsync($"Variant id length: {variant.Id?.Length ?? 0}");
                }

                var rawVariants = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getVariants", [LiveGuard.Json(uuid)], cancellationToken));
                LiveOutput.Dump("getVariants", rawVariants);
            }
            catch (MyJdApiException ex) when (IsVariantsUnsupported(ex))
            {
                await TestContext.Out.WriteLineAsync("getVariants: not supported for this link (INTERNAL_SERVER_ERROR)");
            }

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

    [Test]
    [CancelAfter(120_000)]
    public async Task VariantLink_GetVariants(CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("MYJD_LIVE_WRITES") != "1")
        {
            Assert.Ignore("Live writes not enabled (set MYJD_LIVE_WRITES=1)");
        }

        var variantUrl = Environment.GetEnvironmentVariable("MYJD_VARIANT_URL");
        if (!Uri.TryCreate(variantUrl, UriKind.Absolute, out var variantUri)
            || (variantUri.Scheme != Uri.UriSchemeHttps && variantUri.Scheme != Uri.UriSchemeHttp))
        {
            Assert.Ignore("No variant link (set MYJD_VARIANT_URL to an http(s) link with variants)");
        }

        // The crawler may rewrite the URL (a plugin-specific scheme for example): own only the links of this job
        // whose plugin host or URL host is the host of the variant URL.
        var host = StripWww(variantUri.Host);
        bool IsVariantLink(CrawledLink link) =>
            (link.Host is { } linkHost && string.Equals(StripWww(linkHost), host, StringComparison.OrdinalIgnoreCase))
            || (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri)
                && string.Equals(StripWww(uri.Host), host, StringComparison.OrdinalIgnoreCase));

        // The URL is user data: never write it, nor the link names, to the test output.
        var job = await LiveGuard.RunAsync(() => LinkGrabber.AddLinksAsync(new AddLinksRequest { Links = [variantUri.AbsoluteUri], Autostart = false, AssignJobId = true }, cancellationToken));
        if (job.Id <= 0)
        {
            Assert.Inconclusive("Unexpected link collecting job id");
        }

        long? linkId = null;
        try
        {
            var link = await WaitForLinkAsync(job.Id, IsVariantLink, VariantWaitAttempts, cancellationToken);
            if (link is null)
            {
                await WriteJobDiagnosticAsync(job.Id, cancellationToken);
                Assert.Inconclusive("Variant link did not appear");
            }

            linkId = link.Uuid;
            var uuid = link.Uuid.ToString(CultureInfo.InvariantCulture);

            var rawVariants = await LiveGuard.RunAsync(() => LiveGuard.RawAsync(BasePath + "getVariants", [LiveGuard.Json(uuid)], cancellationToken));
            LiveOutput.Dump("getVariants (variant link)", rawVariants);

            var variants = await LiveGuard.RunAsync(() => LinkGrabber.GetVariantsAsync(link.Uuid, cancellationToken));
            await TestContext.Out.WriteLineAsync($"Variants: {variants.Count}");
            Assert.That(variants, Is.Not.Empty);
            Assert.That(variants.All(v => !string.IsNullOrEmpty(v.Id)), Is.True);
        }
        finally
        {
            await RemoveTestLinkAsync(job.Id, linkId, IsVariantLink);
        }
    }

    [TestCase(ContainerType.CrawlJob)]
    [TestCase(ContainerType.Meta4)]
    [CancelAfter(60_000)]
    public async Task Container_AddThenRemove(ContainerType type, CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("MYJD_LIVE_WRITES") != "1")
        {
            Assert.Ignore("Live writes not enabled (set MYJD_LIVE_WRITES=1)");
        }

        // Container links may be collected under another job than the one returned: the probe looks for the fixed
        // test link in the whole link grabber. Only links whose URL is exactly TestLink are touched; leftovers of an
        // interrupted run are removed first so they cannot be mistaken for this run's link.
        Assert.That(await RemoveAllTestLinksAsync(), Is.True, "Could not remove leftover test links");

        var content = System.Text.Encoding.UTF8.GetBytes(ContainerHoldingTestLink(type));
        bool cleanedUp;
        try
        {
            var job = await LiveGuard.RunAsync(() => LinkGrabber.AddContainerAsync(type, content, cancellationToken));
            if (job is null)
            {
                Assert.Inconclusive("The device could not store the container");
            }

            await TestContext.Out.WriteLineAsync($"Job id positive: {job.Id > 0}");
            var link = await WaitForTestLinkAnywhereAsync(cancellationToken);
            if (link is null)
            {
                await WriteJobDiagnosticAsync(job.Id, cancellationToken);
                var downloads = await LiveGuard.RunAsync(() => LiveSession.Device.Downloads.QueryLinksAsync(cancellationToken: cancellationToken));
                await TestContext.Out.WriteLineAsync($"Test link in the download list: {downloads.Count(d => string.Equals(d.Url, TestLink, StringComparison.Ordinal))}");
                Assert.Inconclusive("Container link did not appear");
            }

            await TestContext.Out.WriteLineAsync("Container link found");
        }
        finally
        {
            cleanedUp = await RemoveAllTestLinksAsync();
        }

        if (cleanedUp)
        {
            var remaining = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(new LinkQuery(), cancellationToken));
            Assert.That(remaining.Where(IsTestLink), Is.Empty);
        }
    }

    private static async Task<CrawledLink?> WaitForTestLinkAnywhereAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var links = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(new LinkQuery(), cancellationToken));
            var match = links.FirstOrDefault(IsTestLink);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    /// <summary>Removes every link grabber link whose URL is exactly the test link; never lets a cleanup failure replace the test outcome.</summary>
    private static async Task<bool> RemoveAllTestLinksAsync()
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            var ids = (await LinkGrabber.QueryLinksAsync(new LinkQuery(), cleanup.Token)).Where(IsTestLink).Select(l => l.Uuid).ToArray();
            if (ids.Length > 0)
            {
                await LinkGrabber.RemoveLinksAsync(ids, [], cleanup.Token);
            }

            return true;
        }
        catch (Exception ex) when (IsCleanupFailure(ex))
        {
            await TestContext.Out.WriteLineAsync("WARNING: test link cleanup failed");
            return false;
        }
    }

    // Containers holding only the fixed test link. The crawljob forces confirmation and start off so the link stays in
    // the link grabber, where the cleanup looks for it; a Metalink has no such keys and follows the device settings.
    private static string ContainerHoldingTestLink(ContainerType type) => type == ContainerType.CrawlJob
        ? "text=" + TestLink + "\nautoConfirm=FALSE\nautoStart=FALSE\nforcedStart=FALSE\nenabled=TRUE\n"
        : "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<metalink xmlns=\"urn:ietf:params:xml:ns:metalink\">\n"
            + "  <file name=\"myjdownloader-live-test.bin\">\n"
            + "    <url>" + TestLink + "</url>\n"
            + "  </file>\n"
            + "</metalink>\n";

    /// <summary>Writes the links and crawler state of a job, without user data (counts, plugin hosts, URL schemes).</summary>
    private static async Task WriteJobDiagnosticAsync(long jobId, CancellationToken cancellationToken)
    {
        var jobLinks = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinksAsync(new LinkQuery { JobUuids = [jobId] }, cancellationToken));
        var shapes = jobLinks.Select(l => (l.Host ?? "?") + " " + (Uri.TryCreate(l.Url, UriKind.Absolute, out var u) ? u.Scheme : "?")).Distinct(StringComparer.Ordinal);
        await TestContext.Out.WriteLineAsync($"Job links: {jobLinks.Count} [{string.Join(", ", shapes)}]");
        var crawlerJobs = await LiveGuard.RunAsync(() => LinkGrabber.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { JobIds = [jobId], CollectorInfo = true }, cancellationToken));
        foreach (var crawlerJob in crawlerJobs)
        {
            await TestContext.Out.WriteLineAsync(
                $"Crawler job: crawling={crawlerJob.Crawling}, checking={crawlerJob.Checking}, crawled={crawlerJob.Crawled}, broken={crawlerJob.Broken}, filtered={crawlerJob.Filtered}, unhandled={crawlerJob.Unhandled}");
        }

        await TestContext.Out.WriteLineAsync($"Crawler jobs for this job id: {crawlerJobs.Count}");
    }

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
}
