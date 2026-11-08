namespace MyJdownloader.Events;

using System.Net;
using System.Runtime.CompilerServices;

/// <summary>The iterator behind <see cref="EventsApi.StreamAsync"/>: subscription lifecycle, long polling and failure policy.</summary>
/// <remarks>
/// Kept apart from <see cref="EventsApi"/> so the loop and its single recovery branch stay readable. A 404 from
/// <c>listen</c> means the device dropped the subscription (expired keepalive or device restart): the stream then
/// subscribes again, without unsubscribing the dead id. A 404 right after a resubscription, with no successful
/// listen in between, is not an expiry but a persistent fault and is rethrown so the loop cannot spin. Any other
/// failure propagates. The current subscription is always removed on exit, best effort, bounded by 10 seconds and independent of the caller's token (ADR-010).
/// </remarks>
internal static class EventStream
{
    public static async IAsyncEnumerable<DeviceEvent> RunAsync(
        EventsApi api,
        string[] subscriptions,
        string[]? exclusions,
        TimeSpan pollTimeout,
        TimeSpan maxKeepalive,
        TimeSpan emptyPollDelay,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        long? held = null;
        var resubscribed = false;
        try
        {
            while (true)
            {
                var subscription = await api.SubscribeAsync(subscriptions, exclusions, cancellationToken).ConfigureAwait(false);
                held = subscription.SubscriptionId;
                await api.ChangeSubscriptionTimeoutsAsync(held.Value, pollTimeout, maxKeepalive, cancellationToken).ConfigureAwait(false);

                var listened = false;
                var expired = false;
                while (!expired)
                {
                    IReadOnlyList<DeviceEvent> batch = [];
                    try
                    {
                        batch = await api.ListenAsync(held.Value, cancellationToken).ConfigureAwait(false);
                    }
                    catch (MyJdApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound && (listened || !resubscribed))
                    {
                        expired = true;
                    }

                    if (expired)
                    {
                        break;
                    }

                    listened = true;
                    foreach (var evt in batch)
                    {
                        yield return evt;
                    }

                    if (batch.Count == 0 && emptyPollDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(emptyPollDelay, cancellationToken).ConfigureAwait(false);
                    }
                }

                // The device already dropped this id: nothing to unsubscribe.
                held = null;
                resubscribed = true;
            }
        }
        finally
        {
            if (held is { } id)
            {
                await api.UnsubscribeQuietlyAsync(id).ConfigureAwait(false);
            }
        }
    }
}
