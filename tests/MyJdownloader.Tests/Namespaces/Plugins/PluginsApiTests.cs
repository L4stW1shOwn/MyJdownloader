namespace MyJdownloader.Tests.Plugins;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Config;
using MyJdownloader.Errors;
using MyJdownloader.Plugins;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class PluginsApiTests
{
    private const string Base = "/plugins/";
    private const string Iface = "org.Iface";
    private const string Name = "foo.com";

    private static IEnumerable<string?> BadTexts()
    {
        yield return null;
        yield return string.Empty;
        yield return " ";
        yield return "a\nb";
        yield return "a\rb";
        yield return "a\u0001b";
        yield return "a\uD800b";
    }

    private static string Label(string? bad) =>
        bad is null ? "null" : bad.Length == 0 ? "empty" : bad.Replace("\n", "LF", StringComparison.Ordinal).Replace("\r", "CR", StringComparison.Ordinal).Replace("\u0001", "ctl", StringComparison.Ordinal).Replace("\uD800", "surrogate", StringComparison.Ordinal).Replace(" ", "blank", StringComparison.Ordinal);

    private static IEnumerable<TestCaseData> InvalidCalls()
    {
        var value = JsonSerializer.SerializeToElement("v");

        foreach (var text in BadTexts())
        {
            var bad = text;
            var label = Label(bad);
            yield return Case($"GetAsync_InterfaceName_{label}_Throws", a => a.GetAsync(bad!, Name, "Key"));
            yield return Case($"GetAsync_DisplayName_{label}_Throws", a => a.GetAsync(Iface, bad!, "Key"));
            yield return Case($"GetAsync_Key_{label}_Throws", a => a.GetAsync(Iface, Name, bad!));
            yield return Case($"SetAsync_InterfaceName_{label}_Throws", a => a.SetAsync(bad!, Name, "Key", value));
            yield return Case($"SetAsync_DisplayName_{label}_Throws", a => a.SetAsync(Iface, bad!, "Key", value));
            yield return Case($"SetAsync_Key_{label}_Throws", a => a.SetAsync(Iface, Name, bad!, value));
            yield return Case($"ResetAsync_InterfaceName_{label}_Throws", a => a.ResetAsync(bad!, Name, "Key"));
            yield return Case($"ResetAsync_DisplayName_{label}_Throws", a => a.ResetAsync(Iface, bad!, "Key"));
            yield return Case($"ResetAsync_Key_{label}_Throws", a => a.ResetAsync(Iface, Name, bad!));
            yield return Case($"GetPluginRegexAsync_Url_{label}_Throws", a => a.GetPluginRegexAsync(bad!));

            if (bad is null)
            {
                continue;
            }

            yield return Case($"QueryAsync_Pattern_{label}_Throws", a => a.QueryAsync(new ConfigQuery { Pattern = bad }));
            yield return Case($"QueryAsync_ConfigInterface_{label}_Throws", a => a.QueryAsync(new ConfigQuery { ConfigInterface = bad }));
        }

        yield return Case("SetAsync_UndefinedValue_Throws", a => a.SetAsync(Iface, Name, "Key", default));
    }

    private static TestCaseData Case(string name, Func<PluginsApi, Task> call)
        => new TestCaseData(call).SetName(name);

    private static (IDeviceChannel Channel, PluginsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new PluginsApi(channel));
    }

    private static (string Path, IReadOnlyList<JsonElement> Args) Sent(IDeviceChannel channel)
    {
        var call = channel.ReceivedCalls().Single();
        var args = call.GetArguments();
        return ((string)args[0]!, args.OfType<IReadOnlyList<JsonElement>>().Single());
    }

    private static void ReturnsElement(IDeviceChannel channel, string path, JsonElement? value) =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<JsonElement?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsBool(IDeviceChannel channel, string path, bool? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? false));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    private static async Task<string> SentSetValueAsync(JsonElement value)
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);

        await api.SetAsync(Iface, Name, "Key", value);

        return Sent(channel).Args[3].GetString()!;
    }

    [Test]
    public async Task GetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        await api.GetAsync(Iface, Name, "Key");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/get"));
            Assert.That(args, Has.Count.EqualTo(3));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo(Name));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
        });
    }

    [Test]
    public async Task SetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);

        await api.SetAsync(Iface, Name, "Key", JsonSerializer.SerializeToElement(42));

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/set"));
            Assert.That(args, Has.Count.EqualTo(4));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo(Name));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
            Assert.That(args[3].GetString(), Is.EqualTo("42"));
        });
    }

    [Test]
    public async Task ResetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", true);

        await api.ResetAsync(Iface, Name, "Key");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/reset"));
            Assert.That(args, Has.Count.EqualTo(3));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo(Name));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
        });
    }

    [Test]
    public async Task ListAsync_Query_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginInfo>>(channel, "list", null);

        await api.ListAsync(new PluginsQuery { Pattern = false, Version = true });

        var (path, args) = Sent(channel);
        var raw = args.Single().GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/list"));
            Assert.That(raw, Does.Contain("\"pattern\":false"));
            Assert.That(raw, Does.Contain("\"version\":true"));
        });
    }

    [Test]
    public async Task ListAsync_NullQuery_SendsDefaultQuery()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginInfo>>(channel, "list", null);

        await api.ListAsync();

        Assert.That(Sent(channel).Args.Single().GetRawText(), Is.EqualTo(/*lang=json,strict*/ """{"pattern":true,"version":true}"""));
    }

    [Test]
    public async Task QueryAsync_Query_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginConfigEntry>>(channel, "query", null);

        await api.QueryAsync(new ConfigQuery { ConfigInterface = Iface, Pattern = "dl.*" });

        var (path, args) = Sent(channel);
        var raw = args.Single().GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/query"));
            Assert.That(raw, Does.Contain("\"configInterface\":\"org.Iface\""));
            Assert.That(raw, Does.Contain("\"pattern\":\"dl.*\""));
        });
    }

    [Test]
    public async Task QueryAsync_DefaultQuery_DoesNotRequestValues()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginConfigEntry>>(channel, "query", null);

        await api.QueryAsync();

        Assert.That(Sent(channel).Args[0].GetRawText(), Does.Contain("\"values\":false"));
    }

    [Test]
    public async Task GetAllPluginRegexAsync_NoArgs_SendsPathAndNoArgs()
    {
        var (channel, api) = Create();
        ReturnsRef<Dictionary<string, List<string>>>(channel, "getAllPluginRegex", null);

        await api.GetAllPluginRegexAsync();

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/getAllPluginRegex"));
            Assert.That(args, Is.Empty);
        });
    }

    [Test]
    public async Task GetPluginRegexAsync_Url_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsRef<List<string>>(channel, "getPluginRegex", null);

        await api.GetPluginRegexAsync("https://foo.com/a");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/plugins/getPluginRegex"));
            Assert.That(args, Has.Count.EqualTo(1));
            Assert.That(args[0].GetString(), Is.EqualTo("https://foo.com/a"));
        });
    }

    [Test]
    public void SetAsync_ValueOverRequestCap_ThrowsWithoutSending()
    {
        var (channel, api) = Create();
        var value = JsonSerializer.SerializeToElement(new string('a', 1_048_575));

        var ex = Assert.Throws<ArgumentException>(() => api.SetAsync(Iface, Name, "Key", value));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
            Assert.That(ex.Message, Does.StartWith("The value must not exceed 1048576 characters."));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task SetAsync_ValueAtRequestCap_ReachesChannel()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);
        var value = JsonSerializer.SerializeToElement(new string('a', 1_048_574));

        await api.SetAsync(Iface, Name, "Key", value);

        Assert.That(Sent(channel).Path, Is.EqualTo("/plugins/set"));
    }

    [Test]
    public async Task SetAsync_String_SendsQuotedJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement("foo")), Is.EqualTo("\"foo\""));

    [Test]
    public async Task SetAsync_Boolean_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement(true)), Is.EqualTo("true"));

    [Test]
    public async Task SetAsync_Object_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonDocument.Parse(/*lang=json,strict*/ "{\"k\":1}").RootElement), Is.EqualTo(/*lang=json,strict*/ "{\"k\":1}"));

    [Test]
    public async Task GetAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", JsonSerializer.SerializeToElement("/dl"));

        var result = await api.GetAsync(Iface, Name, "Key");

        Assert.That(result.GetString(), Is.EqualTo("/dl"));
    }

    [Test]
    public async Task GetAsync_NullData_ReturnsNullElement()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        var result = await api.GetAsync(Iface, Name, "Key");

        Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task SetAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetAsync(Iface, Name, "Key", JsonSerializer.SerializeToElement(1)));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task ResetAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ResetAsync(Iface, Name, "Key"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task SetAsync_True_ReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);

        var result = await api.SetAsync(Iface, Name, "Key", JsonSerializer.SerializeToElement(1));

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ResetAsync_True_ReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", true);

        var result = await api.ResetAsync(Iface, Name, "Key");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ListAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginInfo>>(channel, "list", null);

        var plugins = await api.ListAsync();

        Assert.That(plugins, Is.Empty);
    }

    [Test]
    public async Task ListAsync_Items_ReturnsItems()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<PluginInfo> { new() { DisplayName = Name } });

        var plugins = await api.ListAsync();

        Assert.That(plugins.Single().DisplayName, Is.EqualTo(Name));
    }

    [Test]
    public async Task ListAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<PluginInfo> { null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ListAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task QueryAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<PluginConfigEntry>>(channel, "query", null);

        var entries = await api.QueryAsync();

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task QueryAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "query", new List<PluginConfigEntry> { null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.QueryAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetAllPluginRegexAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<Dictionary<string, List<string>>>(channel, "getAllPluginRegex", null);

        var map = await api.GetAllPluginRegexAsync();

        Assert.That(map, Is.Empty);
    }

    [Test]
    public async Task GetAllPluginRegexAsync_Map_ReturnsMap()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getAllPluginRegex", new Dictionary<string, List<string>> { ["foo"] = ["a", "b"] });

        var map = await api.GetAllPluginRegexAsync();

        Assert.That(map["foo"], Is.EqualTo(new List<string> { "a", "b" }));
    }

    [Test]
    public async Task GetAllPluginRegexAsync_NullListValue_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getAllPluginRegex", new Dictionary<string, List<string>> { ["foo"] = null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetAllPluginRegexAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetAllPluginRegexAsync_NullPattern_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getAllPluginRegex", new Dictionary<string, List<string>> { ["foo"] = [null!] });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetAllPluginRegexAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetPluginRegexAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<string>>(channel, "getPluginRegex", null);

        var patterns = await api.GetPluginRegexAsync("https://foo.com/a");

        Assert.That(patterns, Is.Empty);
    }

    [Test]
    public async Task GetPluginRegexAsync_NullPattern_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getPluginRegex", new List<string> { null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetPluginRegexAsync("https://foo.com/a"));

        AssertDeviceUnreadable(thrown);
    }

    [TestCaseSource(nameof(InvalidCalls))]
    public void InvalidArguments_ThrowWithoutCallingChannel(Func<PluginsApi, Task> call)
    {
        var (channel, api) = Create();

        Assert.Catch<ArgumentException>(() => call(api));

        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task GetAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        await api.GetAsync(Iface, Name, "Key", cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Plugins_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Plugins, Is.SameAs(device.Plugins));
    }

    [Test]
    public void QueryAsync_ConfigInterfaceAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.QueryAsync(new ConfigQuery { ConfigInterface = TextCaps.Identifier }));
    }

    [Test]
    public void QueryAsync_ConfigInterfaceOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.QueryAsync(new ConfigQuery { ConfigInterface = TextCaps.IdentifierOver }), "query");
    }

    [Test]
    public void QueryAsync_PatternAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.QueryAsync(new ConfigQuery { Pattern = TextCaps.LongText }));
    }

    [Test]
    public void QueryAsync_PatternOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.QueryAsync(new ConfigQuery { Pattern = TextCaps.LongTextOver }), "query");
    }

    [Test]
    public void GetPluginRegexAsync_UrlAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetPluginRegexAsync(TextCaps.LongText));
    }

    [Test]
    public void GetPluginRegexAsync_UrlOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetPluginRegexAsync(TextCaps.LongTextOver), "url");
    }

    [Test]
    public void GetAsync_InterfaceNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync(TextCaps.Identifier, "display", "key"));
    }

    [Test]
    public void GetAsync_InterfaceNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync(TextCaps.IdentifierOver, "display", "key"), "interfaceName");
    }

    [Test]
    public void GetAsync_DisplayNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync("iface", TextCaps.Identifier, "key"));
    }

    [Test]
    public void GetAsync_DisplayNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync("iface", TextCaps.IdentifierOver, "key"), "displayName");
    }

    [Test]
    public void GetAsync_KeyAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync("iface", "display", TextCaps.Identifier));
    }

    [Test]
    public void GetAsync_KeyOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new PluginsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync("iface", "display", TextCaps.IdentifierOver), "key");
    }
}
