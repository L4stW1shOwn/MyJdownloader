namespace MyJdownloader.DeviceExtensions;

using System.Text.Json.Serialization;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/extensions</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    AllowDuplicateProperties = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ExtensionQuery))]
[JsonSerializable(typeof(List<Extension>))]
internal sealed partial class ExtensionsJsonContext : JsonSerializerContext
{
}
