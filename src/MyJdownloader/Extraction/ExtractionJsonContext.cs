namespace MyJdownloader.Extraction;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/extraction</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    AllowDuplicateProperties = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<ExtractionControllerStatus>),
        typeof(TolerantEnumConverter<ArchiveFileStatus>),
    ])]
[JsonSerializable(typeof(ArchiveStatus))]
[JsonSerializable(typeof(ArchiveSettings))]
[JsonSerializable(typeof(List<ArchiveStatus>))]
[JsonSerializable(typeof(List<ArchiveSettings>))]
[JsonSerializable(typeof(Dictionary<string, bool?>))]
internal sealed partial class ExtractionJsonContext : JsonSerializerContext
{
}
