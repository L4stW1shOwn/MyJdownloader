namespace MyJdownloader.Tests.Toolbar;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Errors;
using MyJdownloader.Toolbar;
using NSubstitute;

[TestFixture]
public sealed class ToolbarApiTests
{
    private static readonly object[][] _boolCalls =
    [
        ["/toolbar/isAvailable", (Func<ToolbarApi, Task<bool>>)(a => a.IsAvailableAsync())],
        ["/toolbar/startDownloads", (Func<ToolbarApi, Task<bool>>)(a => a.StartDownloadsAsync())],
        ["/toolbar/stopDownloads", (Func<ToolbarApi, Task<bool>>)(a => a.StopDownloadsAsync())],
        ["/toolbar/triggerUpdate", (Func<ToolbarApi, Task<bool>>)(a => a.TriggerUpdateAsync())],
        ["/toolbar/toggleDownloadSpeedLimit", (Func<ToolbarApi, Task<bool>>)(a => a.ToggleDownloadSpeedLimitAsync())],
        ["/toolbar/togglePauseDownloads", (Func<ToolbarApi, Task<bool>>)(a => a.TogglePauseDownloadsAsync())],
        ["/toolbar/togglePremium", (Func<ToolbarApi, Task<bool>>)(a => a.TogglePremiumAsync())],
        ["/toolbar/toggleClipboardMonitoring", (Func<ToolbarApi, Task<bool>>)(a => a.ToggleClipboardMonitoringAsync())],
        ["/toolbar/toggleAutomaticReconnect", (Func<ToolbarApi, Task<bool>>)(a => a.ToggleAutomaticReconnectAsync())],
        ["/toolbar/toggleStopAfterCurrentDownload", (Func<ToolbarApi, Task<bool>>)(a => a.ToggleStopAfterCurrentDownloadAsync())],
    ];

    private static (IDeviceChannel Channel, ToolbarApi Api) CreateBool(string path, bool? returned)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync(path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new ToolbarApi(channel));
    }

    private static (IDeviceChannel Channel, ToolbarApi Api) CreateStatus(ToolbarStatus? returned)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/toolbar/getStatus", Arg.Any<JsonTypeInfo<ToolbarStatus>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new ToolbarApi(channel));
    }

    [TestCaseSource(nameof(_boolCalls))]
    public async Task BoolCall_Call_UsesExactPathWithEmptyArgs(string path, Func<ToolbarApi, Task<bool>> call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var (channel, api) = CreateBool(path, true);

        var result = await call(api);

        var received = channel.ReceivedCalls().Single().GetArguments();
        Assert.Multiple(() =>
        {
            Assert.That((string)received[0]!, Is.EqualTo(path));
            Assert.That((IReadOnlyList<JsonElement>)received[2]!, Is.Empty);
            Assert.That(result, Is.True);
        });
    }

    [TestCaseSource(nameof(_boolCalls))]
    public async Task BoolCall_FalseResult_IsReturned(string path, Func<ToolbarApi, Task<bool>> call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var (_, api) = CreateBool(path, false);

        Assert.That(await call(api), Is.False);
    }

    [TestCaseSource(nameof(_boolCalls))]
    public void BoolCall_NullResult_ThrowsProtocolException(string path, Func<ToolbarApi, Task<bool>> call)
    {
        var (_, api) = CreateBool(path, null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => call(api));
    }

    [Test]
    public async Task GetStatusAsync_Call_UsesExactPathWithEmptyArgs()
    {
        var status = new ToolbarStatus { Running = true };
        var (channel, api) = CreateStatus(status);

        var result = await api.GetStatusAsync();

        var received = channel.ReceivedCalls().Single().GetArguments();
        Assert.Multiple(() =>
        {
            Assert.That((string)received[0]!, Is.EqualTo("/toolbar/getStatus"));
            Assert.That((IReadOnlyList<JsonElement>)received[2]!, Is.Empty);
            Assert.That(result, Is.SameAs(status));
        });
    }

    [Test]
    public void GetStatusAsync_NullStatus_ThrowsProtocolException()
    {
        var (_, api) = CreateStatus(null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetStatusAsync());
    }

    [Test]
    public async Task GetStatusAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateStatus(new ToolbarStatus());

        await api.GetStatusAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task ToggleDownloadSpeedLimitAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateBool("/toolbar/toggleDownloadSpeedLimit", true);

        await api.ToggleDownloadSpeedLimitAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }
}
