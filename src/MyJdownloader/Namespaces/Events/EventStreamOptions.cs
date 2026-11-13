namespace MyJdownloader.Events;

/// <summary>Options of <see cref="EventsApi.StreamAsync"/>: which events to follow and how the device-side subscription is polled.</summary>
/// <remarks>
/// The options are validated, and the lists copied, when <see cref="EventsApi.StreamAsync"/> is called; later changes to
/// this instance or to the lists have no effect on a stream already created (see ADR-010, "Opt-in event stream helper").
/// </remarks>
public sealed record EventStreamOptions
{
    /// <summary>Gets the event patterns to subscribe to; each one is a regular expression evaluated on the device.</summary>
    /// <value>The patterns. Required; may be empty. Items must not be <see langword="null"/>, blank, or contain control characters or lone surrogates, as for <see cref="EventsApi.SubscribeAsync"/>.</value>
    public required IReadOnlyList<string> Subscriptions { get; init; }

    /// <summary>Gets the event patterns to exclude (device-side regular expressions).</summary>
    /// <value>The exclusions, or <see langword="null"/> (the default) for none. Items follow the same rules as <see cref="Subscriptions"/>.</value>
    public IReadOnlyList<string>? Exclusions { get; init; }

    /// <summary>Gets how long the device holds each listen call before answering with the queued events (possibly none).</summary>
    /// <value>At least 1 ms and strictly below <see cref="MaxKeepalive"/>; default 25 seconds. Keep it well below <c>HttpClient.Timeout</c> (100 s by default): the library cannot read that timeout, so this is documented, not enforced.</value>
    public TimeSpan PollTimeout { get; init; } = TimeSpan.FromSeconds(25);

    /// <summary>Gets how long the device keeps the subscription alive without a listen call.</summary>
    /// <value>At least 1 ms and strictly above <see cref="PollTimeout"/>; default 2 minutes. It is also the time after which the device drops a subscription the caller never released.</value>
    public TimeSpan MaxKeepalive { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets the pause applied after a listen call that returned no event, to avoid a hot loop when the device answers at once.</summary>
    /// <value>Zero (no pause) or more; default 1 second.</value>
    public TimeSpan EmptyPollDelay { get; init; } = TimeSpan.FromSeconds(1);
}
