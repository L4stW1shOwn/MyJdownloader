namespace MyJdownloader.Tests.Events;

using System.Text.Json;
using MyJdownloader.Events;
using NSubstitute;

[TestFixture]
public sealed class EventStreamTests
{
    private const string Base = "/events/";

    private static readonly string[] _downloads = ["downloads.*"];

    private static (IDeviceChannel Channel, EventsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        Sub(channel, "subscribe", 1);
        Sub(channel, "changesubscriptiontimeouts", 1);
        Sub(channel, "unsubscribe", 1);
        return (channel, new EventsApi(channel));
    }

    private static void Sub(IDeviceChannel channel, string path, long id) =>
        channel.CallAsync(Base + path, EventsJsonContext.Default.EventSubscription, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventSubscription?>(new EventSubscription { SubscriptionId = id }));

    private static void SubSequence(IDeviceChannel channel, params long[] ids) =>
        channel.CallAsync(Base + "subscribe", EventsJsonContext.Default.EventSubscription, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<EventSubscription?>(new EventSubscription { SubscriptionId = ids[0] }),
                [.. ids.Skip(1).Select(i => Task.FromResult<EventSubscription?>(new EventSubscription { SubscriptionId = i }))]);

    private static void ListenBatches(IDeviceChannel channel, params List<DeviceEvent>[] batches) =>
        channel.CallAsync(Base + "listen", EventsJsonContext.Default.ListDeviceEvent, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<List<DeviceEvent>?>(batches[0]),
                [.. batches.Skip(1).Select(Task.FromResult<List<DeviceEvent>?>)]);

    private static void ListenCancels(IDeviceChannel channel, CancellationTokenSource cts) =>
        channel.CallAsync(Base + "listen", EventsJsonContext.Default.ListDeviceEvent, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return await Task.FromCanceled<List<DeviceEvent>?>(cts.Token);
            });

    private static T[] Seq<T>(params T[] items) => items;

    private static DeviceEvent Evt(string id) => new() { Publisher = "p", EventId = id };

    private static List<(string Path, string[] Args, CancellationToken Token)> Calls(IDeviceChannel channel) =>
        [.. channel.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IDeviceChannel.CallAsync))
            .Select(c =>
            {
                var a = c.GetArguments();
                return (
                    ((string)a[0]!)[Base.Length..],
                    a.OfType<IReadOnlyList<JsonElement>>().Single().Select(e => e.GetRawText()).ToArray(),
                    a.OfType<CancellationToken>().Single());
            })];

    private static EventStreamOptions Options(TimeSpan? emptyPollDelay = null) => new()
    {
        Subscriptions = _downloads,
        EmptyPollDelay = emptyPollDelay ?? TimeSpan.Zero,
    };

    private static async Task<List<DeviceEvent>> Collect(IAsyncEnumerable<DeviceEvent> stream, int take)
    {
        var list = new List<DeviceEvent>();
        await foreach (var e in stream)
        {
            list.Add(e);
            if (list.Count == take)
            {
                break;
            }
        }

        return list;
    }

    [Test]
    public void StreamAsync_NullOptions_ThrowsArgumentNullException()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentNullException>(() => api.StreamAsync(null!));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void StreamAsync_NullSubscriptions_ThrowsArgumentNullException()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentNullException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = null! }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase(" ")]
    [TestCase("a\nb")]
    [TestCase(null)]
    public void StreamAsync_InvalidSubscriptionItem_ThrowsArgumentException(string? item)
    {
        var (channel, api) = Create();

        Assert.Catch<ArgumentException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = [item!] }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [TestCase(" ")]
    [TestCase("a\nb")]
    [TestCase(null)]
    public void StreamAsync_InvalidExclusionItem_ThrowsArgumentException(string? item)
    {
        var (channel, api) = Create();

        Assert.Catch<ArgumentException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = _downloads, Exclusions = [item!] }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void StreamAsync_PollTimeoutBelowOneMillisecond_ThrowsArgumentOutOfRangeException()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = _downloads, PollTimeout = TimeSpan.Zero }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void StreamAsync_MaxKeepaliveBelowOneMillisecond_ThrowsArgumentOutOfRangeException()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = _downloads, MaxKeepalive = TimeSpan.Zero }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void StreamAsync_PollTimeoutNotBelowMaxKeepalive_ThrowsArgumentException()
    {
        var (channel, api) = Create();
        var options = new EventStreamOptions
        {
            Subscriptions = _downloads,
            PollTimeout = TimeSpan.FromSeconds(30),
            MaxKeepalive = TimeSpan.FromSeconds(30),
        };

        Assert.Throws<ArgumentException>(() => api.StreamAsync(options));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void StreamAsync_NegativeEmptyPollDelay_ThrowsArgumentOutOfRangeException()
    {
        var (channel, api) = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => api.StreamAsync(new EventStreamOptions { Subscriptions = _downloads, EmptyPollDelay = TimeSpan.FromMilliseconds(-1) }));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public void EventStreamOptions_Defaults_MatchSpec()
    {
        var options = new EventStreamOptions { Subscriptions = _downloads };

        Assert.That(options.PollTimeout, Is.EqualTo(TimeSpan.FromSeconds(25)));
        Assert.That(options.MaxKeepalive, Is.EqualTo(TimeSpan.FromMinutes(2)));
        Assert.That(options.EmptyPollDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));
        Assert.That(options.Exclusions, Is.Null);
    }

    [Test]
    public async Task StreamAsync_Start_SubscribesThenSetsTimeoutsThenListens()
    {
        var (channel, api) = Create();
        ListenBatches(channel, [Evt("e1")]);

        await Collect(api.StreamAsync(Options()), 1);

        var calls = Calls(channel);
        Assert.That(calls.Select(c => c.Path), Is.EqualTo(Seq("subscribe", "changesubscriptiontimeouts", "listen", "unsubscribe")));
        Assert.That(calls[0].Args, Is.EqualTo(Seq("[\"downloads.*\"]", "[]")));
        Assert.That(calls[1].Args, Is.EqualTo(Seq("1", "25000", "120000")));
        Assert.That(calls[2].Args, Is.EqualTo(Seq("1")));
    }

    [Test]
    public async Task StreamAsync_SeveralBatches_YieldsEventsInOrder()
    {
        var (channel, api) = Create();
        DeviceEvent e1 = Evt("e1"), e2 = Evt("e2"), e3 = Evt("e3");
        ListenBatches(channel, [e1, e2], [], [e3]);

        var events = await Collect(api.StreamAsync(Options()), 3);

        Assert.That(events, Is.EqualTo(Seq(e1, e2, e3)));
    }

    [Test]
    public async Task StreamAsync_Break_UnsubscribesCurrentSubscription()
    {
        var (channel, api) = Create();
        ListenBatches(channel, [Evt("e1"), Evt("e2")]);

        await Collect(api.StreamAsync(Options()), 1);

        var unsubscribes = Calls(channel).Where(c => c.Path == "unsubscribe").ToList();
        Assert.That(unsubscribes, Has.Count.EqualTo(1));
        Assert.That(unsubscribes[0].Args, Is.EqualTo(Seq("1")));
        Assert.That(unsubscribes[0].Token, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public async Task StreamAsync_Cancelled_ThrowsAndUnsubscribes()
    {
        var (channel, api) = Create();
        using var cts = new CancellationTokenSource();
        ListenCancels(channel, cts);

        await Assert.CatchAsync<OperationCanceledException>(async () => await Collect(api.StreamAsync(Options(), cts.Token), 1));

        var unsubscribes = Calls(channel).Where(c => c.Path == "unsubscribe").ToList();
        Assert.That(unsubscribes, Has.Count.EqualTo(1));
        Assert.That(unsubscribes[0].Token, Is.EqualTo(CancellationToken.None));
    }

    [Test]
    public async Task StreamAsync_CancelledDuringEmptyPollDelay_ThrowsAndUnsubscribes()
    {
        var (channel, api) = Create();
        using var cts = new CancellationTokenSource();
        channel.CallAsync(Base + "listen", EventsJsonContext.Default.ListDeviceEvent, Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(50));
                return Task.FromResult<List<DeviceEvent>?>([]);
            });
        var started = System.Diagnostics.Stopwatch.StartNew();

        await Assert.CatchAsync<OperationCanceledException>(async () => await Collect(api.StreamAsync(Options(TimeSpan.FromSeconds(30)), cts.Token), 1));
        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(10)));
        Assert.That(Calls(channel).Count(c => c.Path == "unsubscribe"), Is.EqualTo(1));
    }

    [Test]
    public async Task StreamAsync_OptionsListMutatedAfterCall_UsesSnapshot()
    {
        var (channel, api) = Create();
        ListenBatches(channel, [Evt("e1")]);
        var subscriptions = new[] { "downloads.*" };
        var stream = api.StreamAsync(new EventStreamOptions { Subscriptions = subscriptions, EmptyPollDelay = TimeSpan.Zero });
        subscriptions[0] = "changed";

        await Collect(stream, 1);

        Assert.That(Calls(channel)[0].Args[0], Is.EqualTo("[\"downloads.*\"]"));
    }

    [Test]
    public async Task StreamAsync_TwoEnumerations_CreateTwoSubscriptions()
    {
        var (channel, api) = Create();
        SubSequence(channel, 1, 2);
        ListenBatches(channel, [Evt("e1")]);
        var stream = api.StreamAsync(Options());

        await Collect(stream, 1);
        await Collect(stream, 1);

        Assert.That(Calls(channel).Count(c => c.Path == "subscribe"), Is.EqualTo(2));
    }

    [Test]
    public void StreamAsync_NotEnumerated_MakesNoCall()
    {
        var (channel, api) = Create();

        _ = api.StreamAsync(Options());

        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }
}
