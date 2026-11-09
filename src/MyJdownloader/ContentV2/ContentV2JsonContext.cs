namespace MyJdownloader.ContentV2;

using System.Text.Json.Serialization;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/contentV2</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IconDescriptor))]
internal sealed partial class ContentV2JsonContext : JsonSerializerContext
{
}
