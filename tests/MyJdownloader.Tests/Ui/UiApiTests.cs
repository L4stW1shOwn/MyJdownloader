namespace MyJdownloader.Tests.Ui;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Ui;
using NSubstitute;

[TestFixture]
public sealed class UiApiTests
{
    private static readonly string[] _expectedMenuArgs = ["\"DLC\""];
    private static readonly string[] _expectedLgcArgs = ["\"LGC\""];
    private static readonly string[] _expectedInvokeArgs = ["\"DLC\"", "\"checkonline\"", "[1,2]", "[3]"];
    private static readonly string[] _expectedEmptyInvokeArgs = ["\"DLC\"", "\"forcedls\"", "[]", "[]"];

    private static (IDeviceChannel Channel, UiApi Api) CreateMenu(MenuItem? returned = null)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/ui/getMenu", Arg.Any<JsonTypeInfo<MenuItem>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new UiApi(channel));
    }

    private static (IDeviceChannel Channel, UiApi Api) CreateInvoke(JsonElement? returned = null)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync("/ui/invokeAction", Arg.Any<JsonTypeInfo<JsonElement?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new UiApi(channel));
    }

    private static string[] SentArgs(IDeviceChannel channel) =>
        [.. channel.ReceivedCalls().Single().GetArguments().OfType<IReadOnlyList<JsonElement>>().Single().Select(a => a.GetRawText())];

    [Test]
    public async Task GetMenuAsync_Call_UsesExactPathAndContextArgument()
    {
        var (channel, api) = CreateMenu(new MenuItem());

        await api.GetMenuAsync(MenuContext.Dlc);

        Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/ui/getMenu"));
        Assert.That(SentArgs(channel), Is.EqualTo(_expectedMenuArgs));
    }

    [Test]
    public async Task GetMenuAsync_Menu_IsReturned()
    {
        var menu = new MenuItem { Children = [new MenuItem { Id = "forcedls", Type = MenuItemType.Action }] };
        var (_, api) = CreateMenu(menu);

        var result = await api.GetMenuAsync(MenuContext.Dlc);

        Assert.That(result, Is.SameAs(menu));
    }

    [Test]
    public void GetMenuAsync_NullMenu_ThrowsProtocolException()
    {
        var (_, api) = CreateMenu(null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetMenuAsync(MenuContext.Dlc));
    }

    [Test]
    public void GetMenuAsync_NullChild_ThrowsProtocolException()
    {
        var menu = new MenuItem { Children = [new MenuItem { Children = [null!] }] };
        var (_, api) = CreateMenu(menu);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetMenuAsync(MenuContext.Dlc));
    }

    [Test]
    public void GetMenuAsync_NullChildAtDepthThree_ThrowsProtocolException()
    {
        var menu = new MenuItem { Children = [new MenuItem { Children = [new MenuItem { Children = [null!] }] }] };
        var (_, api) = CreateMenu(menu);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.GetMenuAsync(MenuContext.Dlc));
    }

    [Test]
    public async Task GetMenuAsync_LgcContext_SendsLgcArgument()
    {
        var (channel, api) = CreateMenu(new MenuItem());

        await api.GetMenuAsync(MenuContext.Lgc);

        Assert.That(SentArgs(channel), Is.EqualTo(_expectedLgcArgs));
    }

    [Test]
    public async Task GetMenuAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateMenu(new MenuItem());

        await api.GetMenuAsync(MenuContext.Dlc, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [TestCase(MenuContext.Unknown)]
    [TestCase((MenuContext)99)]
    public void GetMenuAsync_InvalidContext_ThrowsWithoutCallingChannel(MenuContext context)
    {
        var (channel, api) = CreateMenu(new MenuItem());

        var thrown = Assert.Throws<ArgumentException>(() => api.GetMenuAsync(context));

        Assert.That(thrown!.ParamName, Is.EqualTo("context"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task InvokeActionAsync_Call_UsesExactPathAndArguments()
    {
        var (channel, api) = CreateInvoke();

        await api.InvokeActionAsync(MenuContext.Dlc, "checkonline", [1, 2], [3]);

        Assert.That((string)channel.ReceivedCalls().Single().GetArguments()[0]!, Is.EqualTo("/ui/invokeAction"));
        Assert.That(SentArgs(channel), Is.EqualTo(_expectedInvokeArgs));
    }

    [Test]
    public async Task InvokeActionAsync_EmptyLists_SendsEmptyArrays()
    {
        var (channel, api) = CreateInvoke();

        await api.InvokeActionAsync(MenuContext.Dlc, "forcedls", [], []);

        Assert.That(SentArgs(channel), Is.EqualTo(_expectedEmptyInvokeArgs));
    }

    [Test]
    public async Task InvokeActionAsync_NullData_ReturnsNullElement()
    {
        var (_, api) = CreateInvoke(null);

        var result = await api.InvokeActionAsync(MenuContext.Dlc, "forcedls", [], []);

        Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task InvokeActionAsync_Result_IsPassedThrough()
    {
        using var document = JsonDocument.Parse(/*lang=json,strict*/ """{"done":true}""");
        var (_, api) = CreateInvoke(document.RootElement.Clone());

        var result = await api.InvokeActionAsync(MenuContext.Dlc, "forcedls", [], []);

        Assert.That(result.GetRawText(), Is.EqualTo(/*lang=json,strict*/ """{"done":true}"""));
    }

    [Test]
    public async Task InvokeActionAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = CreateInvoke();

        await api.InvokeActionAsync(MenuContext.Dlc, "forcedls", [], [], cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [TestCase(MenuContext.Unknown)]
    [TestCase((MenuContext)99)]
    public void InvokeActionAsync_InvalidContext_ThrowsWithoutCallingChannel(MenuContext context)
    {
        var (channel, api) = CreateInvoke();

        var thrown = Assert.Throws<ArgumentException>(() => api.InvokeActionAsync(context, "forcedls", [], []));

        Assert.That(thrown!.ParamName, Is.EqualTo("context"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void InvokeActionAsync_NullId_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateInvoke();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.InvokeActionAsync(MenuContext.Dlc, null!, [], []));

        Assert.That(thrown!.ParamName, Is.EqualTo("id"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("a\nb")]
    [TestCase("a\rb")]
    public void InvokeActionAsync_InvalidId_ThrowsWithoutCallingChannel(string id)
    {
        var (channel, api) = CreateInvoke();

        var thrown = Assert.Throws<ArgumentException>(() => api.InvokeActionAsync(MenuContext.Dlc, id, [], []));

        Assert.That(thrown!.ParamName, Is.EqualTo("id"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void InvokeActionAsync_LoneSurrogateId_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateInvoke();
        var id = "a" + (char)0xD800;

        var thrown = Assert.Throws<ArgumentException>(() => api.InvokeActionAsync(MenuContext.Dlc, id, [], []));

        Assert.That(thrown!.ParamName, Is.EqualTo("id"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void InvokeActionAsync_NullLinkIds_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateInvoke();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.InvokeActionAsync(MenuContext.Dlc, "forcedls", null!, []));

        Assert.That(thrown!.ParamName, Is.EqualTo("linkIds"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void InvokeActionAsync_NullPackageIds_ThrowsWithoutCallingChannel()
    {
        var (channel, api) = CreateInvoke();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.InvokeActionAsync(MenuContext.Dlc, "forcedls", [], null!));

        Assert.That(thrown!.ParamName, Is.EqualTo("packageIds"));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }
}
