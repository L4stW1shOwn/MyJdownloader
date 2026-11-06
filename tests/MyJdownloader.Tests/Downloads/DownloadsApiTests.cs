namespace MyJdownloader.Tests.Downloads;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Downloads;
using NSubstitute;

[TestFixture]
public sealed class DownloadsApiTests
{
    private const string Base = "/downloadsV2/";

    private static readonly long[] _ids = [1, 2];
    private static readonly long[] _packageIds = [3];
    private static readonly long[] _noIds = [];

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
        yield return Case("RemoveLinksAsync_NullLinkIds_Throws", a => a.RemoveLinksAsync(null!, _noIds));
        yield return Case("RemoveLinksAsync_NullPackageIds_Throws", a => a.RemoveLinksAsync(_noIds, null!));
        yield return Case("CleanupAsync_NullLinkIds_Throws", a => a.CleanupAsync(null!, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All));
        yield return Case("CleanupAsync_NullPackageIds_Throws", a => a.CleanupAsync(_noIds, null!, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All));
        yield return Case("SetEnabledAsync_NullLinkIds_Throws", a => a.SetEnabledAsync(true, null!, _noIds));
        yield return Case("SetEnabledAsync_NullPackageIds_Throws", a => a.SetEnabledAsync(true, _noIds, null!));
        yield return Case("SetPriorityAsync_NullLinkIds_Throws", a => a.SetPriorityAsync(Priority.High, null!, _noIds));
        yield return Case("SetPriorityAsync_NullPackageIds_Throws", a => a.SetPriorityAsync(Priority.High, _noIds, null!));
        yield return Case("ForceDownloadAsync_NullLinkIds_Throws", a => a.ForceDownloadAsync(null!, _noIds));
        yield return Case("ForceDownloadAsync_NullPackageIds_Throws", a => a.ForceDownloadAsync(_noIds, null!));
        yield return Case("ResetLinksAsync_NullLinkIds_Throws", a => a.ResetLinksAsync(null!, _noIds));
        yield return Case("ResetLinksAsync_NullPackageIds_Throws", a => a.ResetLinksAsync(_noIds, null!));
        yield return Case("ResumeLinksAsync_NullLinkIds_Throws", a => a.ResumeLinksAsync(null!, _noIds));
        yield return Case("ResumeLinksAsync_NullPackageIds_Throws", a => a.ResumeLinksAsync(_noIds, null!));
        yield return Case("SetDownloadDirectoryAsync_NullPackageIds_Throws", a => a.SetDownloadDirectoryAsync("/dl", null!));
        yield return Case("MoveToNewPackageAsync_NullLinkIds_Throws", a => a.MoveToNewPackageAsync(null!, _noIds, "pkg", "/dl"));
        yield return Case("MoveToNewPackageAsync_NullPackageIds_Throws", a => a.MoveToNewPackageAsync(_noIds, null!, "pkg", "/dl"));

        foreach (var text in BadTexts())
        {
            var bad = text;
            var label = bad is null ? "null" : bad.Length == 0 ? "empty" : bad.Replace("\n", "LF", StringComparison.Ordinal).Replace("\r", "CR", StringComparison.Ordinal).Replace("\u0001", "ctl", StringComparison.Ordinal).Replace("\u2028", "LS", StringComparison.Ordinal).Replace("\uD800", "surrogate", StringComparison.Ordinal).Replace(" ", "blank", StringComparison.Ordinal);
            yield return Case($"RenameLinkAsync_NewName_{label}_Throws", a => a.RenameLinkAsync(1, bad!));
            yield return Case($"RenamePackageAsync_NewName_{label}_Throws", a => a.RenamePackageAsync(1, bad!));
            yield return Case($"SetDownloadDirectoryAsync_Directory_{label}_Throws", a => a.SetDownloadDirectoryAsync(bad!, _noIds));
            yield return Case($"MoveToNewPackageAsync_NewPackageName_{label}_Throws", a => a.MoveToNewPackageAsync(_noIds, _noIds, bad!, "/dl"));
            yield return Case($"MoveToNewPackageAsync_DownloadPath_{label}_Throws", a => a.MoveToNewPackageAsync(_noIds, _noIds, "pkg", bad!));
        }

        yield return Case("QueryLinksAsync_MaxResultsBelowMinusOne_Throws", a => a.QueryLinksAsync(new DownloadLinkQuery { MaxResults = -2 }));
        yield return Case("QueryLinksAsync_NegativeStartAt_Throws", a => a.QueryLinksAsync(new DownloadLinkQuery { StartAt = -1 }));
        yield return Case("QueryPackagesAsync_MaxResultsBelowMinusOne_Throws", a => a.QueryPackagesAsync(new DownloadPackageQuery { MaxResults = -2 }));
        yield return Case("QueryPackagesAsync_NegativeStartAt_Throws", a => a.QueryPackagesAsync(new DownloadPackageQuery { StartAt = -1 }));
        yield return Case("SetPriorityAsync_UnknownPriority_Throws", a => a.SetPriorityAsync(Priority.Unknown, _noIds, _noIds));
        yield return Case("SetPriorityAsync_UndefinedPriority_Throws", a => a.SetPriorityAsync((Priority)42, _noIds, _noIds));
        yield return Case("CleanupAsync_UnknownAction_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.Unknown, CleanupMode.RemoveLinksOnly, SelectionType.All));
        yield return Case("CleanupAsync_UndefinedAction_Throws", a => a.CleanupAsync(_noIds, _noIds, (CleanupAction)99, CleanupMode.RemoveLinksOnly, SelectionType.All));
        yield return Case("CleanupAsync_UnknownMode_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.Unknown, SelectionType.All));
        yield return Case("CleanupAsync_UndefinedMode_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, (CleanupMode)99, SelectionType.All));
        yield return Case("CleanupAsync_UnknownSelectionType_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.Unknown));
        yield return Case("CleanupAsync_UndefinedSelectionType_Throws", a => a.CleanupAsync(_noIds, _noIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, (SelectionType)99));
    }

    private static TestCaseData Case(string name, Func<DownloadsApi, Task> call)
        => new TestCaseData(call).SetName(name);

    private static (IDeviceChannel Channel, DownloadsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new DownloadsApi(channel));
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

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        Assert.That(thrown.Message, Is.EqualTo(MyJdownloader.Transport.RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task QueryLinksAsync_DefaultQuery_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "queryLinks"));
        Assert.That(args, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryLinksAsync_DefaultQuery_DoesNotRequestPassword()
    {
        var (channel, api) = Create();

        await api.QueryLinksAsync();

        Assert.That(Sent(channel).Args[0].GetRawText(), Does.Contain("\"password\":false"));
    }

    [Test]
    public async Task QueryLinksAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryLinks", Arg.Any<JsonTypeInfo<List<DownloadLink>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<DownloadLink>?>(null));

        var links = await api.QueryLinksAsync();

        Assert.That(links, Is.Empty);
    }

    [Test]
    public async Task QueryPackagesAsync_DefaultQuery_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.QueryPackagesAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "queryPackages"));
        Assert.That(args, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryPackagesAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryPackages", Arg.Any<JsonTypeInfo<List<DownloadPackage>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<DownloadPackage>?>(null));

        var packages = await api.QueryPackagesAsync();

        Assert.That(packages, Is.Empty);
    }

    [Test]
    public async Task QueryLinksAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryLinks", Arg.Any<JsonTypeInfo<List<DownloadLink>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<DownloadLink>?>([null!]));

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryLinksAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryPackagesAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryPackages", Arg.Any<JsonTypeInfo<List<DownloadPackage>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<DownloadPackage>?>([null!]));

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryPackagesAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryPackagesAsync_NullHost_ThrowsProtocol()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "queryPackages", Arg.Any<JsonTypeInfo<List<DownloadPackage>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<DownloadPackage>?>([new DownloadPackage { Hosts = [null!] }]));

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryPackagesAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetPackageCountAsync_Called_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "packageCount", 7);

        await api.GetPackageCountAsync();

        AssertSent(channel, "packageCount");
    }

    [Test]
    public async Task GetPackageCountAsync_ReturnsCount()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "packageCount", 7);

        var result = await api.GetPackageCountAsync();

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public async Task GetPackageCountAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "packageCount", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetPackageCountAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task RemoveLinksAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RemoveLinksAsync(_ids, _packageIds);

        AssertSent(channel, "removeLinks", "[1,2]", "[3]");
    }

    [Test]
    public async Task RemoveLinksAsync_EmptyLists_SendsEmptyArrays()
    {
        var (channel, api) = Create();

        await api.RemoveLinksAsync(_noIds, _noIds);

        AssertSent(channel, "removeLinks", "[]", "[]");
    }

    [Test]
    public async Task CleanupAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.CleanupAsync(_ids, _packageIds, CleanupAction.DeleteFinished, CleanupMode.RemoveLinksOnly, SelectionType.All);

        AssertSent(channel, "cleanup", "[1,2]", "[3]", "\"DELETE_FINISHED\"", "\"REMOVE_LINKS_ONLY\"", "\"ALL\"");
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

        AssertSent(channel, "setPriority", "\"HIGH\"", "[1,2]", "[3]");
    }

    [Test]
    public async Task RenameLinkAsync_Name_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RenameLinkAsync(5, "x");

        AssertSent(channel, "renameLink", "5", "\"x\"");
    }

    [Test]
    public async Task RenamePackageAsync_Name_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RenamePackageAsync(6, "x");

        AssertSent(channel, "renamePackage", "6", "\"x\"");
    }

    [Test]
    public async Task ForceDownloadAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "forceDownload", true);
        await api.ForceDownloadAsync(_ids, _packageIds);

        AssertSent(channel, "forceDownload", "[1,2]", "[3]");
    }

    [Test]
    public async Task ResetLinksAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.ResetLinksAsync(_ids, _packageIds);

        AssertSent(channel, "resetLinks", "[1,2]", "[3]");
    }

    [Test]
    public async Task ResumeLinksAsync_Ids_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.ResumeLinksAsync(_ids, _packageIds);

        AssertSent(channel, "resumeLinks", "[1,2]", "[3]");
    }

    [Test]
    public async Task SetDownloadDirectoryAsync_Directory_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.SetDownloadDirectoryAsync("/dl", _packageIds);

        AssertSent(channel, "setDownloadDirectory", "\"/dl\"", "[3]");
    }

    [Test]
    public async Task MoveToNewPackageAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.MoveToNewPackageAsync(_ids, _packageIds, "pkg", "/dl");

        AssertSent(channel, "movetoNewPackage", "[1,2]", "[3]", "\"pkg\"", "\"/dl\"");
    }

    [Test]
    public async Task ForceDownloadAsync_ReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "forceDownload", true);

        var result = await api.ForceDownloadAsync(_ids, _packageIds);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ForceDownloadAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "forceDownload", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ForceDownloadAsync(_ids, _packageIds));

        AssertDeviceUnreadable(thrown);
    }

    [TestCaseSource(nameof(InvalidCalls))]
    public void InvalidArguments_ThrowWithoutCallingChannel(Func<DownloadsApi, Task> call)
    {
        var (channel, api) = Create();

        Assert.Catch<ArgumentException>(() => call(api));

        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void Device_Downloads_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Downloads, Is.SameAs(device.Downloads));
    }
}
