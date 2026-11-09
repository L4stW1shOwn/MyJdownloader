namespace MyJdownloader.Polling;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/polling</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(List<PollingResult>))]
[JsonSerializable(typeof(JsonElement?))]
internal sealed partial class PollingJsonContext : JsonSerializerContext
{
}
