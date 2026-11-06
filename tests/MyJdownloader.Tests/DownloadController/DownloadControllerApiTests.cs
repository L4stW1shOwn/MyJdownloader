namespace MyJdownloader.Tests.DownloadController;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.DownloadController;
using NSubstitute;

[TestFixture]
public sealed class DownloadControllerApiTests
{
    private const string Base = "/downloadcontroller/";

    private static (IDeviceChannel Channel, DownloadControllerApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new DownloadControllerApi(channel));
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

    private static void ReturnsState(IDeviceChannel channel, string path, DownloadState? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<DownloadState>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? DownloadState.Unknown));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<DownloadState?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        Assert.That(thrown.Message, Is.EqualTo(MyJdownloader.Transport.RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task StartAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        ReturnsBool(channel, "start", true);

        await api.StartAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "start"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task StopAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        ReturnsBool(channel, "stop", true);

        await api.StopAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "stop"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task PauseAsync_True_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        ReturnsBool(channel, "pause", true);

        await api.PauseAsync(true);

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "pause"));
        Assert.That(args, Has.Count.EqualTo(1));
        Assert.That(args[0].GetRawText(), Is.EqualTo("true"));
    }

    [Test]
    public async Task PauseAsync_False_SendsFalse()
    {
        var (channel, api) = Create();

        ReturnsBool(channel, "pause", true);

        await api.PauseAsync(false);

        var (_, args) = Sent(channel);
        Assert.That(args, Has.Count.EqualTo(1));
        Assert.That(args[0].GetRawText(), Is.EqualTo("false"));
    }

    [Test]
    public async Task PauseAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "pause", true);

        await api.PauseAsync(true, cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetCurrentStateAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        ReturnsState(channel, "getCurrentState", DownloadState.Idle);

        await api.GetCurrentStateAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "getCurrentState"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task GetSpeedAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        ReturnsInt(channel, "getSpeedInBps", 1);

        await api.GetSpeedAsync();

        var (path, args) = Sent(channel);
        Assert.That(path, Is.EqualTo(Base + "getSpeedInBps"));
        Assert.That(args, Is.Empty);
    }

    [Test]
    public async Task StartAsync_DeviceReturnsTrue_ReturnsTrue()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "start", true);

        var result = await api.StartAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task StopAsync_DeviceReturnsTrue_ReturnsTrue()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "stop", true);

        var result = await api.StopAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task PauseAsync_DeviceReturnsTrue_ReturnsTrue()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "pause", true);

        var result = await api.PauseAsync(true);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetCurrentStateAsync_DeviceReturnsRunning_ReturnsRunning()
    {
        var (channel, api) = Create();
        ReturnsState(channel, "getCurrentState", DownloadState.Running);

        var result = await api.GetCurrentStateAsync();

        Assert.That(result, Is.EqualTo(DownloadState.Running));
    }

    [Test]
    public async Task GetSpeedAsync_DeviceReturnsSpeed_ReturnsSpeed()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getSpeedInBps", 12345);

        var result = await api.GetSpeedAsync();

        Assert.That(result, Is.EqualTo(12345));
    }

    [Test]
    public async Task StartAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "start", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.StartAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task StopAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "stop", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.StopAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task PauseAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "pause", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.PauseAsync(true));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetCurrentStateAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsState(channel, "getCurrentState", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetCurrentStateAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetSpeedAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsInt(channel, "getSpeedInBps", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetSpeedAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void Device_DownloadController_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.DownloadController, Is.SameAs(device.DownloadController));
    }
}
