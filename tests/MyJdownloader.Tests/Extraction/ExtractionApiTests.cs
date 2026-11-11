namespace MyJdownloader.Tests.Extraction;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Extraction;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class ExtractionApiTests
{
    private const string Base = "/extraction/";
    private const string Secret = "s3cr3t-pw";

    private static readonly long[] _ids = [1, 2];
    private static readonly long[] _packageIds = [3];
    private static readonly long[] _noIds = [];
    private static readonly string[] _archiveIds = ["a1", "a2"];

    private static IEnumerable<string> BadTexts()
    {
        yield return string.Empty;
        yield return " ";
        yield return Secret + "\nx";
        yield return Secret + "\rx";
        yield return Secret + "\u0001x";
        yield return Secret + "\u2028x";
        yield return Secret + "\uD800x";
    }

    private static IEnumerable<TestCaseData> InvalidCalls()
    {
        yield return Case("AddArchivePasswordAsync_NullPassword_Throws", a => a.AddArchivePasswordAsync(null!));
        yield return Case("CancelExtractionAsync_NegativeId_Throws", a => a.CancelExtractionAsync(-1));
        yield return Case("GetArchiveInfoAsync_NullLinkIds_Throws", a => a.GetArchiveInfoAsync(null!, _noIds));
        yield return Case("GetArchiveInfoAsync_NullPackageIds_Throws", a => a.GetArchiveInfoAsync(_noIds, null!));
        yield return Case("GetArchiveSettingsAsync_NullList_Throws", a => a.GetArchiveSettingsAsync(null!));
        yield return Case("GetArchiveSettingsAsync_NullItem_Throws", a => a.GetArchiveSettingsAsync([null!]));
        yield return Case("SetArchiveSettingsAsync_NullArchiveId_Throws", a => a.SetArchiveSettingsAsync(null!, new ArchiveSettings()));
        yield return Case("SetArchiveSettingsAsync_NullSettings_Throws", a => a.SetArchiveSettingsAsync("a1", null!));
        yield return Case("SetArchiveSettingsAsync_NullPasswordItem_Throws", a => a.SetArchiveSettingsAsync("a1", new ArchiveSettings { Passwords = [null!] }));
        yield return Case("SetArchiveSettingsAsync_MismatchedSettingsArchiveId_Throws", a => a.SetArchiveSettingsAsync("a1", new ArchiveSettings { ArchiveId = "a2" }));
        yield return Case("StartExtractionNowAsync_NullLinkIds_Throws", a => a.StartExtractionNowAsync(null!, _noIds));
        yield return Case("StartExtractionNowAsync_NullPackageIds_Throws", a => a.StartExtractionNowAsync(_noIds, null!));

        var index = 0;
        foreach (var bad in BadTexts())
        {
            var label = index++;
            yield return Case($"AddArchivePasswordAsync_BadPassword_{label}_Throws", a => a.AddArchivePasswordAsync(bad));
            yield return Case($"GetArchiveSettingsAsync_BadItem_{label}_Throws", a => a.GetArchiveSettingsAsync(["a1", bad]));
            yield return Case($"SetArchiveSettingsAsync_BadArchiveId_{label}_Throws", a => a.SetArchiveSettingsAsync(bad, new ArchiveSettings()));
            yield return Case($"SetArchiveSettingsAsync_BadExtractPath_{label}_Throws", a => a.SetArchiveSettingsAsync("a1", new ArchiveSettings { ExtractPath = bad }));
            yield return Case($"SetArchiveSettingsAsync_BadFinalPassword_{label}_Throws", a => a.SetArchiveSettingsAsync("a1", new ArchiveSettings { FinalPassword = bad }));
            yield return Case($"SetArchiveSettingsAsync_BadPasswordItem_{label}_Throws", a => a.SetArchiveSettingsAsync("a1", new ArchiveSettings { Passwords = ["ok", bad] }));
        }
    }

    private static TestCaseData Case(string name, Func<ExtractionApi, Task> call)
        => new TestCaseData(call).SetName(name);

    private static (IDeviceChannel Channel, ExtractionApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new ExtractionApi(channel));
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

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    private static void AssertToken(IDeviceChannel channel, CancellationToken token)
        => Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(token));

    [Test]
    public async Task AddArchivePasswordAsync_Valid_SendsPathAndRawArgs()
    {
        var (channel, api) = Create();

        await api.AddArchivePasswordAsync(" a\"b ");

        AssertSent(channel, "addArchivePassword", /*lang=json,strict*/ "\" a\\\"b \"");
    }

    [Test]
    public async Task CancelExtractionAsync_Id_SendsIdAndReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "cancelExtraction", true);

        var result = await api.CancelExtractionAsync(7);

        AssertSent(channel, "cancelExtraction", "7");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CancelExtractionAsync_ZeroId_IsAccepted()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "cancelExtraction", false);

        var result = await api.CancelExtractionAsync(0);

        AssertSent(channel, "cancelExtraction", "0");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CancelExtractionAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "cancelExtraction", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.CancelExtractionAsync(7));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void CancelExtractionAsync_NegativeId_MessageOmitsValue()
    {
        var (_, api) = Create();

        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() => api.CancelExtractionAsync(-987654));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("controllerId"));
            Assert.That(thrown.Message, Does.Not.Contain("987654"));
            Assert.That(thrown.ActualValue, Is.Null);
        });
    }

    [Test]
    public void CancelExtractionAsync_NegativeId_ThrowsOutOfRange()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() => api.CancelExtractionAsync(-1));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("controllerId"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task GetArchiveInfoAsync_Ids_SendsPathAndArgsAndReturnsStatuses()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getArchiveInfo", new List<ArchiveStatus> { new() { ArchiveId = "a1" } });

        var statuses = await api.GetArchiveInfoAsync(_ids, _packageIds);

        AssertSent(channel, "getArchiveInfo", "[1,2]", "[3]");
        Assert.That(statuses.Single().ArchiveId, Is.EqualTo("a1"));
    }

    [Test]
    public async Task GetArchiveInfoAsync_EmptyLists_SendsEmptyArrays()
    {
        var (channel, api) = Create();

        await api.GetArchiveInfoAsync(_noIds, _noIds);

        AssertSent(channel, "getArchiveInfo", "[]", "[]");
    }

    [Test]
    public async Task GetArchiveInfoAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<ArchiveStatus>>(channel, "getArchiveInfo", null);

        var statuses = await api.GetArchiveInfoAsync(_ids, _packageIds);

        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public void GetArchiveInfoAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getArchiveInfo", new List<ArchiveStatus> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetArchiveInfoAsync(_ids, _packageIds));
    }

    [Test]
    public async Task GetArchiveSettingsAsync_Ids_SendsNestedArrayAndReturnsSettings()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getArchiveSettings", new List<ArchiveSettings> { new() { ArchiveId = "a1" } });

        var settings = await api.GetArchiveSettingsAsync(_archiveIds);

        AssertSent(channel, "getArchiveSettings", /*lang=json,strict*/ """["a1","a2"]""");
        Assert.That(settings.Single().ArchiveId, Is.EqualTo("a1"));
    }

    [Test]
    public async Task GetArchiveSettingsAsync_EmptyList_SendsEmptyArray()
    {
        var (channel, api) = Create();

        await api.GetArchiveSettingsAsync([]);

        AssertSent(channel, "getArchiveSettings", "[]");
    }

    [Test]
    public async Task GetArchiveSettingsAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<ArchiveSettings>>(channel, "getArchiveSettings", null);

        var settings = await api.GetArchiveSettingsAsync(_archiveIds);

        Assert.That(settings, Is.Empty);
    }

    [Test]
    public void GetArchiveSettingsAsync_NullResultItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getArchiveSettings", new List<ArchiveSettings> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetArchiveSettingsAsync(_archiveIds));
    }

    [Test]
    public async Task GetQueueAsync_Called_SendsPathAndNoArgsAndReturnsStatuses()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getQueue", new List<ArchiveStatus> { new() { ArchiveId = "q1" } });

        var queue = await api.GetQueueAsync();

        AssertSent(channel, "getQueue");
        Assert.That(queue.Single().ArchiveId, Is.EqualTo("q1"));
    }

    [Test]
    public async Task GetQueueAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<ArchiveStatus>>(channel, "getQueue", null);

        var queue = await api.GetQueueAsync();

        Assert.That(queue, Is.Empty);
    }

    [Test]
    public void GetQueueAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getQueue", new List<ArchiveStatus> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetQueueAsync());
    }

    [Test]
    public async Task SetArchiveSettingsAsync_FullSettings_SendsIdAndSettings()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setArchiveSettings", true);
        var settings = new ArchiveSettings
        {
            ArchiveId = "a1",
            AutoExtract = true,
            ExtractPath = "/dl/x",
            FinalPassword = "fp",
            Passwords = ["p1", "p2"],
            RemoveDownloadLinksAfterExtraction = false,
            RemoveFilesAfterExtraction = true,
        };

        var result = await api.SetArchiveSettingsAsync("a1", settings);

        AssertSent(
            channel,
            "setArchiveSettings",
            /*lang=json,strict*/ "\"a1\"",
            /*lang=json,strict*/ """{"archiveId":"a1","autoExtract":true,"extractPath":"/dl/x","finalPassword":"fp","passwords":["p1","p2"],"removeDownloadLinksAfterExtraction":false,"removeFilesAfterExtraction":true}""");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SetArchiveSettingsAsync_EmptySettings_OmitsNullMembers()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setArchiveSettings", true);

        await api.SetArchiveSettingsAsync("a1", new ArchiveSettings { AutoExtract = false });

        AssertSent(channel, "setArchiveSettings", "\"a1\"", /*lang=json,strict*/ """{"autoExtract":false}""");
    }

    [Test]
    public async Task SetArchiveSettingsAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setArchiveSettings", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetArchiveSettingsAsync("a1", new ArchiveSettings()));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void SetArchiveSettingsAsync_NullSettings_ThrowsWithParamName()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.SetArchiveSettingsAsync("a1", null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("settings"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void SetArchiveSettingsAsync_BadFinalPassword_MessageDoesNotContainPassword()
    {
        var (_, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SetArchiveSettingsAsync("a1", new ArchiveSettings { FinalPassword = Secret + "\n" }));

        Assert.That(thrown!.Message, Does.Not.Contain(Secret));
    }

    [Test]
    public async Task StartExtractionNowAsync_Ids_SendsPathAndArgsAndReturnsMap()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "startExtractionNow", new Dictionary<string, bool?> { ["a1"] = true, ["a2"] = null });

        var result = await api.StartExtractionNowAsync(_ids, _packageIds);

        AssertSent(channel, "startExtractionNow", "[1,2]", "[3]");
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result["a1"], Is.True);
            Assert.That(result["a2"], Is.Null);
        });
    }

    [Test]
    public async Task StartExtractionNowAsync_EmptyLists_SendsEmptyArrays()
    {
        var (channel, api) = Create();

        await api.StartExtractionNowAsync(_noIds, _noIds);

        AssertSent(channel, "startExtractionNow", "[]", "[]");
    }

    [Test]
    public async Task StartExtractionNowAsync_NullData_ReturnsEmptyDictionary()
    {
        var (channel, api) = Create();
        ReturnsRef<Dictionary<string, bool?>>(channel, "startExtractionNow", null);

        var result = await api.StartExtractionNowAsync(_ids, _packageIds);

        Assert.That(result, Is.Empty);
    }

    [TestCaseSource(nameof(InvalidCalls))]
    public void InvalidArguments_ThrowWithoutCallingChannel(Func<ExtractionApi, Task> call)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => call(api));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Does.Not.Contain(Secret));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task AddArchivePasswordAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.AddArchivePasswordAsync("pw", cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task CancelExtractionAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "cancelExtraction", true);

        await api.CancelExtractionAsync(1, cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task GetArchiveInfoAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.GetArchiveInfoAsync(_ids, _packageIds, cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task GetArchiveSettingsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.GetArchiveSettingsAsync(_archiveIds, cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task GetQueueAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.GetQueueAsync(cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task SetArchiveSettingsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "setArchiveSettings", true);

        await api.SetArchiveSettingsAsync("a1", new ArchiveSettings(), cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public async Task StartExtractionNowAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.StartExtractionNowAsync(_ids, _packageIds, cts.Token);

        AssertToken(channel, cts.Token);
    }

    [Test]
    public void Device_Extraction_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Extraction, Is.SameAs(device.Extraction));
    }

    [Test]
    public void AddArchivePasswordAsync_PasswordAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.AddArchivePasswordAsync(TextCaps.LongText));
    }

    [Test]
    public void AddArchivePasswordAsync_PasswordOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddArchivePasswordAsync(TextCaps.LongTextOver), "password");
    }

    [Test]
    public void GetArchiveSettingsAsync_ArchiveIdItemAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.GetArchiveSettingsAsync([TextCaps.Identifier]));
    }

    [Test]
    public void GetArchiveSettingsAsync_ArchiveIdItemOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetArchiveSettingsAsync([TextCaps.IdentifierOver]), "archiveIds");
    }

    [Test]
    public void SetArchiveSettingsAsync_ArchiveIdAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.SetArchiveSettingsAsync(TextCaps.Identifier, new ArchiveSettings()));
    }

    [Test]
    public void SetArchiveSettingsAsync_ArchiveIdOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetArchiveSettingsAsync(TextCaps.IdentifierOver, new ArchiveSettings()), "archiveId");
    }

    [Test]
    public void SetArchiveSettingsAsync_ExtractPathAtPathMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { ExtractPath = TextCaps.Path }));
    }

    [Test]
    public void SetArchiveSettingsAsync_ExtractPathOverPathMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { ExtractPath = TextCaps.PathOver }), "settings");
    }

    [Test]
    public void SetArchiveSettingsAsync_FinalPasswordAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { FinalPassword = TextCaps.LongText }));
    }

    [Test]
    public void SetArchiveSettingsAsync_FinalPasswordOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { FinalPassword = TextCaps.LongTextOver }), "settings");
    }

    [Test]
    public void SetArchiveSettingsAsync_PasswordItemAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertSent(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { Passwords = [TextCaps.LongText] }));
    }

    [Test]
    public void SetArchiveSettingsAsync_PasswordItemOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtractionApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetArchiveSettingsAsync("archive", new ArchiveSettings { Passwords = [TextCaps.LongTextOver] }), "settings");
    }
}
