namespace MyJdownloader.Log;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/log</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(UnixMillisecondsConverter)])]
[JsonSerializable(typeof(List<LogFolder>))]
[JsonSerializable(typeof(LogFolder[]))]
internal sealed partial class LogJsonContext : JsonSerializerContext
{
}
