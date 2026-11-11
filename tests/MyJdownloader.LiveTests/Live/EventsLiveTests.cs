namespace MyJdownloader.LiveTests.Live;

[TestFixture]
[Category("Live")]
public sealed class EventsLiveTests
{
    [Test]
    [CancelAfter(60_000)]
    public async Task Publishers_NotEmpty(CancellationToken cancellationToken)
    {
        var publishers = await LiveGuard.RunAsync(() => LiveSession.Device.Events.ListPublishersAsync(cancellationToken));

        Assert.That(publishers, Is.Not.Empty);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task SubscribeListenUnsubscribe_Works(CancellationToken cancellationToken)
    {
        var events = LiveSession.Device.Events;
        var subscription = await LiveGuard.RunAsync(() => events.SubscribeAsync(["downloadwatchdog.*"], cancellationToken: cancellationToken));
        try
        {
            Assert.That(subscription.Subscribed, Is.True);
            var batch = await LiveGuard.RunAsync(() => events.ListenAsync(subscription.SubscriptionId, cancellationToken));
            Assert.That(batch, Is.Not.Null);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await events.UnsubscribeAsync(subscription.SubscriptionId, cleanup.Token);
            }
            catch (Exception ex) when (ex is MyJdException or OperationCanceledException)
            {
                await TestContext.Out.WriteLineAsync("WARNING: unsubscribe cleanup failed");
            }
        }
    }
}
