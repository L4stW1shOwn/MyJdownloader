namespace MyJdownloader.Events;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/events</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(MillisecondsDurationConverter)])]
[JsonSerializable(typeof(EventSubscription))]
[JsonSerializable(typeof(EventSubscriptionStatus))]
[JsonSerializable(typeof(List<EventPublisher>))]
[JsonSerializable(typeof(List<DeviceEvent>))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class EventsJsonContext : JsonSerializerContext
{
}
