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
            await events.UnsubscribeAsync(subscription.SubscriptionId, CancellationToken.None);
        }
    }
}
