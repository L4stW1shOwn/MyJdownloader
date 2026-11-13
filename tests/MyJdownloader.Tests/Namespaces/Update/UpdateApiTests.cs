namespace MyJdownloader.Tests.Update;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Errors;
using MyJdownloader.Update;
using NSubstitute;

[TestFixture]
public sealed class UpdateApiTests
{
    private const string Base = "/update/";

    private static (IDeviceChannel Channel, UpdateApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new UpdateApi(channel));
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

    [Test]
    public async Task IsUpdateAvailableAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isUpdateAvailable", true);

        await api.IsUpdateAvailableAsync();

        AssertSent(channel, "isUpdateAvailable");
    }

    [Test]
    public async Task IsUpdateAvailableAsync_DeviceReturnsTrue_ReturnsTrue()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isUpdateAvailable", true);

        var result = await api.IsUpdateAvailableAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsUpdateAvailableAsync_DeviceReturnsFalse_ReturnsFalse()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isUpdateAvailable", false);

        var result = await api.IsUpdateAvailableAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUpdateAvailableAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "isUpdateAvailable", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.IsUpdateAvailableAsync());

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task RunUpdateCheckAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RunUpdateCheckAsync();

        AssertSent(channel, "runUpdateCheck");
    }

    [Test]
    public async Task RestartAndUpdateAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RestartAndUpdateAsync();

        AssertSent(channel, "restartAndUpdate");
    }

    [Test]
    public async Task IsUpdateAvailableAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "isUpdateAvailable", true);

        await api.IsUpdateAvailableAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task RunUpdateCheckAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.RunUpdateCheckAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task RestartAndUpdateAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.RestartAndUpdateAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Update_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Update, Is.SameAs(device.Update));
    }
}
