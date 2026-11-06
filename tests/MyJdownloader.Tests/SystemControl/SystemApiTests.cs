namespace MyJdownloader.Tests.SystemControl;

using System.Text.Json;
using MyJdownloader.SystemControl;
using NSubstitute;

[TestFixture]
public sealed class SystemApiTests
{
    private const string Base = "/system/";

    private static (IDeviceChannel Channel, SystemApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new SystemApi(channel));
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

    [Test]
    public async Task RestartAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.RestartAsync();

        AssertSent(channel, "restartJD");
    }

    [Test]
    public async Task ExitAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.ExitAsync();

        AssertSent(channel, "exitJD");
    }

    [Test]
    public async Task ShutdownOsAsync_True_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.ShutdownOsAsync(true);

        AssertSent(channel, "shutdownOS", "true");
    }

    [Test]
    public async Task ShutdownOsAsync_False_SendsFalse()
    {
        var (channel, api) = Create();

        await api.ShutdownOsAsync(false);

        AssertSent(channel, "shutdownOS", "false");
    }

    [Test]
    public async Task StandbyOsAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.StandbyOsAsync();

        AssertSent(channel, "standbyOS");
    }

    [Test]
    public async Task HibernateOsAsync_NoArgs_SendsPathAndArgs()
    {
        var (channel, api) = Create();

        await api.HibernateOsAsync();

        AssertSent(channel, "hibernateOS");
    }

    [Test]
    public async Task RestartAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.RestartAsync(cts.Token);

        var sentToken = channel.ReceivedCalls().Single().GetArguments().Last();
        Assert.That(sentToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_System_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.System, Is.SameAs(device.System));
    }
}
