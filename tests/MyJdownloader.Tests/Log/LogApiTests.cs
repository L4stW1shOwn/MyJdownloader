namespace MyJdownloader.Tests.Log;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Log;
using NSubstitute;

[TestFixture]
public sealed class LogApiTests
{
    private static readonly bool[] _expectedCurrent = [true, false];
    private static readonly DateTimeOffset _created = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000);
    private static readonly DateTimeOffset _modified = DateTimeOffset.FromUnixTimeMilliseconds(1700000500000);

    private static LogFolder Folder(bool current = true) => new() { Created = _created, Current = current, LastModified = _modified };

    private static (IDeviceChannel Channel, LogApi Api) CreateList(List<LogFolder>? returned = null)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/log/getAvailableLogs", Arg.Any<JsonTypeInfo<List<LogFolder>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new LogApi(channel));
    }

    private static (IDeviceChannel Channel, LogApi Api) CreateSend(string? returned = null)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/log/sendLogFile", Arg.Any<JsonTypeInfo<string>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new LogApi(channel));
    }

    private static IReadOnlyList<JsonElement> SentArgs(IDeviceChannel channel) =>
        channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single();

    [Test]
    public async Task GetAvailableLogsAsync_Call_UsesExactPathWithoutArguments()
    {
        var (channel, api) = CreateList([]);

        await api.GetAvailableLogsAsync();

        Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/log/getAvailableLogs"));
        Assert.That(SentArgs(channel), Is.Empty);
    }

    [Test]
    public async Task GetAvailableLogsAsync_NullData_ReturnsEmptyList()
    {
        var (_, api) = CreateList(null);

        var result = await api.GetAvailableLogsAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAvailableLogsAsync_Folders_AreReturned()
    {
        var (_, api) = CreateList([Folder(), Folder(false)]);

        var result = await api.GetAvailableLogsAsync();

        Assert.That(result.Select(f => f.Current), Is.EqualTo(_expectedCurrent));
    }

    [Test]
    public async Task GetAvailableLogsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateList([]);

        await api.GetAvailableLogsAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task SendLogFileAsync_OneFolder_SendsExactArgument()
    {
        var (channel, api) = CreateSend("id");

        await api.SendLogFileAsync([Folder()]);

        Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/log/sendLogFile"));
        Assert.That(SentArgs(channel).Single().GetRawText(), Is.EqualTo(/*lang=json,strict*/ """[{"created":1700000000000,"current":true,"lastModified":1700000500000}]"""));
    }

    [Test]
    public async Task SendLogFileAsync_SeveralFolders_SendsOneArrayArgument()
    {
        var (channel, api) = CreateSend("id");

        await api.SendLogFileAsync([Folder(), Folder(false)]);

        Assert.That(SentArgs(channel).Single().GetRawText(), Is.EqualTo(/*lang=json,strict*/ """[{"created":1700000000000,"current":true,"lastModified":1700000500000},{"created":1700000000000,"current":false,"lastModified":1700000500000}]"""));
    }

    [Test]
    public async Task SendLogFileAsync_IdReturned_IsReturned()
    {
        var (_, api) = CreateSend("abc123");

        var id = await api.SendLogFileAsync([Folder()]);

        Assert.That(id, Is.EqualTo("abc123"));
    }

    [Test]
    public async Task SendLogFileAsync_NullData_ReturnsNull()
    {
        var (_, api) = CreateSend(null);

        var id = await api.SendLogFileAsync([Folder()]);

        Assert.That(id, Is.Null);
    }

    [Test]
    public async Task SendLogFileAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateSend("id");

        await api.SendLogFileAsync([Folder()], cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void SendLogFileAsync_NullList_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateSend();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.SendLogFileAsync(null!));

        Assert.That(thrown!.ParamName, Is.EqualTo("logFolders"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void SendLogFileAsync_EmptyList_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateSend();

        var thrown = Assert.Throws<ArgumentException>(() => api.SendLogFileAsync([]));

        Assert.That(thrown!.ParamName, Is.EqualTo("logFolders"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void SendLogFileAsync_NullElement_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateSend();

        var thrown = Assert.Throws<ArgumentException>(() => api.SendLogFileAsync([Folder(), null!]));

        Assert.That(thrown!.ParamName, Is.EqualTo("logFolders"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void SendLogFileAsync_MissingCreated_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateSend();

        var thrown = Assert.Throws<ArgumentException>(() => api.SendLogFileAsync([new LogFolder { LastModified = _modified }]));

        Assert.That(thrown!.ParamName, Is.EqualTo("logFolders"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void SendLogFileAsync_MissingLastModified_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateSend();

        var thrown = Assert.Throws<ArgumentException>(() => api.SendLogFileAsync([new LogFolder { Created = _created }]));

        Assert.That(thrown!.ParamName, Is.EqualTo("logFolders"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void Device_Log_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Log, Is.SameAs(device.Log));
    }
}
