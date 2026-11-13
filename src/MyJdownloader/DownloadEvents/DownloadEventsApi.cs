namespace MyJdownloader.DownloadEvents;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/downloadevents</c> namespace of one device (the push interval of the download events).</summary>
/// <remarks>
/// <para>The facade exposes 1 of the 2 actions. <c>/downloadevents/queryLinks</c> is not exposed because the device always fails it ("Not Implemented").</para>
/// <para>See also <see cref="Events.EventsApi"/>, which creates the events subscriptions.</para>
/// </remarks>
public sealed class DownloadEventsApi
{
    private const string Base = "/downloadevents/";

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="DownloadEventsApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public DownloadEventsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Sets the push interval of the <c>downloads.*</c> interval events for one events subscription (<c>/downloadevents/setStatusEventInterval</c>).</summary>
    /// <param name="subscriptionId">The subscription to configure, see <see cref="Events.EventSubscription.SubscriptionId"/>.</param>
    /// <param name="interval">The new interval, sent as whole milliseconds (a fraction of a millisecond is truncated). The device default is 1000 ms.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the interval is set; <see langword="false"/> when the subscription is unknown or its channel is closed.</returns>
    /// <remarks>The interval applies until the subscription ends. Intervals well below the 1000 ms default raise the load on the device and on the relay.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is shorter than 1 millisecond.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a missing result).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> SetStatusEventIntervalAsync(long subscriptionId, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var milliseconds = (long)interval.TotalMilliseconds;
        if (milliseconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be at least 1 millisecond.");
        }

        JsonElement[] args =
        [
            JsonSerializer.SerializeToElement(subscriptionId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(milliseconds, FacadeJsonContext.Default.Int64),
        ];
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "setStatusEventInterval", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }
}
