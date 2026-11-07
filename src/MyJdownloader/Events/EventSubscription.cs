namespace MyJdownloader.Events;

using System.Text.Json.Serialization;

/// <summary>An event subscription as reported by the device.</summary>
public sealed record EventSubscription
{
    /// <summary>Gets the opaque identifier of the subscription, to pass to the other methods.</summary>
    [JsonPropertyName("subscriptionid")]
    public long SubscriptionId { get; init; }

    /// <summary>Gets a value indicating whether the subscription is active.</summary>
    public bool Subscribed { get; init; }

    /// <summary>Gets the subscribed event patterns; each one is a regular expression evaluated on the device.</summary>
    public IReadOnlyList<string>? Subscriptions { get; init; }

    /// <summary>Gets the excluded event patterns; each one is a regular expression evaluated on the device.</summary>
    public IReadOnlyList<string>? Exclusions { get; init; }

    /// <summary>
    /// Gets the maximum poll timeout, or <see langword="null"/> when unknown. Milliseconds on the wire
    /// (assumption to confirm by live tests).
    /// </summary>
    [JsonPropertyName("maxPolltimeout")]
    public TimeSpan? MaxPollTimeout { get; init; }

    /// <summary>
    /// Gets the maximum keep-alive duration, or <see langword="null"/> when unknown. Milliseconds on the wire
    /// (assumption to confirm by live tests).
    /// </summary>
    public TimeSpan? MaxKeepalive { get; init; }
}
