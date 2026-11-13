namespace MyJdownloader.Tests.Jd;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Errors;
using MyJdownloader.Jd;
using NSubstitute;

[TestFixture]
public sealed class JdApiTests
{
    private const string Base = "/jd/";

    private static (IDeviceChannel Channel, JdApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new JdApi(channel));
    }

    private static (string Path, IReadOnlyList<JsonElement> Args) Sent(IDeviceChannel channel)
    {
        var call = channel.ReceivedCalls().Single();
        var args = call.GetArguments();
        return ((string)args[0]!, args.OfType<IReadOnlyList<JsonElement>>().Single());
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

    private static void ReturnsLong(IDeviceChannel channel, string path, long? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<long>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? 0L));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<long?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task GetUptimeAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "uptime", 1);

        await api.GetUptimeAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo("/jd/uptime"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task GetVersionAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "version", 1);

        await api.GetVersionAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo("/jd/version"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task GetCoreRevisionAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getCoreRevision", 1);

        await api.GetCoreRevisionAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo("/jd/getCoreRevision"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task RefreshPluginsAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "refreshPlugins", true);

        await api.RefreshPluginsAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo("/jd/refreshPlugins"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task GetUptimeAsync_Milliseconds_ReturnsTimeSpan()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "uptime", 90000);

        var result = await api.GetUptimeAsync();

        Assert.That(result, Is.EqualTo(TimeSpan.FromSeconds(90)));
    }

    [Test]
    public async Task GetVersionAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "version", 123456789);

        var result = await api.GetVersionAsync();

        Assert.That(result, Is.EqualTo(123456789L));
    }

    [Test]
    public async Task GetCoreRevisionAsync_Value_ReturnsValue()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getCoreRevision", 48000);

        var result = await api.GetCoreRevisionAsync();

        Assert.That(result, Is.EqualTo(48000));
    }

    [Test]
    public async Task RefreshPluginsAsync_True_ReturnsTrue()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "refreshPlugins", true);

        var result = await api.RefreshPluginsAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetUptimeAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "uptime", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetUptimeAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetVersionAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "version", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetVersionAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetCoreRevisionAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getCoreRevision", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetCoreRevisionAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task RefreshPluginsAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "refreshPlugins", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.RefreshPluginsAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetUptimeAsync_Negative_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "uptime", -1);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetUptimeAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetUptimeAsync_OutOfRange_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "uptime", long.MaxValue);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetUptimeAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetCoreRevisionAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsInt(channel, "getCoreRevision", 1);

        await api.GetCoreRevisionAsync(cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Jd_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Jd, Is.SameAs(device.Jd));
    }
}
