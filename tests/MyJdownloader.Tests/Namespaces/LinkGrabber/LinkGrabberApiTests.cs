namespace MyJdownloader.Tests.LinkGrabber;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Errors;
using MyJdownloader.LinkGrabber;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class LinkGrabberApiTests
{
    private const string Base = "/linkgrabberv2/";
    private const string Name = "name";

    private static readonly long[] _ids = [1, 2];
    private static readonly long[] _packageIds = [3];
    private static readonly long[] _noIds = [];
    private static readonly long[] _singleUuid = [5];
    private static readonly string[] _folders = ["/a", "/b"];
    private static readonly UrlDisplayType[] _contentTypes = [UrlDisplayType.Content];

    private static IEnumerable<string?> BadTexts()
    {
        yield return null;
        yield return string.Empty;
        yield return " ";
        yield return "a\nb";
        yield return "a\rb";
        yield return "a\u0001b";
        yield return "a\u2028b";
        yield return "a\uD800b";
    }

    private static IEnumerable<TestCaseData> InvalidCalls()
    {
        var valid = new[] { "https://h/a" };
        yield return Case("AddLinksAsync_NullRequest_Throws", a => a.AddLinksAsync(null!));
        yield return Case("AddLinksAsync_EmptyLinks_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = [] }));
        yield return Case("AddLinksAsync_NullLinks_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = null! }));
        yield return Case("AddLinksAsync_NullLink_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = [null!] }));
        yield return Case("AddLinksAsync_BlankLink_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["  "] }));
        yield return Case("AddLinksAsync_EmptyLink_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = [string.Empty] }));
        yield return Case("AddLinksAsync_LinkWithNewline_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a\nb"] }));
        yield return Case("AddLinksAsync_LinkWithCarriageReturn_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a\rb"] }));
        yield return Case("AddLinksAsync_LinkWithControlChar_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a\u0001b"] }));
        yield return Case("AddLinksAsync_LinkWithSpace_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a b"] }));
        yield return Case("AddLinksAsync_LinkWithLineSeparator_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a\u2028b"] }));
        yield return Case("AddLinksAsync_LinkWithLoneSurrogate_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = ["https://a\uD800"] }));
        yield return Case("AddLinksAsync_UnknownPriority_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = valid, Priority = Priority.Unknown }));

        foreach (var text in BadTexts().Where(t => t is not null))
        {
            var bad = text!;
            var label = bad.Length == 0 ? "empty" : bad.Replace("\n", "LF", StringComparison.Ordinal).Replace("\r", "CR", StringComparison.Ordinal).Replace("\u0001", "ctl", StringComparison.Ordinal).Replace("\u2028", "LS", StringComparison.Ordinal).Replace("\uD800", "surrogate", StringComparison.Ordinal).Replace(" ", "blank", StringComparison.Ordinal);
            yield return Case($"AddLinksAsync_PackageName_{label}_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = valid, PackageName = bad }));
            yield return Case($"AddLinksAsync_DestinationFolder_{label}_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = valid, DestinationFolder = bad }));
            yield return Case($"AddLinksAsync_SourceUrl_{label}_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = valid, SourceUrl = bad }));
            yield return Case($"RenameLinkAsync_NewName_{label}_Throws", a => a.RenameLinkAsync(1, bad));
            yield return Case($"RenamePackageAsync_NewName_{label}_Throws", a => a.RenamePackageAsync(1, bad));
        }

        yield return Case("RenameLinkAsync_NullName_Throws", a => a.RenameLinkAsync(1, null!));
        yield return Case("RenamePackageAsync_NullName_Throws", a => a.RenamePackageAsync(1, null!));
        yield return Case("QueryLinksAsync_MaxResultsBelowMinusOne_Throws", a => a.QueryLinksAsync(new LinkQuery { MaxResults = -2 }));
        yield return Case("QueryLinksAsync_NegativeStartAt_Throws", a => a.QueryLinksAsync(new LinkQuery { StartAt = -1 }));
        yield return Case("QueryPackagesAsync_MaxResultsBelowMinusOne_Throws", a => a.QueryPackagesAsync(new PackageQuery { MaxResults = -2 }));
        yield return Case("QueryPackagesAsync_NegativeStartAt_Throws", a => a.QueryPackagesAsync(new PackageQuery { StartAt = -1 }));
        yield return Case("MoveToDownloadListAsync_NullLinkIds_Throws", a => a.MoveToDownloadListAsync(null!, _noIds));
        yield return Case("MoveToDownloadListAsync_NullPackageIds_Throws", a => a.MoveToDownloadListAsync(_noIds, null!));
        yield return Case("RemoveLinksAsync_NullLinkIds_Throws", a => a.RemoveLinksAsync(null!, _noIds));
        yield return Case("RemoveLinksAsync_NullPackageIds_Throws", a => a.RemoveLinksAsync(_noIds, null!));
        yield return Case("SetEnabledAsync_NullLinkIds_Throws", a => a.SetEnabledAsync(true, null!, _noIds));
        yield return Case("SetEnabledAsync_NullPackageIds_Throws", a => a.SetEnabledAsync(true, _noIds, null!));
        yield return Case("SetPriorityAsync_NullLinkIds_Throws", a => a.SetPriorityAsync(Priority.High, null!, _noIds));
        yield return Case("SetPriorityAsync_NullPackageIds_Throws", a => a.SetPriorityAsync(Priority.High, _noIds, null!));
        yield return Case("SetPriorityAsync_UndefinedPriority_Throws", a => a.SetPriorityAsync((Priority)42, _noIds, _noIds));
        yield return Case("AddLinksAsync_UndefinedPriority_Throws", a => a.AddLinksAsync(new AddLinksRequest { Links = valid, Priority = (Priority)42 }));
        yield return Case("SetPriorityAsync_UnknownPriority_Throws", a => a.SetPriorityAsync(Priority.Unknown, _noIds, _noIds));

        yield return Case("CleanupAsync_NullLinkIds_Throws", a => a.CleanupAsync(null!, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Selected));
        yield return Case("CleanupAsync_NullPackageIds_Throws", a => a.CleanupAsync(_noIds, null!, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Selected));
        yield return Case("CleanupAsync_UnknownAction_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.Unknown, CleanupMode.RemoveLinksOnly, SelectionType.Selected));
        yield return Case("CleanupAsync_UndefinedAction_Throws", a => a.CleanupAsync(_noIds, _noIds, (CleanupAction)99, CleanupMode.RemoveLinksOnly, SelectionType.Selected));
        yield return Case("CleanupAsync_UnknownMode_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.Unknown, SelectionType.Selected));
        yield return Case("CleanupAsync_UndefinedMode_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, (CleanupMode)99, SelectionType.Selected));
        yield return Case("CleanupAsync_UnknownSelectionType_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Unknown));
        yield return Case("CleanupAsync_UndefinedSelectionType_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, (SelectionType)99));
        yield return Case("MoveLinksAsync_NullLinkIds_Throws", a => a.MoveLinksAsync(null!, 0, 0));
        yield return Case("MoveLinksAsync_EmptyLinkIds_Throws", a => a.MoveLinksAsync(_noIds, 0, 0));
        yield return Case("MoveLinksAsync_NegativeAfterLinkId_Throws", a => a.MoveLinksAsync(_ids, -1, 0));
        yield return Case("MoveLinksAsync_NegativeDestinationPackageId_Throws", a => a.MoveLinksAsync(_ids, 0, -1));
        yield return Case("MovePackagesAsync_NullPackageIds_Throws", a => a.MovePackagesAsync(null!, 0));
        yield return Case("MovePackagesAsync_EmptyPackageIds_Throws", a => a.MovePackagesAsync(_noIds, 0));
        yield return Case("MovePackagesAsync_NegativeAfterPackageId_Throws", a => a.MovePackagesAsync(_packageIds, -1));
        yield return Case("MoveToNewPackageAsync_NullLinkIds_Throws", a => a.MoveToNewPackageAsync(null!, _noIds, "pkg", "/dl"));
        yield return Case("MoveToNewPackageAsync_NullPackageIds_Throws", a => a.MoveToNewPackageAsync(_noIds, null!, "pkg", "/dl"));
        yield return Case("SplitPackageByHosterAsync_NullLinkIds_Throws", a => a.SplitPackageByHosterAsync(null!, _packageIds));
        yield return Case("SplitPackageByHosterAsync_NullPackageIds_Throws", a => a.SplitPackageByHosterAsync(_ids, null!));
        yield return Case("SetDownloadDirectoryAsync_NullPackageIds_Throws", a => a.SetDownloadDirectoryAsync("/dl", null!));
        yield return Case("StartOnlineStatusCheckAsync_NullLinkIds_Throws", a => a.StartOnlineStatusCheckAsync(null!, _packageIds));
        yield return Case("StartOnlineStatusCheckAsync_NullPackageIds_Throws", a => a.StartOnlineStatusCheckAsync(_ids, null!));

        foreach (var text in BadTexts())
        {
            var bad = text;
            var label = bad is null ? "null" : bad.Length == 0 ? "empty" : bad.Replace("\n", "LF", StringComparison.Ordinal).Replace("\r", "CR", StringComparison.Ordinal).Replace("\u0001", "ctl", StringComparison.Ordinal).Replace("\u2028", "LS", StringComparison.Ordinal).Replace("\uD800", "surrogate", StringComparison.Ordinal).Replace(" ", "blank", StringComparison.Ordinal);
            yield return Case($"SetDownloadDirectoryAsync_Directory_{label}_Throws", a => a.SetDownloadDirectoryAsync(bad!, _noIds));
            yield return Case($"SetVariantAsync_VariantId_{label}_Throws", a => a.SetVariantAsync(1, bad!));
            yield return Case($"AddVariantCopyAsync_VariantId_{label}_Throws", a => a.AddVariantCopyAsync(1, 2, 3, bad!));
            yield return Case($"MoveToNewPackageAsync_NewPackageName_{label}_Throws", a => a.MoveToNewPackageAsync(_noIds, _noIds, bad!, "/dl"));
            yield return Case($"MoveToNewPackageAsync_DownloadPath_{label}_Throws", a => a.MoveToNewPackageAsync(_noIds, _noIds, "pkg", bad!));
            if (bad is not null)
            {
                yield return Case($"SetDownloadPasswordAsync_Password_{label}_Throws", a => a.SetDownloadPasswordAsync(_noIds, _noIds, bad));
            }
        }

        yield return Case("GetVariantsAsync_NegativeLinkId_Throws", a => a.GetVariantsAsync(-1));
        yield return Case("SetVariantAsync_NegativeLinkId_Throws", a => a.SetVariantAsync(-1, "v1"));
        yield return Case("AddVariantCopyAsync_NegativeLinkId_Throws", a => a.AddVariantCopyAsync(-1, 2, 3, "v1"));
        yield return Case("AddVariantCopyAsync_NegativeAfterLinkId_Throws", a => a.AddVariantCopyAsync(1, -1, 3, "v1"));
        yield return Case("AddVariantCopyAsync_NegativeDestinationPackageId_Throws", a => a.AddVariantCopyAsync(1, 2, -1, "v1"));
        yield return Case("SetVariantAsync_VariantId_TooLong_Throws", a => a.SetVariantAsync(1, TextCaps.IdentifierOver));
        yield return Case("AddVariantCopyAsync_VariantId_TooLong_Throws", a => a.AddVariantCopyAsync(1, 2, 3, TextCaps.IdentifierOver));
        yield return Case("SetDownloadPasswordAsync_Password_TooLong_Throws", a => a.SetDownloadPasswordAsync(_noIds, _noIds, TextCaps.LongTextOver));
        yield return Case("SetDownloadPasswordAsync_NullLinkIds_Throws", a => a.SetDownloadPasswordAsync(null!, _noIds, "pw"));
        yield return Case("SetDownloadPasswordAsync_NullPackageIds_Throws", a => a.SetDownloadPasswordAsync(_noIds, null!, "pw"));
        yield return Case("GetDownloadUrlsAsync_NullLinkIds_Throws", a => a.GetDownloadUrlsAsync(null!, _noIds, _contentTypes));
        yield return Case("GetDownloadUrlsAsync_NullPackageIds_Throws", a => a.GetDownloadUrlsAsync(_noIds, null!, _contentTypes));
        yield return Case("GetDownloadUrlsAsync_NullTypes_Throws", a => a.GetDownloadUrlsAsync(_noIds, _noIds, null!));
        yield return Case("GetDownloadUrlsAsync_EmptyTypes_Throws", a => a.GetDownloadUrlsAsync(_noIds, _noIds, []));
        yield return Case("GetDownloadUrlsAsync_DuplicateType_Throws", a => a.GetDownloadUrlsAsync(_noIds, _noIds, [UrlDisplayType.Content, UrlDisplayType.Content]));
        yield return Case("GetDownloadUrlsAsync_UnknownType_Throws", a => a.GetDownloadUrlsAsync(_noIds, _noIds, [UrlDisplayType.Unknown]));
        yield return Case("GetDownloadUrlsAsync_UndefinedType_Throws", a => a.GetDownloadUrlsAsync(_noIds, _noIds, [(UrlDisplayType)99]));
        yield return Case("AbortAsync_NegativeJobId_Throws", a => a.AbortAsync(-1));
        yield return Case("GetChildrenChangedAsync_NegativeWatermark_Throws", a => a.GetChildrenChangedAsync(-1));
        yield return Case("QueryLinkCrawlerJobsAsync_NegativeJobId_Throws", a => a.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { JobIds = [-1] }));
    }

    private static TestCaseData Case(string name, Func<LinkGrabberApi, Task> call)
        => new TestCaseData(call).SetName(name);

    private static (IDeviceChannel Channel, LinkGrabberApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new LinkGrabberApi(channel));
    }

    private static (string Path, IReadOnlyList<JsonElement> Args) Sent(IDeviceChannel channel)
    {
        var call = channel.ReceivedCalls().Single();
        var args = call.GetArguments();
        return ((string)args[0]!, args.OfType<IReadOnlyList<JsonElement>>().Single());
    }

    private static void AssertSent(IDeviceChannel channel, string path, params string[] expectedRawJson)
    {
        var (actualPath, args) = Sent(channel);
        Assert.That(actualPath, Is.EqualTo(Base + path));
        Assert.That(args, Has.Count.EqualTo(expectedRawJson.Length));
        for (var i = 0; i < expectedRawJson.Length; i++)
        {
            var actual = JsonNode.Parse(args[i].GetRawText());
            var expected = JsonNode.Parse(expectedRawJson[i]);
            Assert.That(JsonNode.DeepEquals(actual, expected), Is.True, $"arg {i}: {args[i].GetRawText()} vs {expectedRawJson[i]}");
        }
    }

    private static void ReturnsBool(IDeviceChannel channel, string path, bool? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? false));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void ReturnsInt(IDeviceChannel channel, string path, int? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<int>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? 0));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<int?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void ReturnsJob(IDeviceChannel channel, LinkCollectingJob? value)
        => channel.CallAsync(Base + "addLinks", Arg.Any<JsonTypeInfo<LinkCollectingJob>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        Assert.That(thrown.Message, Is.EqualTo(MyJdownloader.Transport.RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task AddLinksAsync_FullRequest_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsJob(channel, new LinkCollectingJob { Id = 1 });
        var request = new AddLinksRequest
        {
            Links = ["https://h/a", "https://h/b"],
            PackageName = "pkg",
            DestinationFolder = "/dl",
            ExtractPassword = "e p",
            DownloadPassword = "d p",
            SourceUrl = "https://src",
            Priority = Priority.High,
            Autostart = true,
            AutoExtract = false,
            DeepDecrypt = true,
            OverwritePackagizerRules = false,
            AssignJobId = true,
        };

        await api.AddLinksAsync(request);

        AssertSent(
            channel,
            "addLinks",
            /*lang=json,strict*/ """{"links":"https://h/a\nhttps://h/b","packageName":"pkg","destinationFolder":"/dl","extractPassword":"e p","downloadPassword":"d p","sourceUrl":"https://src","priority":"HIGH","autostart":true,"autoExtract":false,"deepDecrypt":true,"overwritePackagizerRules":false,"assignJobID":true}""");
    }

    [Test]
    public async Task AddLinksAsync_MinimalRequest_OmitsNullMembers()
    {
        var (channel, api) = Create();
        ReturnsJob(channel, new LinkCollectingJob { Id = 1 });

        await api.AddLinksAsync(new AddLinksRequest { Links = ["https://h/a"] });

        AssertSent(channel, "addLinks", /*lang=json,strict*/ """{"links":"https://h/a"}""");
    }

    [Test]
    public async Task AddLinksAsync_PasswordWithSpaces_IsAccepted()
    {
        var (channel, api) = Create();
        ReturnsJob(channel, new LinkCollectingJob { Id = 1 });

        await api.AddLinksAsync(new AddLinksRequest { Links = ["https://h/a"], ExtractPassword = "my secret pass", DownloadPassword = " x " });

        AssertSent(channel, "addLinks", /*lang=json,strict*/ """{"links":"https://h/a","extractPassword":"my secret pass","downloadPassword":" x "}""");
    }

    [Test]
    public async Task AddLinksAsync_ReturnsJob()
    {
        var (channel, api) = Create();
        ReturnsJob(channel, new LinkCollectingJob { Id = 42 });

        var job = await api.AddLinksAsync(new AddLinksRequest { Links = ["https://h/a"] });

        Assert.That(job.Id, Is.EqualTo(42L));
    }

    [Test]
    public async Task AddLinksAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsJob(channel, null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.AddLinksAsync(new AddLinksRequest { Links = ["https://h/a"] }));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryLinksAsync_DefaultQuery_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync();

        AssertSent(
            channel,
            "queryLinks",
            /*lang=json,strict*/ """{"availability":true,"bytesTotal":true,"comment":true,"enabled":true,"host":true,"priority":true,"status":true,"url":true,"variantID":true,"variantIcon":true,"variantName":true,"variants":true,"password":false,"maxResults":-1,"startAt":0}""");
    }

    [Test]
    public async Task QueryLinksAsync_DefaultQuery_DoesNotRequestPassword()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync();

        Assert.That(Sent(channel).Args[0].GetRawText(), Does.Contain("\"password\":false"));
    }

    [Test]
    public async Task QueryLinksAsync_QueryWithFilters_SendsUuidArrays()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync(new LinkQuery { PackageUuids = _ids, JobUuids = _packageIds, MaxResults = 10, StartAt = 5, Password = true });

        var args = Sent(channel).Args;
        using var document = JsonDocument.Parse(args[0].GetRawText());
        var root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("packageUUIDs").GetRawText(), Is.EqualTo("[1,2]"));
            Assert.That(root.GetProperty("jobUUIDs").GetRawText(), Is.EqualTo("[3]"));
            Assert.That(root.GetProperty("maxResults").GetInt32(), Is.EqualTo(10));
            Assert.That(root.GetProperty("startAt").GetInt32(), Is.EqualTo(5));
            Assert.That(root.GetProperty("password").GetBoolean(), Is.True);
        });
    }

    [Test]
    public async Task QueryLinksAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryLinks", Arg.Any<JsonTypeInfo<List<CrawledLink>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<CrawledLink>?>(null));

        var links = await api.QueryLinksAsync();

        Assert.That(links, Is.Empty);
    }

    [Test]
    public async Task QueryLinksAsync_Data_ReturnsLinks()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryLinks", Arg.Any<JsonTypeInfo<List<CrawledLink>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<CrawledLink>?>([new CrawledLink { Uuid = 5, PackageUuid = 6 }]));

        var links = await api.QueryLinksAsync();

        Assert.That(links.Select(l => l.Uuid), Is.EqualTo(_singleUuid));
    }

    [Test]
    public async Task QueryPackagesAsync_DefaultQuery_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.QueryPackagesAsync();

        AssertSent(
            channel,
            "queryPackages",
            /*lang=json,strict*/ """{"availableOfflineCount":true,"availableOnlineCount":true,"availableTempUnknownCount":true,"availableUnknownCount":true,"bytesTotal":true,"childCount":true,"comment":true,"enabled":true,"hosts":true,"priority":true,"saveTo":true,"status":true,"maxResults":-1,"startAt":0}""");
    }

    [Test]
    public async Task QueryPackagesAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryPackages", Arg.Any<JsonTypeInfo<List<CrawledPackage>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<CrawledPackage>?>(null));

        var packages = await api.QueryPackagesAsync();

        Assert.That(packages, Is.Empty);
    }

    [Test]
    public async Task QueryLinksAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryLinks", Arg.Any<JsonTypeInfo<List<CrawledLink>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<CrawledLink>?>([null!]));

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryLinksAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryPackagesAsync_NullHost_ThrowsProtocol()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryPackages", Arg.Any<JsonTypeInfo<List<CrawledPackage>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<CrawledPackage>?>([new CrawledPackage { Hosts = [null!] }]));

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryPackagesAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task MoveToDownloadListAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.MoveToDownloadListAsync(_ids, _packageIds);

        AssertSent(channel, "moveToDownloadlist", "[1,2]", "[3]");
    }

    [Test]
    public async Task MoveToDownloadListAsync_EmptyLists_SendsEmptyArrays()
    {
        var (channel, api) = Create();

        await api.MoveToDownloadListAsync(_noIds, _noIds);

        AssertSent(channel, "moveToDownloadlist", "[]", "[]");
    }

    [Test]
    public async Task RemoveLinksAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RemoveLinksAsync(_ids, _packageIds);

        AssertSent(channel, "removeLinks", "[1,2]", "[3]");
    }

    [Test]
    public async Task ClearListAsync_Called_SendsPathAndNoArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "clearList", true);

        var result = await api.ClearListAsync();

        AssertSent(channel, "clearList");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ClearListAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "clearList", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ClearListAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task IsCollectingAsync_Called_SendsPathAndNoArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isCollecting", true);

        var result = await api.IsCollectingAsync();

        AssertSent(channel, "isCollecting");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsCollectingAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isCollecting", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.IsCollectingAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetPackageCountAsync_Called_SendsPathAndNoArgs()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getPackageCount", 7);

        var result = await api.GetPackageCountAsync();

        AssertSent(channel, "getPackageCount");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public async Task GetPackageCountAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getPackageCount", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetPackageCountAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task RenameLinkAsync_NameWithSpace_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RenameLinkAsync(5, "new name.zip");

        AssertSent(channel, "renameLink", "5", /*lang=json,strict*/ "\"new name.zip\"");
    }

    [Test]
    public async Task RenamePackageAsync_Name_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RenamePackageAsync(6, Name);

        AssertSent(channel, "renamePackage", "6", /*lang=json,strict*/ "\"name\"");
    }

    [Test]
    public async Task SetEnabledAsync_Enabled_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.SetEnabledAsync(true, _ids, _packageIds);

        AssertSent(channel, "setEnabled", "true", "[1,2]", "[3]");
    }

    [Test]
    public async Task SetPriorityAsync_High_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.SetPriorityAsync(Priority.High, _ids, _packageIds);

        AssertSent(channel, "setPriority", /*lang=json,strict*/ "\"HIGH\"", "[1,2]", "[3]");
    }

    [Test]
    public async Task CleanupAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.CleanupAsync(_ids, _packageIds, CleanupAction.DeleteDisabled, CleanupMode.RemoveLinksOnly, SelectionType.Selected);

        AssertSent(channel, "cleanup", "[1,2]", "[3]", "\"DELETE_DISABLED\"", "\"REMOVE_LINKS_ONLY\"", "\"SELECTED\"");
    }

    [Test]
    public void CleanupAsync_UnselectedWithEmptyLists_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentException>(() => api.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Unselected));

        Assert.That(thrown!.ParamName, Is.EqualTo("selectionType"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void CleanupAsync_UnselectedWithListChangingAfterCopy_Throws()
    {
        var (channel, api) = Create();
        var changing = new CountOnlyList();

        var thrown = Assert.Throws<ArgumentException>(() => api.CleanupAsync(changing, changing, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Unselected));

        Assert.That(thrown!.ParamName, Is.EqualTo("selectionType"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    private sealed class CountOnlyList : IReadOnlyList<long>
    {
        public int Count => 1;

        public long this[int index] => throw new NotSupportedException();

        public IEnumerator<long> GetEnumerator() => Enumerable.Empty<long>().GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Test]
    public async Task CleanupAsync_UnselectedWithIds_IsSent()
    {
        var (channel, api) = Create();

        await api.CleanupAsync(_ids, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Unselected);

        AssertSent(channel, "cleanup", "[1,2]", "[]", "\"DELETE_FINISHED\"", "\"REMOVE_LINKS_ONLY\"", "\"UNSELECTED\"");
    }

    [Test]
    public async Task CleanupAsync_AllWithEmptyLists_IsSent()
    {
        var (channel, api) = Create();

        await api.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All);

        AssertSent(channel, "cleanup", "[]", "[]", "\"DELETE_FINISHED\"", "\"REMOVE_LINKS_ONLY\"", "\"ALL\"");
    }

    [Test]
    public async Task MoveLinksAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.MoveLinksAsync(_ids, 3, 4);

        AssertSent(channel, "moveLinks", "[1,2]", "3", "4");
    }

    [Test]
    public async Task MovePackagesAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.MovePackagesAsync(_packageIds, 0);

        AssertSent(channel, "movePackages", "[3]", "0");
    }

    [Test]
    public async Task MoveToNewPackageAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.MoveToNewPackageAsync(_ids, _packageIds, "Pkg", "/dl");

        AssertSent(channel, "movetoNewPackage", "[1,2]", "[3]", "\"Pkg\"", "\"/dl\"");
    }

    [Test]
    public async Task SetDownloadDirectoryAsync_Args_SendsDirectoryFirst()
    {
        var (channel, api) = Create();

        await api.SetDownloadDirectoryAsync("/dl", _packageIds);

        AssertSent(channel, "setDownloadDirectory", "\"/dl\"", "[3]");
    }

    [Test]
    public async Task SplitPackageByHosterAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.SplitPackageByHosterAsync(_ids, _packageIds);

        AssertSent(channel, "splitPackageByHoster", "[1,2]", "[3]");
    }

    [Test]
    public async Task StartOnlineStatusCheckAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.StartOnlineStatusCheckAsync(_ids, _packageIds);

        AssertSent(channel, "startOnlineStatusCheck", "[1,2]", "[3]");
    }

    [TestCaseSource(nameof(InvalidCalls))]
    public void InvalidArguments_ThrowWithoutCallingChannel(Func<LinkGrabberApi, Task> call)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => call(api));

        Assert.That(thrown!.Message, Does.Not.Contain("https://").And.Not.Contain("a\nb").And.Not.Contain("a\u0001b").And.Not.Contain("a\uD800b"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void Device_LinkGrabber_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.LinkGrabber, Is.SameAs(device.LinkGrabber));
    }

    [Test]
    public void AddLinksAsync_PackageNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], PackageName = TextCaps.Identifier }));
    }

    [Test]
    public void AddLinksAsync_PackageNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], PackageName = TextCaps.IdentifierOver }), "request");
    }

    [Test]
    public void AddLinksAsync_DestinationFolderAtPathMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], DestinationFolder = TextCaps.Path }));
    }

    [Test]
    public void AddLinksAsync_DestinationFolderOverPathMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], DestinationFolder = TextCaps.PathOver }), "request");
    }

    [Test]
    public void AddLinksAsync_SourceUrlAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], SourceUrl = TextCaps.LongText }));
    }

    [Test]
    public void AddLinksAsync_SourceUrlOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddLinksAsync(new AddLinksRequest { Links = ["https://example.com/a"], SourceUrl = TextCaps.LongTextOver }), "request");
    }

    [Test]
    public void RenameLinkAsync_NewNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.RenameLinkAsync(1, TextCaps.Identifier));
    }

    [Test]
    public void RenameLinkAsync_NewNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.RenameLinkAsync(1, TextCaps.IdentifierOver), "newName");
    }

    [Test]
    public void SetDownloadDirectoryAsync_DirectoryAtPathMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.SetDownloadDirectoryAsync(TextCaps.Path, [1L]));
    }

    [Test]
    public void SetDownloadDirectoryAsync_DirectoryOverPathMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetDownloadDirectoryAsync(TextCaps.PathOver, [1L]), "directory");
    }

    [Test]
    public void MoveToNewPackageAsync_NewPackageNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.MoveToNewPackageAsync([1L], [2L], TextCaps.Identifier, "/downloads"));
    }

    [Test]
    public void MoveToNewPackageAsync_NewPackageNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.MoveToNewPackageAsync([1L], [2L], TextCaps.IdentifierOver, "/downloads"), "newPackageName");
    }

    [Test]
    public void MoveToNewPackageAsync_DownloadPathAtPathMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertSent(channel, () => api.MoveToNewPackageAsync([1L], [2L], "package", TextCaps.Path));
    }

    [Test]
    public void MoveToNewPackageAsync_DownloadPathOverPathMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new LinkGrabberApi(channel);

        TextCaps.AssertRejected(channel, () => api.MoveToNewPackageAsync([1L], [2L], "package", TextCaps.PathOver), "downloadPath");
    }

    [Test]
    public async Task SetDownloadPasswordAsync_Password_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setDownloadPassword", true);

        var result = await api.SetDownloadPasswordAsync(_ids, _packageIds, "pw");

        Assert.That(result, Is.True);
        AssertSent(channel, "setDownloadPassword", "[1,2]", "[3]", "\"pw\"");
    }

    [Test]
    public async Task SetDownloadPasswordAsync_NullPassword_SendsJsonNull()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setDownloadPassword", true);

        await api.SetDownloadPasswordAsync(_ids, _packageIds, null);

        var (_, args) = Sent(channel);
        Assert.That(args[2].GetRawText(), Is.EqualTo("null"));
    }

    [Test]
    public void SetDownloadPasswordAsync_PasswordAtMax_Sends()
    {
        var (channel, api) = Create();

        TextCaps.AssertSent(channel, () => api.SetDownloadPasswordAsync(_ids, _packageIds, TextCaps.LongText));
    }

    [Test]
    public async Task SetDownloadPasswordAsync_EmptyAnswer_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setDownloadPassword", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetDownloadPasswordAsync(_ids, _packageIds, "pw"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void SetDownloadPasswordAsync_InvalidPassword_MessageOmitsPassword()
    {
        var (_, api) = Create();

        var ex = Assert.Throws<ArgumentException>(() => api.SetDownloadPasswordAsync(_ids, _packageIds, "Zq7\u0001Xv9"));

        Assert.That(ex!.ParamName, Is.EqualTo("password"));
        Assert.That(ex.Message, Does.Not.Contain("Zq7").And.Not.Contain("Xv9"));
        Assert.That(ex.ToString(), Does.Not.Contain("Zq7").And.Not.Contain("Xv9"));
    }

    private static void ReturnsVariants(IDeviceChannel channel, List<LinkVariant>? value)
        => channel.CallAsync(Base + "getVariants", Arg.Any<JsonTypeInfo<List<LinkVariant>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsUrlMap(IDeviceChannel channel, Dictionary<string, List<long>>? value)
        => channel.CallAsync(Base + "getDownloadUrls", Arg.Any<JsonTypeInfo<Dictionary<string, List<long>>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    [Test]
    public async Task GetDownloadUrlsAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsUrlMap(channel, []);

        await api.GetDownloadUrlsAsync(_ids, _packageIds, [UrlDisplayType.Content, UrlDisplayType.Origin]);

        AssertSent(channel, "getDownloadUrls", "[1,2]", "[3]", "[\"CONTENT\",\"ORIGIN\"]");
    }

    [Test]
    public async Task GetDownloadUrlsAsync_Map_ReturnsDictionary()
    {
        var (channel, api) = Create();
        ReturnsUrlMap(channel, new Dictionary<string, List<long>> { ["https://h/a"] = [1, 2] });

        var result = await api.GetDownloadUrlsAsync(_ids, _packageIds, _contentTypes);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result["https://h/a"], Is.EqualTo(new long[] { 1, 2 }));
    }

    [Test]
    public async Task GetDownloadUrlsAsync_NullAnswer_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsUrlMap(channel, null);

        var result = await api.GetDownloadUrlsAsync(_ids, _packageIds, _contentTypes);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetDownloadUrlsAsync_NullList_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsUrlMap(channel, new Dictionary<string, List<long>> { ["u"] = null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetDownloadUrlsAsync(_ids, _packageIds, _contentTypes));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetVariantsAsync_Variants_ReturnsList()
    {
        var (channel, api) = Create();
        ReturnsVariants(channel, [new LinkVariant { Id = "v1", Name = "720p", IconKey = "k" }]);

        var result = await api.GetVariantsAsync(5);

        AssertSent(channel, "getVariants", "5");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("v1"));
        Assert.That(result[0].Name, Is.EqualTo("720p"));
    }

    [Test]
    public async Task GetVariantsAsync_NullAnswer_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsVariants(channel, null);

        var result = await api.GetVariantsAsync(5);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetVariantsAsync_NullItem_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsVariants(channel, [null!]);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetVariantsAsync(5));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task SetVariantAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.SetVariantAsync(5, "v1");

        AssertSent(channel, "setVariant", "5", "\"v1\"");
    }

    [Test]
    public async Task AddVariantCopyAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.AddVariantCopyAsync(5, 6, 7, "v1");

        AssertSent(channel, "addVariantCopy", "5", "6", "7", "\"v1\"");
    }

    private static void ReturnsCrawlerJobs(IDeviceChannel channel, List<LinkCrawlerJob>? value)
        => channel.CallAsync(Base + "queryLinkCrawlerJobs", Arg.Any<JsonTypeInfo<List<LinkCrawlerJob>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsLong(IDeviceChannel channel, string path, long? value)
        => channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<long?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsFolders(IDeviceChannel channel, List<string>? value)
        => channel.CallAsync(Base + "getDownloadFolderHistorySelectionBase", Arg.Any<JsonTypeInfo<List<string>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    [Test]
    public async Task QueryLinkCrawlerJobsAsync_NullQuery_SendsEmptyObject()
    {
        var (channel, api) = Create();

        await api.QueryLinkCrawlerJobsAsync();

        AssertSent(channel, "queryLinkCrawlerJobs", "{}");
    }

    [Test]
    public async Task QueryLinkCrawlerJobsAsync_Query_SendsJobIds()
    {
        var (channel, api) = Create();

        await api.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { JobIds = [4], CollectorInfo = true });

        AssertSent(channel, "queryLinkCrawlerJobs", /*lang=json,strict*/ """{"jobIds":[4],"collectorInfo":true}""");
    }

    [Test]
    public async Task QueryLinkCrawlerJobsAsync_Jobs_ReturnsList()
    {
        var (channel, api) = Create();
        ReturnsCrawlerJobs(channel, [new LinkCrawlerJob { JobId = 4, Crawling = true, Crawled = 2 }]);

        var result = await api.QueryLinkCrawlerJobsAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].JobId, Is.EqualTo(4));
        Assert.That(result[0].Crawling, Is.True);
        Assert.That(result[0].Crawled, Is.EqualTo(2));
    }

    [Test]
    public async Task QueryLinkCrawlerJobsAsync_NullItem_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsCrawlerJobs(channel, [null!]);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryLinkCrawlerJobsAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryLinkCrawlerJobsAsync_NullAnswer_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsCrawlerJobs(channel, null);

        var result = await api.QueryLinkCrawlerJobsAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void QueryLinkCrawlerJobsAsync_NegativeJobId_ThrowsOutOfRangeNamingQuery()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.QueryLinkCrawlerJobsAsync(new LinkCrawlerJobQuery { JobIds = [-1] }));

        Assert.That(ex!.ParamName, Is.EqualTo("query"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task AbortAsync_NoJob_SendsNoArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "abort", true);

        var result = await api.AbortAsync();

        Assert.That(result, Is.True);
        AssertSent(channel, "abort");
    }

    [Test]
    public async Task AbortAsync_JobId_SendsJobId()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "abort", true);

        await api.AbortAsync(4);

        AssertSent(channel, "abort", "4");
    }

    [Test]
    public async Task AbortAsync_EmptyAnswer_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "abort", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.AbortAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetChildrenChangedAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "getChildrenChanged", 9);

        var result = await api.GetChildrenChangedAsync(3);

        Assert.That(result, Is.EqualTo(9));
        AssertSent(channel, "getChildrenChanged", "3");
    }

    [Test]
    public async Task GetChildrenChangedAsync_EmptyAnswer_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "getChildrenChanged", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetChildrenChangedAsync(3));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetDownloadFolderHistoryAsync_Folders_ReturnsList()
    {
        var (channel, api) = Create();
        ReturnsFolders(channel, ["/a", "/b"]);

        var result = await api.GetDownloadFolderHistoryAsync();

        Assert.That(result, Is.EqualTo(_folders));
        AssertSent(channel, "getDownloadFolderHistorySelectionBase");
    }

    [Test]
    public async Task GetDownloadFolderHistoryAsync_NullAnswer_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsFolders(channel, null);

        var result = await api.GetDownloadFolderHistoryAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetDownloadFolderHistoryAsync_NullItem_ThrowsUnreadable()
    {
        var (channel, api) = Create();
        ReturnsFolders(channel, [null!]);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetDownloadFolderHistoryAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryLinksAsync_FilterListChangingAfterCheck_SendsCheckedCopy()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync(new LinkQuery { PackageUuids = new ShiftingList() });

        var json = Sent(channel).Args[0].GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"packageUUIDs\":[1]"));
            Assert.That(json, Does.Not.Contain("-1]"));
        });
    }

    [Test]
    public void QueryLinksAsync_NegativePackageFilterId_Throws()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.QueryLinksAsync(new LinkQuery { PackageUuids = [-1] }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("query"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void QueryLinksAsync_NegativeJobFilterId_Throws()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.QueryLinksAsync(new LinkQuery { JobUuids = [-1] }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("query"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void QueryPackagesAsync_NegativeFilterId_Throws()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.QueryPackagesAsync(new PackageQuery { PackageUuids = [-1] }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("query"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase(ContainerType.Dlc, "dlc")]
    [TestCase(ContainerType.Ccf, "ccf")]
    [TestCase(ContainerType.Rsdf, "rsdf")]
    [TestCase(ContainerType.Nzb, "nzb")]
    [TestCase(ContainerType.CrawlJob, "crawljob")]
    [TestCase(ContainerType.Sft, "sft")]
    [TestCase(ContainerType.Sfdl, "sfdl")]
    [TestCase(ContainerType.Metalink, "metalink")]
    [TestCase(ContainerType.Meta4, "meta4")]
    public async Task AddContainerAsync_EachType_SendsWireExtensionAndDataUrl(ContainerType type, string extension)
    {
        var (channel, api) = Create();
        ReturnsContainerJob(channel, new LinkCollectingJob { Id = 1 });
        byte[] bytes = [1, 2, 3, 250, 255];

        await api.AddContainerAsync(type, bytes);

        AssertSent(
            channel,
            "addContainer",
            JsonSerializer.Serialize(extension),
            JsonSerializer.Serialize("data:application/octet-stream;base64," + Convert.ToBase64String(bytes)));
    }

    [Test]
    public async Task AddContainerAsync_ReturnsJob()
    {
        var (channel, api) = Create();
        ReturnsContainerJob(channel, new LinkCollectingJob { Id = 42 });

        var job = await api.AddContainerAsync(ContainerType.Dlc, new byte[] { 1 });

        Assert.That(job!.Id, Is.EqualTo(42L));
    }

    [Test]
    public async Task AddContainerAsync_NullAnswer_ReturnsNull()
    {
        var (channel, api) = Create();
        ReturnsContainerJob(channel, null);

        var job = await api.AddContainerAsync(ContainerType.Dlc, new byte[] { 1 });

        Assert.That(job, Is.Null);
    }

    [TestCase(ContainerType.Unknown)]
    [TestCase((ContainerType)99)]
    public void AddContainerAsync_UnsendableType_ThrowsWithoutCallingChannel(ContainerType type)
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentException>(() => api.AddContainerAsync(type, new byte[] { 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("type"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void AddContainerAsync_EmptyContent_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentException>(() => api.AddContainerAsync(ContainerType.Dlc, ReadOnlyMemory<byte>.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("content"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task AddContainerAsync_ContentAtMaxSize_IsSent()
    {
        var (channel, api) = Create();
        ReturnsContainerJob(channel, new LinkCollectingJob { Id = 1 });

        await api.AddContainerAsync(ContainerType.Dlc, new byte[524_288]);

        Assert.That(channel.ReceivedCalls().Count(), Is.EqualTo(1));
    }

    [Test]
    public void AddContainerAsync_ContentOverMaxSize_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentException>(() => api.AddContainerAsync(ContainerType.Dlc, new byte[524_289]));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("content"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    private static void ReturnsContainerJob(IDeviceChannel channel, LinkCollectingJob? value)
        => channel.CallAsync(Base + "addContainer", Arg.Any<JsonTypeInfo<LinkCollectingJob>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
}
