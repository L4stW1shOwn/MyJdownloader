namespace MyJdownloader.Events;

using System.Text.Json.Serialization;

/// <summary>The status of an event subscription.</summary>
public sealed record EventSubscriptionStatus
{
    /// <summary>Gets the opaque identifier of the subscription.</summary>
    [JsonPropertyName("subscriptionid")]
    public long SubscriptionId { get; init; }

    /// <summary>Gets a value indicating whether the subscription is active.</summary>
    public bool Subscribed { get; init; }

    /// <summary>Gets the number of events waiting to be listened to.</summary>
    public int QueueSize { get; init; }
}
