namespace MyJdownloader.Tests.LinkGrabber;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.LinkGrabber;
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
}
