namespace MyJdownloader.Tests.Polling;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Polling;
using NSubstitute;

[TestFixture]
public sealed class PollingApiTests
{
    private const string Path = "/polling/poll";

    private static readonly string[] _expectedNames = ["jdState", "linkGrabberState"];

    private static (IDeviceChannel Channel, PollingApi Api) Create(List<PollingResult>? returned = null)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync(Path, Arg.Any<JsonTypeInfo<List<PollingResult>>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new PollingApi(channel));
    }

    private static IReadOnlyList<JsonElement> SentArgs(IDeviceChannel channel) =>
        channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single();

    private static string SentJson(IDeviceChannel channel) => SentArgs(channel).Single().GetRawText();

    [Test]
    public async Task PollAsync_DefaultQuery_SendsAllKeysInOrder()
    {
        var (channel, api) = Create();

        await api.PollAsync(new PollingQuery());

        Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo(Path));
        Assert.That(SentJson(channel), Is.EqualTo(/*lang=json,strict*/ """{"downloadProgress":null,"jdState":true,"linkGrabberState":true,"captchasWaiting":true,"aggregatedNumbers":true}"""));
    }

    [Test]
    public async Task PollAsync_NullQuery_UsesDefaults()
    {
        var (channel, api) = Create();

        await api.PollAsync();

        Assert.That(SentJson(channel), Is.EqualTo(/*lang=json,strict*/ """{"downloadProgress":null,"jdState":true,"linkGrabberState":true,"captchasWaiting":true,"aggregatedNumbers":true}"""));
    }

    [Test]
    public async Task PollAsync_SubsetOfFlags_SendsOnlyEnabledKeys()
    {
        var (channel, api) = Create();

        await api.PollAsync(new PollingQuery { DownloadProgress = false, LinkGrabberState = false, AggregatedNumbers = false });

        Assert.That(SentJson(channel), Is.EqualTo(/*lang=json,strict*/ """{"jdState":true,"captchasWaiting":true}"""));
    }

    [Test]
    public async Task PollAsync_PackageUuids_SendsUuidArray()
    {
        var (channel, api) = Create();

        await api.PollAsync(new PollingQuery { JdState = false, LinkGrabberState = false, CaptchasWaiting = false, AggregatedNumbers = false, DownloadProgressPackageUuids = [12L, 34L] });

        Assert.That(SentJson(channel), Is.EqualTo(/*lang=json,strict*/ """{"downloadProgress":[12,34]}"""));
    }

    [Test]
    public async Task PollAsync_EmptyUuidList_SendsEmptyArray()
    {
        var (channel, api) = Create();

        await api.PollAsync(new PollingQuery { DownloadProgressPackageUuids = [] });

        Assert.That(SentJson(channel), Does.StartWith("""{"downloadProgress":[],"""));
    }

    [Test]
    public async Task PollAsync_DownloadProgressDisabledWithUuids_OmitsKey()
    {
        var (channel, api) = Create();

        await api.PollAsync(new PollingQuery { DownloadProgress = false, DownloadProgressPackageUuids = [1L] });

        Assert.That(SentJson(channel), Is.EqualTo(/*lang=json,strict*/ """{"jdState":true,"linkGrabberState":true,"captchasWaiting":true,"aggregatedNumbers":true}"""));
    }

    [Test]
    public void PollAsync_NoFlagEnabled_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = Create();
        var query = new PollingQuery { DownloadProgress = false, JdState = false, LinkGrabberState = false, CaptchasWaiting = false, AggregatedNumbers = false };

        var thrown = Assert.Throws<ArgumentException>(() => api.PollAsync(query));

        Assert.That(thrown!.ParamName, Is.EqualTo("query"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task PollAsync_NullData_ReturnsEmptyList()
    {
        var (_, api) = Create(null);

        var result = await api.PollAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task PollAsync_Results_AreReturned()
    {
        var (_, api) = Create([new PollingResult { EventName = "jdState" }, new PollingResult { EventName = "linkGrabberState" }]);

        var result = await api.PollAsync();

        Assert.That(result.Select(r => r.EventName), Is.EqualTo(_expectedNames));
    }

    [Test]
    public async Task PollAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.PollAsync(null, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Polling_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Polling, Is.SameAs(device.Polling));
    }
}
