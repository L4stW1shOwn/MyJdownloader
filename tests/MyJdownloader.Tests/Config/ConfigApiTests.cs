namespace MyJdownloader.Tests.Config;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Config;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class ConfigApiTests
{
    private const string Base = "/config/";
    private const string Iface = "org.Iface";

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
            yield return Case($"GetAsync_InterfaceName_{label}_Throws", a => a.GetAsync(bad!, null, "Key"));
            yield return Case($"GetAsync_Key_{label}_Throws", a => a.GetAsync(Iface, null, bad!));
            yield return Case($"GetDefaultAsync_InterfaceName_{label}_Throws", a => a.GetDefaultAsync(bad!, null, "Key"));
            yield return Case($"GetDefaultAsync_Key_{label}_Throws", a => a.GetDefaultAsync(Iface, null, bad!));
            yield return Case($"SetAsync_InterfaceName_{label}_Throws", a => a.SetAsync(bad!, null, "Key", value));
            yield return Case($"SetAsync_Key_{label}_Throws", a => a.SetAsync(Iface, null, bad!, value));
            yield return Case($"ResetAsync_InterfaceName_{label}_Throws", a => a.ResetAsync(bad!, null, "Key"));
            yield return Case($"ResetAsync_Key_{label}_Throws", a => a.ResetAsync(Iface, null, bad!));
            yield return Case($"ListEnumAsync_Type_{label}_Throws", a => a.ListEnumAsync(bad!));

            if (bad is null)
            {
                continue;
            }

            yield return Case($"GetAsync_Storage_{label}_Throws", a => a.GetAsync(Iface, bad, "Key"));
            yield return Case($"GetDefaultAsync_Storage_{label}_Throws", a => a.GetDefaultAsync(Iface, bad, "Key"));
            yield return Case($"SetAsync_Storage_{label}_Throws", a => a.SetAsync(Iface, bad, "Key", value));
            yield return Case($"ResetAsync_Storage_{label}_Throws", a => a.ResetAsync(Iface, bad, "Key"));
            yield return Case($"QueryAsync_Pattern_{label}_Throws", a => a.QueryAsync(new ConfigQuery { Pattern = bad }));
            yield return Case($"QueryAsync_ConfigInterface_{label}_Throws", a => a.QueryAsync(new ConfigQuery { ConfigInterface = bad }));
        }

        yield return Case("SetAsync_UndefinedValue_Throws", a => a.SetAsync(Iface, null, "Key", default));
    }

    private static TestCaseData Case(string name, Func<ConfigApi, Task> call)
        => new TestCaseData(call).SetName(name);

    private static (IDeviceChannel Channel, ConfigApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new ConfigApi(channel));
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

        await api.SetAsync(Iface, null, "Key", value);

        return Sent(channel).Args[3].GetString()!;
    }

    [Test]
    public async Task GetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        await api.GetAsync(Iface, "cfg/x", "Key");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/get"));
            Assert.That(args, Has.Count.EqualTo(3));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo("cfg/x"));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
        });
    }

    [Test]
    public async Task GetDefaultAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "getDefault", null);

        await api.GetDefaultAsync(Iface, "cfg/x", "Key");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/getDefault"));
            Assert.That(args, Has.Count.EqualTo(3));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo("cfg/x"));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
        });
    }

    [Test]
    public async Task SetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);

        await api.SetAsync(Iface, "cfg/x", "Key", JsonSerializer.SerializeToElement(42));

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/set"));
            Assert.That(args, Has.Count.EqualTo(4));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo("cfg/x"));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
            Assert.That(args[3].GetString(), Is.EqualTo("42"));
        });
    }

    [Test]
    public async Task ResetAsync_Args_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", true);

        await api.ResetAsync(Iface, "cfg/x", "Key");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/reset"));
            Assert.That(args, Has.Count.EqualTo(3));
            Assert.That(args[0].GetString(), Is.EqualTo(Iface));
            Assert.That(args[1].GetString(), Is.EqualTo("cfg/x"));
            Assert.That(args[2].GetString(), Is.EqualTo("Key"));
        });
    }

    [Test]
    public async Task QueryAsync_Query_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "query", Arg.Any<JsonTypeInfo<List<ConfigEntry>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<ConfigEntry>?>(null));

        await api.QueryAsync(new ConfigQuery { ConfigInterface = Iface, Pattern = "dl.*" });

        var (path, args) = Sent(channel);
        var raw = args.Single().GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/query"));
            Assert.That(raw, Does.Contain("\"configInterface\":\"org.Iface\""));
            Assert.That(raw, Does.Contain("\"pattern\":\"dl.*\""));
        });
    }

    [Test]
    public async Task ListEnumAsync_Type_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "listEnum", Arg.Any<JsonTypeInfo<List<ConfigEnumOption>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<ConfigEnumOption>?>(null));

        await api.ListEnumAsync("org.Type");

        var (path, args) = Sent(channel);
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/config/listEnum"));
            Assert.That(args, Has.Count.EqualTo(1));
            Assert.That(args[0].GetString(), Is.EqualTo("org.Type"));
        });
    }

    [Test]
    public async Task GetAsync_NullStorage_SendsJsonNull()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        await api.GetAsync(Iface, null, "Key");

        Assert.That(Sent(channel).Args[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void SetAsync_ValueOverRequestCap_ThrowsWithoutSending()
    {
        var (channel, api) = Create();
        var value = JsonSerializer.SerializeToElement(new string('a', 1_048_575));

        var ex = Assert.Throws<ArgumentException>(() => api.SetAsync(Iface, null, "Key", value));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task SetAsync_String_SendsQuotedJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement("foo")), Is.EqualTo("\"foo\""));

    [Test]
    public async Task SetAsync_StringTrue_SendsQuotedJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement("true")), Is.EqualTo("\"true\""));

    [Test]
    public async Task SetAsync_Boolean_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement(true)), Is.EqualTo("true"));

    [Test]
    public async Task SetAsync_Number_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonSerializer.SerializeToElement(42)), Is.EqualTo("42"));

    [Test]
    public async Task SetAsync_Array_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonDocument.Parse(/*lang=json,strict*/ "[\"a\",\"b\"]").RootElement), Is.EqualTo(/*lang=json,strict*/ "[\"a\",\"b\"]"));

    [Test]
    public async Task SetAsync_Object_SendsJsonText() =>
        Assert.That(await SentSetValueAsync(JsonDocument.Parse(/*lang=json,strict*/ "{\"k\":1}").RootElement), Is.EqualTo(/*lang=json,strict*/ "{\"k\":1}"));

    [Test]
    public async Task QueryAsync_DefaultQuery_DoesNotRequestValues()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "query", Arg.Any<JsonTypeInfo<List<ConfigEntry>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<ConfigEntry>?>(null));

        await api.QueryAsync();

        Assert.That(Sent(channel).Args[0].GetRawText(), Does.Contain("\"values\":false"));
    }

    [Test]
    public async Task GetAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", JsonSerializer.SerializeToElement("/dl"));

        var result = await api.GetAsync(Iface, null, "Key");

        Assert.That(result.GetString(), Is.EqualTo("/dl"));
    }

    [Test]
    public async Task GetDefaultAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "getDefault", JsonSerializer.SerializeToElement("/dl"));

        var result = await api.GetDefaultAsync(Iface, null, "Key");

        Assert.That(result.GetString(), Is.EqualTo("/dl"));
    }

    [Test]
    public async Task GetAsync_NullData_ReturnsNullElement()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "get", null);

        var result = await api.GetAsync(Iface, null, "Key");

        Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task GetDefaultAsync_NullData_ReturnsNullElement()
    {
        var (channel, api) = Create();
        ReturnsElement(channel, "getDefault", null);

        var result = await api.GetDefaultAsync(Iface, null, "Key");

        Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task SetAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetAsync(Iface, null, "Key", JsonSerializer.SerializeToElement(1)));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task ResetAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ResetAsync(Iface, null, "Key"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task SetAsync_True_ReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "set", true);

        var result = await api.SetAsync(Iface, null, "Key", JsonSerializer.SerializeToElement(1));

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ResetAsync_True_ReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "reset", true);

        var result = await api.ResetAsync(Iface, null, "Key");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task QueryAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "query", Arg.Any<JsonTypeInfo<List<ConfigEntry>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<ConfigEntry>?>(null));

        var entries = await api.QueryAsync();

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task ListEnumAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        channel.CallAsync(Base + "listEnum", Arg.Any<JsonTypeInfo<List<ConfigEnumOption>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<ConfigEnumOption>?>(null));

        var options = await api.ListEnumAsync("org.Type");

        Assert.That(options, Is.Empty);
    }

    [TestCaseSource(nameof(InvalidCalls))]
    public void InvalidArguments_ThrowWithoutCallingChannel(Func<ConfigApi, Task> call)
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

        await api.GetAsync(Iface, null, "Key", cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Config_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Config, Is.SameAs(device.Config));
    }

    [Test]
    public void QueryAsync_ConfigInterfaceAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.QueryAsync(new ConfigQuery { ConfigInterface = TextCaps.Identifier }));
    }

    [Test]
    public void QueryAsync_ConfigInterfaceOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.QueryAsync(new ConfigQuery { ConfigInterface = TextCaps.IdentifierOver }), "query");
    }

    [Test]
    public void QueryAsync_PatternAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.QueryAsync(new ConfigQuery { Pattern = TextCaps.LongText }));
    }

    [Test]
    public void QueryAsync_PatternOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.QueryAsync(new ConfigQuery { Pattern = TextCaps.LongTextOver }), "query");
    }

    [Test]
    public void ListEnumAsync_TypeAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.ListEnumAsync(TextCaps.Identifier));
    }

    [Test]
    public void ListEnumAsync_TypeOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.ListEnumAsync(TextCaps.IdentifierOver), "type");
    }

    [Test]
    public void GetAsync_InterfaceNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync(TextCaps.Identifier, null, "key"));
    }

    [Test]
    public void GetAsync_InterfaceNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync(TextCaps.IdentifierOver, null, "key"), "interfaceName");
    }

    [Test]
    public void GetAsync_StorageAtPathMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync("iface", TextCaps.Path, "key"));
    }

    [Test]
    public void GetAsync_StorageOverPathMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync("iface", TextCaps.PathOver, "key"), "storage");
    }

    [Test]
    public void GetAsync_KeyAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertSent(channel, () => api.GetAsync("iface", null, TextCaps.Identifier));
    }

    [Test]
    public void GetAsync_KeyOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ConfigApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetAsync("iface", null, TextCaps.IdentifierOver), "key");
    }
}
