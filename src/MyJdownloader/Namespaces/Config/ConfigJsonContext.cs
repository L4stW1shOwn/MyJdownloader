namespace MyJdownloader.Config;

using System.Text.Json;
using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/config</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<ConfigValueType>),
        typeof(EnumOptionPairsConverter),
    ])]
[JsonSerializable(typeof(ConfigQuery))]
[JsonSerializable(typeof(List<ConfigEntry>))]
[JsonSerializable(typeof(List<ConfigEnumOption>))]
[JsonSerializable(typeof(ConfigValueType))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement?))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext
{
}
