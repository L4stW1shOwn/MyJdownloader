namespace MyJdownloader.Tests.CaptchaForward;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.CaptchaForward;
using NSubstitute;

[TestFixture]
public sealed class CaptchaForwardApiTests
{
    private static readonly string[] _expectedDefaultArgs = ["\"key\"", "\"\"", "\"example.com\"", "\"\""];
    private static readonly string[] _expectedFullArgs = ["\"key\"", "\"stoken\"", "\"example.com\"", "\"why\""];
    private static readonly string[] _expectedResultArgs = ["42"];

    private static (IDeviceChannel Channel, CaptchaForwardApi Api) CreateCreate(long? returned = 7)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/captchaforward/createJobRecaptchaV2", Arg.Any<JsonTypeInfo<long?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new CaptchaForwardApi(channel));
    }

    private static (IDeviceChannel Channel, CaptchaForwardApi Api) CreateResult(string? returned = "token")
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/captchaforward/getResult", Arg.Any<JsonTypeInfo<string>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new CaptchaForwardApi(channel));
    }

    private static string[] SentArgs(IDeviceChannel channel) =>
        [.. channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single().Select(a => a.GetRawText())];

    [Test]
    public async Task CreateJobRecaptchaV2Async_Defaults_SendsEmptyOptionalsInWireOrder()
    {
        var (channel, api) = CreateCreate();

        var id = await api.CreateJobRecaptchaV2Async("key", "example.com");

        Assert.Multiple(() =>
        {
            Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/captchaforward/createJobRecaptchaV2"));
            Assert.That(SentArgs(channel), Is.EqualTo(_expectedDefaultArgs));
            Assert.That(id, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task CreateJobRecaptchaV2Async_AllSet_SendsAllInWireOrder()
    {
        var (channel, api) = CreateCreate();

        await api.CreateJobRecaptchaV2Async("key", "example.com", "stoken", "why");

        Assert.That(SentArgs(channel), Is.EqualTo(_expectedFullArgs));
    }

    [Test]
    public void CreateJobRecaptchaV2Async_NullJobId_ThrowsProtocolException()
    {
        var (_, api) = CreateCreate(null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.CreateJobRecaptchaV2Async("key", "example.com"));
    }

    [Test]
    public async Task CreateJobRecaptchaV2Async_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateCreate();

        await api.CreateJobRecaptchaV2Async("key", "example.com", cancellationToken: cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void CreateJobRecaptchaV2Async_NullSiteKey_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateCreate();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.CreateJobRecaptchaV2Async(null!, "example.com"));

        Assert.That(thrown!.ParamName, Is.EqualTo("siteKey"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void CreateJobRecaptchaV2Async_NullDomain_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateCreate();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.CreateJobRecaptchaV2Async("key", null!));

        Assert.That(thrown!.ParamName, Is.EqualTo("domain"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase("", "example.com", null, null, "siteKey")]
    [TestCase("k\ney", "example.com", null, null, "siteKey")]
    [TestCase("key", " ", null, null, "domain")]
    [TestCase("key", "exa\tmple.com", null, null, "domain")]
    [TestCase("key", "example.com", "", null, "stoken")]
    [TestCase("key", "example.com", "st\u0000", null, "stoken")]
    [TestCase("key", "example.com", null, " ", "reason")]
    [TestCase("key", "example.com", null, "a\rb", "reason")]
    [TestCase("key", "example.com", "a\"><script>", null, "stoken")]
    [TestCase("key", "example.com", "a b", null, "stoken")]
    [TestCase("k<y", "example.com", null, null, "siteKey")]
    [TestCase("k&y", "example.com", null, null, "siteKey")]
    [TestCase("key", "ex ample.com", null, null, "domain")]
    [TestCase("key", "example.com/\"x", null, null, "domain")]
    [TestCase("key", "<x>", null, null, "domain")]
    public void CreateJobRecaptchaV2Async_InvalidText_ThrowsWithoutCallingChannel(string siteKey, string domain, string? stoken, string? reason, string paramName)
    {
        var (channel, api) = CreateCreate();

        var thrown = Assert.Throws<ArgumentException>(() => api.CreateJobRecaptchaV2Async(siteKey, domain, stoken, reason));

        Assert.That(thrown!.ParamName, Is.EqualTo(paramName));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase("6Le-abc_DEF123", "sub.example.com", "tok-EN_1")]
    [TestCase("key", "127.0.0.1", null)]
    public async Task CreateJobRecaptchaV2Async_SafeValues_AreAccepted(string siteKey, string domain, string? stoken)
    {
        var (channel, api) = CreateCreate();

        await api.CreateJobRecaptchaV2Async(siteKey, domain, stoken);

        Assert.That(channel.ReceivedCalls(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task GetResultAsync_Call_UsesExactPathAndJobId()
    {
        var (channel, api) = CreateResult();

        var token = await api.GetResultAsync(42);

        Assert.Multiple(() =>
        {
            Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/captchaforward/getResult"));
            Assert.That(SentArgs(channel), Is.EqualTo(_expectedResultArgs));
            Assert.That(token, Is.EqualTo("token"));
        });
    }

    [Test]
    public async Task GetResultAsync_NullToken_IsPassedThrough()
    {
        var (_, api) = CreateResult(null);

        var token = await api.GetResultAsync(42);

        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task GetResultAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateResult();

        await api.GetResultAsync(42, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }
}
