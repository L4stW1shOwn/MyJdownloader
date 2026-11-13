namespace MyJdownloader.SystemControl;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/system</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(UnixMillisecondsConverter)])]
[JsonSerializable(typeof(SystemInfo))]
[JsonSerializable(typeof(List<StorageInfo>))]
internal sealed partial class SystemJsonContext : JsonSerializerContext
{
}
