namespace MyJdownloader.Events;

using System.Text.Json.Serialization;

/// <summary>An event publisher and the identifiers of the events it can emit.</summary>
public sealed record EventPublisher
{
    /// <summary>Gets the publisher name.</summary>
    public string? Publisher { get; init; }

    /// <summary>Gets the identifiers of the events the publisher emits.</summary>
    [JsonPropertyName("eventids")]
    public IReadOnlyList<string>? EventIds { get; init; }
}
