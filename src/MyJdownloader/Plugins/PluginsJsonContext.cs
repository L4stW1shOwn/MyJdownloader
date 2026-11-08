namespace MyJdownloader.Plugins;

using System.Text.Json;
using System.Text.Json.Serialization;
using MyJdownloader.Config;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/plugins</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<ConfigValueType>),
        typeof(EnumOptionPairsConverter),
    ])]
[JsonSerializable(typeof(PluginsQuery))]
[JsonSerializable(typeof(ConfigQuery))]
[JsonSerializable(typeof(List<PluginInfo>))]
[JsonSerializable(typeof(List<PluginConfigEntry>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
[JsonSerializable(typeof(JsonElement?))]
internal sealed partial class PluginsJsonContext : JsonSerializerContext
{
}
