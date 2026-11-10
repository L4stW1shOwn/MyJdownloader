namespace MyJdownloader.Tests.DeviceExtensions;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.DeviceExtensions;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class ExtensionsApiTests
{
    private const string Base = "/extensions/";
    private const string DefaultQueryJson = /*lang=json,strict*/ """{"configInterface":true,"description":true,"enabled":true,"iconKey":true,"installed":true,"name":true}""";

    private static (IDeviceChannel Channel, ExtensionsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new ExtensionsApi(channel));
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

    private static void AssertRejected(ArgumentException? thrown, IDeviceChannel channel, string paramName)
        => Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo(paramName));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });

    [Test]
    public async Task ListAsync_NoQuery_SendsDefaultQueryAndReturnsExtensions()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<Extension> { new() { Id = "a" } });

        var extensions = await api.ListAsync();

        AssertSent(channel, "list", DefaultQueryJson);
        Assert.That(extensions.Single().Id, Is.EqualTo("a"));
    }

    [Test]
    public async Task ListAsync_Pattern_SendsPattern()
    {
        var (channel, api) = Create();
        ReturnsRef<List<Extension>>(channel, "list", []);

        await api.ListAsync(new ExtensionQuery { Pattern = ".*extract.*" });

        var (_, args) = Sent(channel);
        Assert.That(args.Single().GetRawText(), Does.Contain("\"pattern\":\".*extract.*\""));
    }

    [Test]
    public async Task ListAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<Extension>>(channel, "list", null);

        var extensions = await api.ListAsync();

        Assert.That(extensions, Is.Empty);
    }

    [Test]
    public void ListAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<Extension> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.ListAsync());
    }

    [TestCase("")]
    [TestCase(" ")]
    public void ListAsync_BlankPattern_Throws(string pattern)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.ListAsync(new ExtensionQuery { Pattern = pattern }));

        AssertRejected(thrown, channel, "query");
    }

    [Test]
    public async Task InstallAsync_Id_SendsIdAndReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "install", true);

        var result = await api.InstallAsync("org.x.Ext");

        AssertSent(channel, "install", "\"org.x.Ext\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InstallAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "install", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.InstallAsync("org.x.Ext"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task IsInstalledAsync_Id_SendsIdAndReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isInstalled", true);

        var result = await api.IsInstalledAsync("org.x.Ext");

        AssertSent(channel, "isInstalled", "\"org.x.Ext\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsInstalledAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isInstalled", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.IsInstalledAsync("org.x.Ext"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task IsEnabledAsync_ClassName_SendsClassNameAndReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isEnabled", true);

        var result = await api.IsEnabledAsync("org.x.Ext");

        AssertSent(channel, "isEnabled", "\"org.x.Ext\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsEnabledAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isEnabled", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.IsEnabledAsync("org.x.Ext"));

        AssertDeviceUnreadable(thrown);
    }

    [TestCase(true, "true")]
    [TestCase(false, "false")]
    public async Task SetEnabledAsync_ClassNameAndFlag_SendsBoth(bool enabled, string rawFlag)
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setEnabled", true);

        var result = await api.SetEnabledAsync("org.x.Ext", enabled);

        AssertSent(channel, "setEnabled", "\"org.x.Ext\"", rawFlag);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SetEnabledAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setEnabled", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetEnabledAsync("org.x.Ext", true));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void InstallAsync_NullId_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.InstallAsync(null!));

        AssertRejected(thrown, channel, "id");
    }

    [TestCase("")]
    [TestCase(" ")]
    public void InstallAsync_BlankId_Throws(string id)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.InstallAsync(id));

        AssertRejected(thrown, channel, nameof(id));
    }

    [Test]
    public void InstallAsync_ControlCharacterId_ThrowsWithoutValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.InstallAsync("secret\nx"));

        Assert.That(thrown!.Message, Does.Not.Contain("secret"));
        AssertRejected(thrown, channel, "id");
    }

    [Test]
    public void IsInstalledAsync_NullId_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.IsInstalledAsync(null!));

        AssertRejected(thrown, channel, "id");
    }

    [TestCase("")]
    [TestCase(" ")]
    public void IsInstalledAsync_BlankId_Throws(string id)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.IsInstalledAsync(id));

        AssertRejected(thrown, channel, nameof(id));
    }

    [Test]
    public void IsInstalledAsync_ControlCharacterId_ThrowsWithoutValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.IsInstalledAsync("secret\nx"));

        Assert.That(thrown!.Message, Does.Not.Contain("secret"));
        AssertRejected(thrown, channel, "id");
    }

    [Test]
    public void IsEnabledAsync_NullClassName_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.IsEnabledAsync(null!));

        AssertRejected(thrown, channel, "className");
    }

    [TestCase("")]
    [TestCase(" ")]
    public void IsEnabledAsync_BlankClassName_Throws(string className)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.IsEnabledAsync(className));

        AssertRejected(thrown, channel, nameof(className));
    }

    [Test]
    public void IsEnabledAsync_ControlCharacterClassName_ThrowsWithoutValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.IsEnabledAsync("secret\nx"));

        Assert.That(thrown!.Message, Does.Not.Contain("secret"));
        AssertRejected(thrown, channel, "className");
    }

    [Test]
    public void SetEnabledAsync_NullClassName_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.SetEnabledAsync(null!, true));

        AssertRejected(thrown, channel, "className");
    }

    [TestCase("")]
    [TestCase(" ")]
    public void SetEnabledAsync_BlankClassName_Throws(string className)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SetEnabledAsync(className, true));

        AssertRejected(thrown, channel, nameof(className));
    }

    [Test]
    public void SetEnabledAsync_ControlCharacterClassName_ThrowsWithoutValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SetEnabledAsync("secret\nx", true));

        Assert.That(thrown!.Message, Does.Not.Contain("secret"));
        AssertRejected(thrown, channel, "className");
    }

    [Test]
    public async Task ListAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef<List<Extension>>(channel, "list", null);

        await api.ListAsync(null, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task InstallAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "install", true);

        await api.InstallAsync("org.x.Ext", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task IsInstalledAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "isInstalled", true);

        await api.IsInstalledAsync("org.x.Ext", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task IsEnabledAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "isEnabled", true);

        await api.IsEnabledAsync("org.x.Ext", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task SetEnabledAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "setEnabled", true);

        await api.SetEnabledAsync("org.x.Ext", true, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Extensions_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Extensions, Is.SameAs(device.Extensions));
    }

    [Test]
    public void ListAsync_PatternAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertSent(channel, () => api.ListAsync(new ExtensionQuery { Pattern = TextCaps.LongText }));
    }

    [Test]
    public void ListAsync_PatternOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertRejected(channel, () => api.ListAsync(new ExtensionQuery { Pattern = TextCaps.LongTextOver }), "query");
    }

    [Test]
    public void IsInstalledAsync_IdAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertSent(channel, () => api.IsInstalledAsync(TextCaps.Identifier));
    }

    [Test]
    public void IsInstalledAsync_IdOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertRejected(channel, () => api.IsInstalledAsync(TextCaps.IdentifierOver), "id");
    }

    [Test]
    public void IsEnabledAsync_ClassNameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertSent(channel, () => api.IsEnabledAsync(TextCaps.Identifier));
    }

    [Test]
    public void IsEnabledAsync_ClassNameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new ExtensionsApi(channel);

        TextCaps.AssertRejected(channel, () => api.IsEnabledAsync(TextCaps.IdentifierOver), "className");
    }
}
