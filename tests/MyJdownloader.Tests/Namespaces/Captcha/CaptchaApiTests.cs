namespace MyJdownloader.Tests.Captcha;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Captcha;
using MyJdownloader.Errors;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class CaptchaApiTests
{
    private const string Base = "/captcha/";

    private static (IDeviceChannel Channel, CaptchaApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new CaptchaApi(channel));
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

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task ListAsync_Data_ReturnsJobsAndSendsNoArgs()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<CaptchaJob> { new() { Id = 5, Hoster = "h" } });

        var jobs = await api.ListAsync();

        AssertSent(channel, "list");
        Assert.That(jobs.Single().Id, Is.EqualTo(5));
    }

    [Test]
    public async Task ListAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<CaptchaJob>>(channel, "list", null);

        var jobs = await api.ListAsync();

        Assert.That(jobs, Is.Empty);
    }

    [Test]
    public async Task GetJobAsync_Data_SendsIdAndReturnsJob()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getCaptchaJob", new CaptchaJob { Id = 42 });

        var job = await api.GetJobAsync(42);

        AssertSent(channel, "getCaptchaJob", "42");
        Assert.That(job.Id, Is.EqualTo(42));
    }

    [Test]
    public async Task GetJobAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef<CaptchaJob>(channel, "getCaptchaJob", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetJobAsync(42));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetImageAsync_NoFormat_SendsIdOnly()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "get", "data:image/png;base64,AAAA");

        var image = await api.GetImageAsync(42);

        AssertSent(channel, "get", "42");
        Assert.That(image, Is.EqualTo("data:image/png;base64,AAAA"));
    }

    [Test]
    public async Task GetImageAsync_Format_SendsIdAndFormat()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "get", "data:image/png;base64,AAAA");

        await api.GetImageAsync(42, "png");

        AssertSent(channel, "get", "42", "\"png\"");
    }

    [Test]
    public async Task GetImageAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef<string>(channel, "get", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetImageAsync(42));

        AssertDeviceUnreadable(thrown);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void GetImageAsync_BlankFormat_Throws(string format)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.GetImageAsync(42, format));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("format"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task SkipAsync_Single_SendsIdAndType()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "skip", true);

        var result = await api.SkipAsync(42, CaptchaSkipRequest.Single);

        AssertSent(channel, "skip", "42", "\"SINGLE\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SkipAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "skip", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SkipAsync(42, CaptchaSkipRequest.Single));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void SkipAsync_UnknownType_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SkipAsync(42, CaptchaSkipRequest.Unknown));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("type"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void SkipAsync_UndefinedType_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SkipAsync(42, (CaptchaSkipRequest)99));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("type"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void ListAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "list", new List<CaptchaJob> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.ListAsync());
    }

    [Test]
    public async Task SolveAsync_NoFormat_SendsIdAndResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "solve", true);

        var result = await api.SolveAsync(42, "abc");

        AssertSent(channel, "solve", "42", "\"abc\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SolveAsync_ResultFormat_SendsThreeArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "solve", true);

        await api.SolveAsync(42, "abc", resultFormat: "json");

        AssertSent(channel, "solve", "42", "\"abc\"", "\"json\"");
    }

    [Test]
    public async Task SolveAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "solve", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SolveAsync(42, "abc"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void SolveAsync_NullResult_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.SolveAsync(42, null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("result"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    public void SolveAsync_BlankResult_Throws(string result)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SolveAsync(42, result));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("result"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void SolveAsync_ControlCharacterResult_ThrowsWithoutValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SolveAsync(42, "secret\nanswer"));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("result"));
            Assert.That(thrown.Message, Does.Not.Contain("secret"));
            Assert.That(thrown.Message, Does.Not.Contain("answer"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    public void SolveAsync_BlankResultFormat_Throws(string resultFormat)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.SolveAsync(42, "abc", resultFormat));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("resultFormat"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task ListAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef<List<CaptchaJob>>(channel, "list", null);

        await api.ListAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetJobAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef(channel, "getCaptchaJob", new CaptchaJob { Id = 42 });

        await api.GetJobAsync(42, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetImageAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef(channel, "get", "data:image/png;base64,AAAA");

        await api.GetImageAsync(42, null, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task SkipAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "skip", true);

        await api.SkipAsync(42, CaptchaSkipRequest.Single, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task SolveAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "solve", true);

        await api.SolveAsync(42, "abc", null, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Captcha_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Captcha, Is.SameAs(device.Captcha));
    }

    [Test]
    public void GetImageAsync_FormatAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertSent(channel, () => api.GetImageAsync(42, TextCaps.Identifier));
    }

    [Test]
    public void GetImageAsync_FormatOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetImageAsync(42, TextCaps.IdentifierOver), "format");
    }

    [Test]
    public void SolveAsync_ResultAtLongTextMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertSent(channel, () => api.SolveAsync(42, TextCaps.LongText));
    }

    [Test]
    public void SolveAsync_ResultOverLongTextMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertRejected(channel, () => api.SolveAsync(42, TextCaps.LongTextOver), "result");
    }

    [Test]
    public void SolveAsync_ResultFormatAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertSent(channel, () => api.SolveAsync(42, "answer", TextCaps.Identifier));
    }

    [Test]
    public void SolveAsync_ResultFormatOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new CaptchaApi(channel);

        TextCaps.AssertRejected(channel, () => api.SolveAsync(42, "answer", TextCaps.IdentifierOver), "resultFormat");
    }
}
