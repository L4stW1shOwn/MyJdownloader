namespace MyJdownloader.Events;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/events</c> namespace of one device (event subscriptions and long polling).</summary>
/// <remarks>
/// There is no built-in listen loop (ADR-009): call <see cref="ListenAsync"/> repeatedly. Subscription patterns are
/// regular expressions evaluated on the device, not by this library.
/// </remarks>
public sealed class EventsApi
{
    private const string Base = "/events/";

    private readonly IDeviceChannel _channel;

    internal EventsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Creates an event subscription (<c>/events/subscribe</c>).</summary>
    /// <remarks>Each pattern is a regular expression evaluated on the device.</remarks>
    /// <param name="subscriptions">The event patterns; each one is a device-side regular expression. May be empty.</param>
    /// <param name="exclusions">The excluded event patterns (device-side regular expressions), or <see langword="null"/> for none (sent as an empty array).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The resulting subscription.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <paramref name="subscriptions"/> or <paramref name="exclusions"/> is <see langword="null"/>, blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> SubscribeAsync(IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)
    {
        var args = ListArgs(subscriptions, exclusions);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "subscribe", EventsJsonContext.Default.EventSubscription, args, cancellationToken));
    }

    /// <summary>Waits for and returns the queued events of a subscription (<c>/events/listen</c>).</summary>
    /// <remarks>
    /// This is a long poll: the device holds the request until events are queued or the subscription poll timeout
    /// elapses, then answers with the queued events (possibly none). The call is bounded by <c>HttpClient.Timeout</c>
    /// (response body included), which the library neither raises nor bypasses, so keep the poll timeout well below it
    /// (see <see cref="ChangeSubscriptionTimeoutsAsync"/>; for example 25 s for the default 100 s client timeout).
    /// Responses are capped at 1 MiB. The shape of the events is undocumented (assumption to confirm by live tests).
    /// </remarks>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The queued events; empty when none arrived or the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<DeviceEvent>> ListenAsync(long subscriptionId, CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "listen", EventsJsonContext.Default.ListDeviceEvent, [Id(subscriptionId)], cancellationToken));

    /// <summary>Gets a subscription (<c>/events/getsubscription</c>).</summary>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The subscription.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> GetSubscriptionAsync(long subscriptionId, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getsubscription", EventsJsonContext.Default.EventSubscription, [Id(subscriptionId)], cancellationToken));

    /// <summary>Gets the status of a subscription (<c>/events/getsubscriptionstatus</c>).</summary>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The subscription status.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscriptionStatus> GetSubscriptionStatusAsync(long subscriptionId, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "getsubscriptionstatus", EventsJsonContext.Default.EventSubscriptionStatus, [Id(subscriptionId)], cancellationToken));

    /// <summary>Adds patterns to a subscription (<c>/events/addsubscription</c>).</summary>
    /// <remarks>Each pattern is a regular expression evaluated on the device.</remarks>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="subscriptions">The event patterns; each one is a device-side regular expression. May be empty.</param>
    /// <param name="exclusions">The excluded event patterns (device-side regular expressions), or <see langword="null"/> for none (sent as an empty array).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The resulting subscription.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <paramref name="subscriptions"/> or <paramref name="exclusions"/> is <see langword="null"/>, blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> AddSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)
    {
        var args = ListArgs(Id(subscriptionId), subscriptions, exclusions);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "addsubscription", EventsJsonContext.Default.EventSubscription, args, cancellationToken));
    }

    /// <summary>Removes patterns from a subscription (<c>/events/removesubscription</c>).</summary>
    /// <remarks>Each pattern is a regular expression evaluated on the device.</remarks>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="subscriptions">The event patterns; each one is a device-side regular expression. May be empty.</param>
    /// <param name="exclusions">The excluded event patterns (device-side regular expressions), or <see langword="null"/> for none (sent as an empty array).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The resulting subscription.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <paramref name="subscriptions"/> or <paramref name="exclusions"/> is <see langword="null"/>, blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> RemoveSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)
    {
        var args = ListArgs(Id(subscriptionId), subscriptions, exclusions);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "removesubscription", EventsJsonContext.Default.EventSubscription, args, cancellationToken));
    }

    /// <summary>Replaces the patterns of a subscription (<c>/events/setsubscription</c>).</summary>
    /// <remarks>Each pattern is a regular expression evaluated on the device.</remarks>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="subscriptions">The event patterns; each one is a device-side regular expression. May be empty.</param>
    /// <param name="exclusions">The excluded event patterns (device-side regular expressions), or <see langword="null"/> for none (sent as an empty array).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The resulting subscription.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <paramref name="subscriptions"/> or <paramref name="exclusions"/> is <see langword="null"/>, blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> SetSubscriptionAsync(long subscriptionId, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions = null, CancellationToken cancellationToken = default)
    {
        var args = ListArgs(Id(subscriptionId), subscriptions, exclusions);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "setsubscription", EventsJsonContext.Default.EventSubscription, args, cancellationToken));
    }

    /// <summary>Changes the timeouts of a subscription (<c>/events/changesubscriptiontimeouts</c>).</summary>
    /// <remarks>Sent as whole milliseconds (the fraction is truncated). Keep <paramref name="pollTimeout"/> well below <c>HttpClient.Timeout</c> (for example 25 s for the default 100 s client timeout).</remarks>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="pollTimeout">How long the device holds a <see cref="ListenAsync"/> call; at least 1 ms.</param>
    /// <param name="maxKeepalive">How long the device keeps the subscription without a listen call; at least 1 ms.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The updated subscription.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pollTimeout"/> or <paramref name="maxKeepalive"/> is below 1 ms.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> ChangeSubscriptionTimeoutsAsync(long subscriptionId, TimeSpan pollTimeout, TimeSpan maxKeepalive, CancellationToken cancellationToken = default)
    {
        var pollMs = Milliseconds(pollTimeout, nameof(pollTimeout));
        var keepaliveMs = Milliseconds(maxKeepalive, nameof(maxKeepalive));
        JsonElement[] args = [Id(subscriptionId), Id(pollMs), Id(keepaliveMs)];
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "changesubscriptiontimeouts", EventsJsonContext.Default.EventSubscription, args, cancellationToken));
    }

    /// <summary>Cancels a subscription (<c>/events/unsubscribe</c>).</summary>
    /// <param name="subscriptionId">The subscription identifier (<see cref="EventSubscription.SubscriptionId"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The subscription returned by the device; JDownloader does not document its content, so some properties may be absent.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the subscription is expired or unknown the device usually answers 404, which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<EventSubscription> UnsubscribeAsync(long subscriptionId, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "unsubscribe", EventsJsonContext.Default.EventSubscription, [Id(subscriptionId)], cancellationToken));

    /// <summary>Lists the event publishers and the events they emit (<c>/events/listpublisher</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The publishers; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<EventPublisher>> ListPublishersAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "listpublisher", EventsJsonContext.Default.ListEventPublisher, [], cancellationToken));

    /// <summary>Follows the events of the device as an asynchronous stream, owning the whole subscription lifecycle.</summary>
    /// <remarks>
    /// <para>
    /// Each enumeration creates its own device subscription (<see cref="SubscribeAsync"/>), sets its timeouts
    /// (<see cref="ChangeSubscriptionTimeoutsAsync"/>), then long-polls with <see cref="ListenAsync"/> and yields the
    /// events in the order received; after an empty batch it waits <see cref="EventStreamOptions.EmptyPollDelay"/>.
    /// Enumerating the same stream twice therefore creates two subscriptions.
    /// </para>
    /// <para>
    /// Whatever ends the enumeration (<see langword="break"/>, cancellation, exception, <c>DisposeAsync</c>), the
    /// subscription is removed with <see cref="UnsubscribeAsync"/> using its own token, independent of the caller's, so a
    /// cancelled stream still cleans up; the cleanup returns within 10 seconds (an in-flight session reconnect on the client
    /// may continue in the background). Failures of that cleanup
    /// call (<see cref="MyJdException"/>, <see cref="TimeoutException"/>, <see cref="HttpRequestException"/>,
    /// <see cref="IOException"/>, <see cref="OperationCanceledException"/>, <see cref="InvalidOperationException"/>)
    /// are ignored so they never mask the exception that ended the stream; the device then drops the subscription after
    /// <see cref="EventStreamOptions.MaxKeepalive"/>. An enumerator obtained manually and never disposed leaks the
    /// device subscription until <see cref="EventStreamOptions.MaxKeepalive"/>.
    /// </para>
    /// <para>
    /// The caller's token (and the one given through <c>WithCancellation</c>) flows into every call and delay. Nothing
    /// runs between two <c>MoveNextAsync</c> calls: no background task, no timer, no network retry (ADR-005).
    /// Events published while no subscription exists are lost. See ADR-010, "Opt-in event stream helper", for the
    /// rationale and the full failure policy.
    /// </para>
    /// </remarks>
    /// <param name="options">The patterns and polling options; copied at this call.</param>
    /// <param name="cancellationToken">A token to cancel the stream; combined with the one given to <c>WithCancellation</c>.</param>
    /// <returns>A lazy stream: no call is made until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or its <see cref="EventStreamOptions.Subscriptions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An item of <see cref="EventStreamOptions.Subscriptions"/> or <see cref="EventStreamOptions.Exclusions"/> is <see langword="null"/>, blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates; or <see cref="EventStreamOptions.PollTimeout"/> is not strictly below <see cref="EventStreamOptions.MaxKeepalive"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="EventStreamOptions.PollTimeout"/> or <see cref="EventStreamOptions.MaxKeepalive"/> is below 1 ms, or <see cref="EventStreamOptions.EmptyPollDelay"/> is negative.</exception>
    public IAsyncEnumerable<DeviceEvent> StreamAsync(EventStreamOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePatterns(options.Subscriptions, options.Exclusions);
        _ = Milliseconds(options.PollTimeout, nameof(options.PollTimeout));
        _ = Milliseconds(options.MaxKeepalive, nameof(options.MaxKeepalive));
        if (options.PollTimeout >= options.MaxKeepalive)
        {
            throw new ArgumentException("PollTimeout must be strictly below MaxKeepalive.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(options.EmptyPollDelay, TimeSpan.Zero, nameof(options.EmptyPollDelay));

        string[] subscriptions = [.. options.Subscriptions];
        string[]? exclusions = options.Exclusions is null ? null : [.. options.Exclusions];
        return EventStream.RunAsync(this, subscriptions, exclusions, options.PollTimeout, options.MaxKeepalive, options.EmptyPollDelay, cancellationToken);
    }

    internal static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(10);

    internal Task UnsubscribeQuietlyAsync(long subscriptionId) => this.UnsubscribeQuietlyAsync(subscriptionId, CleanupTimeout);

    internal async Task UnsubscribeQuietlyAsync(long subscriptionId, TimeSpan cleanupTimeout)
    {
        // Own token, independent of the caller's (typically already cancelled): bounds the cleanup (CWE-400).
        using var cleanup = new CancellationTokenSource(cleanupTimeout);
        Task<EventSubscription>? call = null;
        try
        {
            // WaitAsync bounds the wait itself: a session reconnect inside the channel ignores the token (ADR-004).
            call = this.UnsubscribeAsync(subscriptionId, cleanup.Token);
            _ = await call.WaitAsync(cleanupTimeout).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is MyJdException or TimeoutException or HttpRequestException or IOException or OperationCanceledException or InvalidOperationException)
        {
            // Best effort: never mask the exception that ended the stream; the device drops the subscription after MaxKeepalive.
            // The abandoned call may fault later: observe it so it never surfaces as an unobserved task exception.
            _ = call?.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    private static JsonElement[] ListArgs(JsonElement first, IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        return [first, StringList(subscriptions, nameof(subscriptions)), StringList(exclusions, nameof(exclusions))];
    }

    private static JsonElement[] ListArgs(IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        return [StringList(subscriptions, nameof(subscriptions)), StringList(exclusions, nameof(exclusions))];
    }

    private static void ValidatePatterns(IReadOnlyList<string> subscriptions, IReadOnlyList<string>? exclusions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        foreach (var value in subscriptions)
        {
            FacadeGuard.ValidateText(value, nameof(subscriptions), FacadeGuard.MaxLongTextLength);
        }

        foreach (var value in exclusions ?? [])
        {
            FacadeGuard.ValidateText(value, nameof(exclusions), FacadeGuard.MaxLongTextLength);
        }
    }

    private static JsonElement StringList(IReadOnlyList<string>? items, string paramName)
    {
        string[] values = items is null ? [] : [.. items];
        foreach (var value in values)
        {
            FacadeGuard.ValidateText(value, paramName, FacadeGuard.MaxLongTextLength);
        }

        return JsonSerializer.SerializeToElement(values, EventsJsonContext.Default.StringArray);
    }

    private static long Milliseconds(TimeSpan value, string paramName)
    {
        var ms = (long)value.TotalMilliseconds;
        if (ms < 1)
        {
            throw new ArgumentOutOfRangeException(paramName, "The duration must be at least 1 millisecond.");
        }

        return ms;
    }

    private static JsonElement Id(long value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.Int64);
}
