namespace MyJdownloader.Tests.Reconnect;

using System.Text.Json;
using MyJdownloader.Reconnect;
using NSubstitute;

[TestFixture]
public sealed class ReconnectApiTests
{
    private static (IDeviceChannel Channel, ReconnectApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return (channel, new ReconnectApi(channel));
    }

    [Test]
    public async Task DoReconnectAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.DoReconnectAsync();

        var args = channel.ReceivedCalls().Single().GetArguments();
        Assert.That((string)args[0]!, Is.EqualTo("/reconnect/doReconnect"));
        Assert.That(args.OfType<IReadOnlyList<JsonElement>>().Single(), Is.Empty);
    }

    [Test]
    public async Task DoReconnectAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.DoReconnectAsync(cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Reconnect_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Reconnect, Is.SameAs(device.Reconnect));
    }
}
