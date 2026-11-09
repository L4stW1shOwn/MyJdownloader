namespace MyJdownloader.Tests.ContentV2;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.ContentV2;
using NSubstitute;

[TestFixture]
public sealed class ContentV2ApiTests
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1];

    private static IEnumerable<string?> BadTexts()
    {
        yield return null;
        yield return string.Empty;
        yield return "  ";
        yield return "a\nb";
        yield return "a\0b";
    }

    private static IEnumerable<TestCaseData> TextMethods()
    {
        yield return new TestCaseData((Func<ContentV2Api, string, Task>)((api, text) => api.GetFavIconAsync(text))).SetName("GetFavIconAsync");
        yield return new TestCaseData((Func<ContentV2Api, string, Task>)((api, text) => api.GetFileIconAsync(text))).SetName("GetFileIconAsync");
        yield return new TestCaseData((Func<ContentV2Api, string, Task>)((api, text) => api.GetIconAsync(text, 16))).SetName("GetIconAsync");
        yield return new TestCaseData((Func<ContentV2Api, string, Task>)((api, text) => api.GetIconDescriptionAsync(text))).SetName("GetIconDescriptionAsync");
    }

    private static (FakeBinaryChannel Channel, ContentV2Api Api) Create()
    {
        var channel = new FakeBinaryChannel(_png);
        return (channel, new ContentV2Api(channel));
    }

    private static void AssertBinarySent(FakeBinaryChannel binary, string path, params string[] expectedRawJson)
    {
        var (sentPath, args, _) = binary.Calls.Single();
        Assert.That(sentPath, Is.EqualTo(path));
        Assert.That(args, Has.Count.EqualTo(expectedRawJson.Length));
        for (var i = 0; i < expectedRawJson.Length; i++)
        {
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(args[i].GetRawText()), JsonNode.Parse(expectedRawJson[i])), Is.True, args[i].GetRawText());
        }
    }

    [Test]
    public async Task GetFavIconAsync_SendsHosterNameRaw()
    {
        var (binary, api) = Create();

        var bytes = await api.GetFavIconAsync("rapidgator.net");

        AssertBinarySent(binary, "/contentV2/getFavIcon", /*lang=json,strict*/ "\"rapidgator.net\"");
        Assert.That(bytes, Is.SameAs(_png));
    }

    [Test]
    public async Task GetFileIconAsync_SendsFileName()
    {
        var (binary, api) = Create();

        var bytes = await api.GetFileIconAsync(" a\"b.mkv ");

        AssertBinarySent(binary, "/contentV2/getFileIcon", /*lang=json,strict*/ "\" a\\\"b.mkv \"");
        Assert.That(bytes, Is.SameAs(_png));
    }

    [Test]
    public async Task GetIconAsync_SendsKeyAndSize()
    {
        var (binary, api) = Create();

        var bytes = await api.GetIconAsync("kc.abc", 16);

        AssertBinarySent(binary, "/contentV2/getIcon", /*lang=json,strict*/ "\"kc.abc\"", "16");
        Assert.That(bytes, Is.SameAs(_png));
    }

    [TestCase(1)]
    [TestCase(128)]
    public async Task GetIconAsync_SizeBounds_Accepted(int size)
    {
        var (binary, api) = Create();

        await api.GetIconAsync("k", size);

        AssertBinarySent(binary, "/contentV2/getIcon", /*lang=json,strict*/ "\"k\"", size.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestCase(0)]
    [TestCase(129)]
    [TestCase(-1)]
    public void GetIconAsync_SizeOutOfRange_Throws(int size)
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.GetIconAsync("k", size));
        Assert.That(ex!.Message, Does.Not.Contain("129").And.Not.Contain("-1"));
        Assert.That(channel.Calls, Is.Empty);
    }

    [TestCaseSource(nameof(TextMethods))]
    public void Methods_NullBlankOrControlChars_Throw(Func<ContentV2Api, string, Task> call)
    {
        var (channel, api) = Create();

        foreach (var text in BadTexts())
        {
            Assert.That(() => call(api, text!), Throws.InstanceOf<ArgumentException>());
        }

        Assert.That(channel.Calls, Is.Empty);
    }

    [Test]
    public void IconMethods_ChannelWithoutBinary_ThrowNotSupportedException()
    {
        var api = new ContentV2Api(Substitute.For<IDeviceChannel>());

        Assert.Multiple(() =>
        {
            Assert.Throws<NotSupportedException>(() => api.GetFavIconAsync("a"));
            Assert.Throws<NotSupportedException>(() => api.GetFileIconAsync("a"));
            Assert.Throws<NotSupportedException>(() => api.GetIconAsync("a", 16));
        });
    }

    [Test]
    public async Task GetIconDescriptionAsync_NullData_ReturnsNull()
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/contentV2/getIconDescription", Arg.Any<JsonTypeInfo<IconDescriptor>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IconDescriptor?>(null));

        var result = await new ContentV2Api(channel).GetIconDescriptionAsync("kc.abc");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetIconDescriptionAsync_ReturnsDescriptor()
    {
        var descriptor = new IconDescriptor { Key = "kc.abc" };
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/contentV2/getIconDescription", Arg.Any<JsonTypeInfo<IconDescriptor>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IconDescriptor?>(descriptor));

        var result = await new ContentV2Api(channel).GetIconDescriptionAsync("kc.abc");

        var args = channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single();
        Assert.That(result, Is.SameAs(descriptor));
        Assert.That(args.Single().GetString(), Is.EqualTo("kc.abc"));
    }

    [Test]
    public async Task Methods_CancellationToken_IsForwarded()
    {
        var (channel, api) = Create();
        using var cts = new CancellationTokenSource();

        await api.GetFavIconAsync("a", cts.Token);

        Assert.That(channel.Calls.Single().Token, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetIconDescriptionAsync_CancellationToken_IsForwarded()
    {
        var channel = Substitute.For<IDeviceChannel>();
        using var cts = new CancellationTokenSource();

        await new ContentV2Api(channel).GetIconDescriptionAsync("a", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void ContentV2_Property_IsCached()
    {
        var device = new JDownloaderDevice("dev1", Substitute.For<IDeviceChannel>());

        Assert.That(device.ContentV2, Is.SameAs(device.ContentV2));
    }
}
