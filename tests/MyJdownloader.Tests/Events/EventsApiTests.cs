namespace MyJdownloader.Tests.Events;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Events;
using NSubstitute;

[TestFixture]
public sealed class EventsApiTests
{
    private const string Base = "/events/";

    private static readonly string[] _subscriptions = ["downloads.*"];
    private static readonly string[] _exclusions = ["x"];
    private static readonly string[] _single = ["a"];
    private static readonly string[] _blankItem = ["ok", " "];
    private static readonly string[] _controlItem = ["ok", "secret\nx"];
    private static readonly string?[] _nullItem = ["ok", null];

    private static (IDeviceChannel Channel, EventsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new EventsApi(channel));
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

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void ReturnsSubscription(IDeviceChannel channel, string path, EventSubscription? value) =>
        channel.CallAsync(Base + path, EventsJsonContext.Default.EventSubscription, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    private static Task<EventSubscription> InvokeList(
        EventsApi api,
        string method,
        IReadOnlyList<string> subscriptions,
        IReadOnlyList<string>? exclusions) =>
        method switch
        {
            "subscribe" => api.SubscribeAsync(subscriptions, exclusions),
            "addsubscription" => api.AddSubscriptionAsync(5, subscriptions, exclusions),
            "removesubscription" => api.RemoveSubscriptionAsync(5, subscriptions, exclusions),
            "setsubscription" => api.SetSubscriptionAsync(5, subscriptions, exclusions),
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };

    private static Task<EventSubscription> InvokeList(
        EventsApi api,
        string method,
        IReadOnlyList<string> subscriptions) =>
        InvokeList(api, method, subscriptions, null);

    [Test]
    public async Task SubscribeAsync_Valid_SendsPathAndArgs()
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, "subscribe", new EventSubscription { SubscriptionId = 5 });

        var result = await api.SubscribeAsync(_subscriptions, _exclusions);

        AssertSent(channel, "subscribe", "[\"downloads.*\"]", "[\"x\"]");
        Assert.That(result.SubscriptionId, Is.EqualTo(5));
    }

    [Test]
    public async Task SubscribeAsync_NullExclusions_SendsEmptyArray()
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, "subscribe", new EventSubscription());

        await api.SubscribeAsync(_subscriptions);

        AssertSent(channel, "subscribe", "[\"downloads.*\"]", "[]");
    }

    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public async Task SubscriptionListMethods_SendIdAndLists(string method)
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, method, new EventSubscription());

        await InvokeList(api, method, _single);

        AssertSent(channel, method, "5", "[\"a\"]", "[]");
    }

    [TestCase("listen")]
    [TestCase("getsubscription")]
    [TestCase("getsubscriptionstatus")]
    [TestCase("unsubscribe")]
    public async Task IdMethods_SendId(string method)
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listen", new List<DeviceEvent>());
        ReturnsRef(channel, "getsubscription", new EventSubscription());
        ReturnsRef(channel, "getsubscriptionstatus", new EventSubscriptionStatus());
        ReturnsRef(channel, "unsubscribe", new EventSubscription());

        Task task = method switch
        {
            "listen" => api.ListenAsync(5),
            "getsubscription" => api.GetSubscriptionAsync(5),
            "getsubscriptionstatus" => api.GetSubscriptionStatusAsync(5),
            _ => api.UnsubscribeAsync(5),
        };
        await task;

        AssertSent(channel, method, "5");
    }

    [Test]
    public async Task ListenAsync_Data_ReturnsEvents()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listen", new List<DeviceEvent> { new() { Publisher = "p", EventId = "e" } });

        var events = await api.ListenAsync(5);

        Assert.That(events.Single().EventId, Is.EqualTo("e"));
    }

    [Test]
    public async Task ListenAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<DeviceEvent>>(channel, "listen", null);

        var events = await api.ListenAsync(5);

        Assert.That(events, Is.Empty);
    }

    [Test]
    public void ListenAsync_NullItem_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listen", new List<DeviceEvent> { null! });

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.ListenAsync(5));
    }

    [Test]
    public async Task ListPublishersAsync_Valid_NoArgs()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listpublisher", new List<EventPublisher> { new() { Publisher = "downloads" } });

        var publishers = await api.ListPublishersAsync();

        AssertSent(channel, "listpublisher");
        Assert.That(publishers.Single().Publisher, Is.EqualTo("downloads"));
    }

    [Test]
    public async Task ListPublishersAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<EventPublisher>>(channel, "listpublisher", null);

        var publishers = await api.ListPublishersAsync();

        Assert.That(publishers, Is.Empty);
    }

    [Test]
    public async Task ChangeSubscriptionTimeoutsAsync_Valid_SendsMilliseconds()
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, "changesubscriptiontimeouts", new EventSubscription());

        await api.ChangeSubscriptionTimeoutsAsync(5, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(120));

        AssertSent(channel, "changesubscriptiontimeouts", "5", "25000", "120000");
    }

    [Test]
    public async Task ChangeSubscriptionTimeoutsAsync_FractionalMs_Truncates()
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, "changesubscriptiontimeouts", new EventSubscription());

        await api.ChangeSubscriptionTimeoutsAsync(5, TimeSpan.FromMilliseconds(1.9), TimeSpan.FromMilliseconds(1.9));

        AssertSent(channel, "changesubscriptiontimeouts", "5", "1", "1");
    }

    [TestCase("pollTimeout", 0.5)]
    [TestCase("pollTimeout", 0.0)]
    [TestCase("pollTimeout", -1.0)]
    [TestCase("maxKeepalive", 0.5)]
    [TestCase("maxKeepalive", 0.0)]
    [TestCase("maxKeepalive", -1.0)]
    public void ChangeSubscriptionTimeoutsAsync_BelowOneMs_Throws(string parameter, double milliseconds)
    {
        var (channel, api) = Create();
        var bad = TimeSpan.FromMilliseconds(milliseconds);
        var good = TimeSpan.FromSeconds(25);

        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.ChangeSubscriptionTimeoutsAsync(
                5,
                parameter == "pollTimeout" ? bad : good,
                parameter == "maxKeepalive" ? bad : good));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo(parameter));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("getsubscription")]
    [TestCase("getsubscriptionstatus")]
    [TestCase("changesubscriptiontimeouts")]
    [TestCase("unsubscribe")]
    public async Task SubscriptionResultMethods_NullData_ThrowsProtocol(string method)
    {
        var (channel, api) = Create();
        ReturnsSubscription(channel, method, null);
        ReturnsRef<EventSubscriptionStatus>(channel, "getsubscriptionstatus", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () =>
        {
            switch (method)
            {
                case "subscribe":
                    await api.SubscribeAsync(_subscriptions);
                    break;
                case "getsubscription":
                    await api.GetSubscriptionAsync(5);
                    break;
                case "getsubscriptionstatus":
                    await api.GetSubscriptionStatusAsync(5);
                    break;
                case "changesubscriptiontimeouts":
                    await api.ChangeSubscriptionTimeoutsAsync(5, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(120));
                    break;
                default:
                    await api.UnsubscribeAsync(5);
                    break;
            }
        });

        AssertDeviceUnreadable(thrown);
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_NullSubscriptions_Throws(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => InvokeList(api, method, null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("subscriptions"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_BlankSubscriptionItem_Throws(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _blankItem));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("subscriptions"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_ControlSubscriptionItem_ThrowsWithoutValue(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _controlItem));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("subscriptions"));
            Assert.That(thrown.Message, Does.Not.Contain("secret"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_NullSubscriptionItem_Throws(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _nullItem!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("subscriptions"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_BlankExclusionItem_Throws(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _single, _blankItem));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("exclusions"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_ControlExclusionItem_ThrowsWithoutValue(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _single, _controlItem));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("exclusions"));
            Assert.That(thrown.Message, Does.Not.Contain("secret"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("subscribe")]
    [TestCase("addsubscription")]
    [TestCase("removesubscription")]
    [TestCase("setsubscription")]
    public void ListMethods_NullExclusionItem_Throws(string method)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => InvokeList(api, method, _single, _nullItem!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("exclusions"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task AllMethods_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var (channel, api) = Create();
        ReturnsSubscription(channel, "subscribe", new EventSubscription());
        ReturnsRef(channel, "listen", new List<DeviceEvent>());
        ReturnsRef(channel, "getsubscriptionstatus", new EventSubscriptionStatus());
        ReturnsRef(channel, "listpublisher", new List<EventPublisher>());

        await api.SubscribeAsync(_subscriptions, null, token);
        await api.ListenAsync(5, token);
        await api.GetSubscriptionStatusAsync(5, token);
        await api.ListPublishersAsync(token);

        var tokens = channel.ReceivedCalls().Select(c => c.GetArguments().Last());
        Assert.That(tokens, Is.All.EqualTo(token));
        Assert.That(channel.ReceivedCalls().Count(), Is.EqualTo(4));
    }

    [Test]
    public void Device_Events_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Events, Is.SameAs(device.Events));
    }
}
