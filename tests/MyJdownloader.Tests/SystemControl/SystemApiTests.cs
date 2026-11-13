namespace MyJdownloader.Tests.SystemControl;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Facades;
using MyJdownloader.SystemControl;
using MyJdownloader.Transport;
using NSubstitute;

[TestFixture]
public sealed class SystemApiTests
{
    private const string Base = "/system/";
    private static readonly string[] _nullArg = ["null"];
    private static readonly string[] _pathArg = ["\"C:\\\\\""];

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

    private const string SystemInfoJson = """
        {"archFamily":"X86","archString":"amd64","hardware":"hw","javaName":"OpenJDK 64-Bit Server VM","javaVendor":"Eclipse Adoptium","javaVersionString":"11.0.25+11","operatingSystem":"LINUX","osFamily":"LINUX","osString":"Linux 6.1","heapCommitted":268435456,"heapMax":1073741824,"heapUsed":123456789,"javaVersion":11000025000011,"startupTimeStamp":1762000000000,"arch64Bit":true,"docker":true,"headless":true,"jvm64Bit":true,"os64Bit":true,"snap":false,"unknownField":42}
        """;

    private static (IDeviceChannel Channel, SystemApi Api) CreateInfo(SystemInfo? returned)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/system/getSystemInfos", Arg.Any<JsonTypeInfo<SystemInfo>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new SystemApi(channel));
    }

    private static (IDeviceChannel Channel, SystemApi Api) CreateStorage(List<StorageInfo>? returned)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/system/getStorageInfos", Arg.Any<JsonTypeInfo<List<StorageInfo>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new SystemApi(channel));
    }

    private static string[] SentRawArgs(IDeviceChannel channel) =>
        [.. channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single().Select(a => a.GetRawText())];

    private static string SentPath(IDeviceChannel channel) => (string)channel.ReceivedCalls().Single().GetArguments()[0]!;

    private static IEnumerable<string> InvalidPaths()
    {
        yield return "/tmp/\u0001x";
        yield return new string('a', FacadeGuard.MaxPathLength + 1);
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
    public async Task GetSystemInfoAsync_Call_UsesExactPathWithoutArguments()
    {
        var (channel, api) = CreateInfo(new SystemInfo());

        await api.GetSystemInfoAsync();

        Assert.That(SentPath(channel), Is.EqualTo("/system/getSystemInfos"));
        Assert.That(SentRawArgs(channel), Is.Empty);
    }

    [Test]
    public void GetSystemInfoAsync_NullData_ThrowsProtocolException()
    {
        var (_, api) = CreateInfo(null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetSystemInfoAsync());
    }

    [Test]
    public async Task GetSystemInfoAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateInfo(new SystemInfo());

        await api.GetSystemInfoAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void SystemInfo_LiveShapedJson_MapsEveryField()
    {
        var info = JsonSerializer.Deserialize(SystemInfoJson, SystemJsonContext.Default.SystemInfo)!;

        Assert.Multiple(() =>
        {
            Assert.That(info.ArchFamily, Is.EqualTo("X86"));
            Assert.That(info.ArchString, Is.EqualTo("amd64"));
            Assert.That(info.Hardware, Is.EqualTo("hw"));
            Assert.That(info.JavaName, Is.EqualTo("OpenJDK 64-Bit Server VM"));
            Assert.That(info.JavaVendor, Is.EqualTo("Eclipse Adoptium"));
            Assert.That(info.JavaVersionString, Is.EqualTo("11.0.25+11"));
            Assert.That(info.OperatingSystem, Is.EqualTo("LINUX"));
            Assert.That(info.OsFamily, Is.EqualTo("LINUX"));
            Assert.That(info.OsString, Is.EqualTo("Linux 6.1"));
            Assert.That(info.HeapCommitted, Is.EqualTo(268435456));
            Assert.That(info.HeapMax, Is.EqualTo(1073741824));
            Assert.That(info.HeapUsed, Is.EqualTo(123456789));
            Assert.That(info.JavaVersion, Is.EqualTo(11000025000011));
            Assert.That(info.StartupTime, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1762000000000)));
            Assert.That(info.Arch64Bit, Is.True);
            Assert.That(info.Docker, Is.True);
            Assert.That(info.Headless, Is.True);
            Assert.That(info.Jvm64Bit, Is.True);
            Assert.That(info.Os64Bit, Is.True);
            Assert.That(info.Snap, Is.False);
        });
    }

    [Test]
    public void SystemInfo_ZeroStartupTimeStamp_IsNull()
    {
        var info = JsonSerializer.Deserialize("""{"startupTimeStamp":0}""", SystemJsonContext.Default.SystemInfo)!;

        Assert.That(info.StartupTime, Is.Null);
    }

    [Test]
    public async Task GetStorageInfosAsync_Default_SendsJsonNullArgument()
    {
        var (channel, api) = CreateStorage([]);

        await api.GetStorageInfosAsync();

        Assert.That(SentPath(channel), Is.EqualTo("/system/getStorageInfos"));
        Assert.That(SentRawArgs(channel), Is.EqualTo(_nullArg));
    }

    [Test]
    public async Task GetStorageInfosAsync_Path_SendsPathArgument()
    {
        var (channel, api) = CreateStorage([]);

        await api.GetStorageInfosAsync("C:\\");

        Assert.That(SentRawArgs(channel), Is.EqualTo(_pathArg));
    }

    [Test]
    public async Task GetStorageInfosAsync_NullData_ReturnsEmptyList()
    {
        var (_, api) = CreateStorage(null);

        var result = await api.GetStorageInfosAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetStorageInfosAsync_NullItem_ThrowsProtocolException()
    {
        var (_, api) = CreateStorage([null!]);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetStorageInfosAsync());
    }

    [Test]
    public async Task GetStorageInfosAsync_Items_AreReturned()
    {
        var (_, api) = CreateStorage([new StorageInfo { Path = "/", Size = 100, Free = 40 }]);

        var result = await api.GetStorageInfosAsync();

        Assert.That(result.Single().Free, Is.EqualTo(40));
    }

    [Test]
    public void StorageInfo_LiveShapedJson_MapsEveryField()
    {
        var list = JsonSerializer.Deserialize("""[{"path":"/","size":100,"free":40,"error":"boom","extra":1}]""", SystemJsonContext.Default.ListStorageInfo)!;

        Assert.Multiple(() =>
        {
            Assert.That(list[0].Path, Is.EqualTo("/"));
            Assert.That(list[0].Size, Is.EqualTo(100));
            Assert.That(list[0].Free, Is.EqualTo(40));
            Assert.That(list[0].Error, Is.EqualTo("boom"));
        });
    }

    [Test]
    public void StorageInfo_NullError_IsNull()
    {
        var list = JsonSerializer.Deserialize("""[{"path":"/","size":100,"free":40,"error":null}]""", SystemJsonContext.Default.ListStorageInfo)!;

        Assert.That(list[0].Error, Is.Null);
    }

    [TestCaseSource(nameof(InvalidPaths))]
    public void GetStorageInfosAsync_InvalidPath_ThrowsBeforeAnyCall(string path)
    {
        var (channel, api) = CreateStorage([]);

        Assert.Throws<ArgumentException>(() => api.GetStorageInfosAsync(path));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task GetStorageInfosAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateStorage([]);

        await api.GetStorageInfosAsync(cancellationToken: cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
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
