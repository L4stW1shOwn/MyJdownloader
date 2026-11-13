namespace MyJdownloader.Tests.Dialogs;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Dialogs;
using MyJdownloader.Errors;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class DialogsApiTests
{
    private const string Base = "/dialogs/";

    private static (IDeviceChannel Channel, DialogsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new DialogsApi(channel));
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
        Assert.That(args.Select(a => a.GetRawText()), Is.EqualTo(expectedRawJson));
    }

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static JsonElement Json(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    [Test]
    public async Task ListAsync_Data_ReturnsIdsAndSendsNoArgs()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new long[] { 3, 9 });

        var ids = await api.ListAsync();

        AssertSent(channel, "list");
        Assert.That(ids, Is.EqualTo(new long[] { 3, 9 }));
    }

    [Test]
    public async Task ListAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<long[]>(channel, "list", null);

        var ids = await api.ListAsync();

        Assert.That(ids, Is.Empty);
    }

    [Test]
    public async Task GetAsync_Defaults_SendsIdWithoutIconAndWithProperties()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "get", new DialogInfo { Type = "t" });

        await api.GetAsync(42);

        AssertSent(channel, "get", "42", "false", "true");
    }

    [Test]
    public async Task GetAsync_Flags_SendsFlagsInOrder()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "get", new DialogInfo());

        await api.GetAsync(7, includeIcon: true, includeProperties: false);

        AssertSent(channel, "get", "7", "true", "false");
    }

    [Test]
    public async Task GetAsync_Data_ReturnsDialog()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "get", new DialogInfo { Type = "ConfirmDialog" });

        var info = await api.GetAsync(42);

        Assert.That(info.Type, Is.EqualTo("ConfirmDialog"));
    }

    [Test]
    public async Task GetAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef<DialogInfo>(channel, "get", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetAsync(42));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task GetTypeInfoAsync_Data_SendsTypeAndReturnsInfo()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getTypeInfo", new DialogTypeInfo { Out = new Dictionary<string, string> { ["ok"] = "boolean" } });

        var info = await api.GetTypeInfoAsync("ConfirmDialog");

        AssertSent(channel, "getTypeInfo", "\"ConfirmDialog\"");
        Assert.That(info.Out!["ok"], Is.EqualTo("boolean"));
    }

    [Test]
    public async Task GetTypeInfoAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef<DialogTypeInfo>(channel, "getTypeInfo", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetTypeInfoAsync("ConfirmDialog"));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public void GetTypeInfoAsync_Null_ThrowsArgumentNull()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentNullException>(() => api.GetTypeInfoAsync(null!));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("  ")]
    [TestCase("a\nb")]
    public void GetTypeInfoAsync_InvalidText_ThrowsArgument(string dialogType)
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentException>(() => api.GetTypeInfoAsync(dialogType));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task AnswerAsync_Data_SendsIdAndObjectJson()
    {
        var (channel, api) = Create();
        var data = new Dictionary<string, JsonElement>
        {
            ["ok"] = Json("true"),
            ["text"] = Json("\"a b\""),
            ["count"] = Json("3"),
        };

        await api.AnswerAsync(5, data);

        AssertSent(channel, "answer", "5", /*lang=json,strict*/ "{\"ok\":true,\"text\":\"a b\",\"count\":3}");
    }

    [Test]
    public async Task AnswerAsync_NestedObjectAndArray_SendsValuesUnchanged()
    {
        var (channel, api) = Create();
        var data = new Dictionary<string, JsonElement>
        {
            ["nested"] = Json(/*lang=json,strict*/ "{\"a\":{\"b\":[1,2]},\"c\":null}"),
            ["list"] = Json(/*lang=json,strict*/ "[\"x\",{\"y\":false},3]"),
        };

        await api.AnswerAsync(5, data);

        var (_, args) = Sent(channel);
        Assert.That(args[1].GetProperty("nested").GetRawText(), Is.EqualTo(/*lang=json,strict*/ "{\"a\":{\"b\":[1,2]},\"c\":null}"));
        Assert.That(args[1].GetProperty("list").GetRawText(), Is.EqualTo(/*lang=json,strict*/ "[\"x\",{\"y\":false},3]"));
    }

    [Test]
    public async Task AnswerAsync_MapMutatedAfterCall_SendsOriginalSnapshot()
    {
        var (channel, api) = Create();
        var data = new Dictionary<string, JsonElement> { ["ok"] = Json("true") };

        var task = api.AnswerAsync(5, data);
        data["ok"] = Json("false");
        data["extra"] = Json("1");
        await task;

        var (_, args) = Sent(channel);
        Assert.That(args[1].GetRawText(), Is.EqualTo(/*lang=json,strict*/ "{\"ok\":true}"));
    }

    [Test]
    public async Task AnswerAsync_EmptyMap_SendsEmptyObject()
    {
        var (channel, api) = Create();

        await api.AnswerAsync(5, new Dictionary<string, JsonElement>());

        AssertSent(channel, "answer", "5", "{}");
    }

    [Test]
    public void AnswerAsync_NullMap_ThrowsArgumentNull()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentNullException>(() => api.AnswerAsync(5, null!));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("a\u0001b")]
    public void AnswerAsync_InvalidKey_ThrowsArgument(string key)
    {
        var (channel, api) = Create();
        var data = new Dictionary<string, JsonElement> { [key] = Json("true") };

        Assert.Throws<ArgumentException>(() => api.AnswerAsync(5, data));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void AnswerAsync_UndefinedValue_ThrowsArgument()
    {
        var (channel, api) = Create();
        var data = new Dictionary<string, JsonElement> { ["ok"] = default };

        Assert.Throws<ArgumentException>(() => api.AnswerAsync(5, data));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task ListAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new long[] { 1 });

        await api.ListAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef(channel, "get", new DialogInfo());

        await api.GetAsync(1, cancellationToken: cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetTypeInfoAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef(channel, "getTypeInfo", new DialogTypeInfo());

        await api.GetTypeInfoAsync("t", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task AnswerAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.AnswerAsync(1, new Dictionary<string, JsonElement>(), cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Dialogs_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Dialogs, Is.SameAs(device.Dialogs));
    }

    [Test]
    public void GetTypeInfoAsync_DialogTypeAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new DialogsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetTypeInfoAsync(TextCaps.Identifier));
    }

    [Test]
    public void GetTypeInfoAsync_DialogTypeOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new DialogsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetTypeInfoAsync(TextCaps.IdentifierOver), "dialogType");
    }

    [Test]
    public void AnswerAsync_DataKeyAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new DialogsApi(channel);

        TextCaps.AssertSent(channel, () => api.AnswerAsync(1, new Dictionary<string, JsonElement> { [TextCaps.Identifier] = JsonSerializer.SerializeToElement(1) }));
    }

    [Test]
    public void AnswerAsync_DataKeyOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new DialogsApi(channel);

        TextCaps.AssertRejected(channel, () => api.AnswerAsync(1, new Dictionary<string, JsonElement> { [TextCaps.IdentifierOver] = JsonSerializer.SerializeToElement(1) }), "data");
    }
}
