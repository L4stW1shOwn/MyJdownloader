namespace MyJdownloader.DownloadController;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/downloadcontroller</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<DownloadState>),
    ])]
[JsonSerializable(typeof(DownloadState))]
[JsonSerializable(typeof(DownloadState?))]
internal sealed partial class DownloadControllerJsonContext : JsonSerializerContext
{
}
