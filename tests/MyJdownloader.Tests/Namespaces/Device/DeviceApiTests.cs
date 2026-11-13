namespace MyJdownloader.Tests.Device;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Device;
using MyJdownloader.Errors;
using NSubstitute;

[TestFixture]
public sealed class DeviceApiTests
{
    private const string Path = "/device/ping";

    private static (IDeviceChannel Channel, DeviceApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new DeviceApi(channel));
    }

    private static void ReturnsBool(IDeviceChannel channel, bool? value)
    {
        channel.CallAsync(Path, Arg.Any<JsonTypeInfo<bool>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? false));
        channel.CallAsync(Path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    [Test]
    public async Task PingAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, true);

        await api.PingAsync();

        var args = channel.ReceivedCalls().Single().GetArguments();
        Assert.That((string)args[0]!, Is.EqualTo(Path));
        Assert.That(args.OfType<IReadOnlyList<JsonElement>>().Single(), Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task PingAsync_Value_ReturnsValue(bool value)
    {
        var (channel, api) = Create();
        ReturnsBool(channel, value);

        var result = await api.PingAsync();

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public async Task PingAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.PingAsync());

        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task PingAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, true);

        await api.PingAsync(cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Device_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Device, Is.SameAs(device.Device));
    }
}
