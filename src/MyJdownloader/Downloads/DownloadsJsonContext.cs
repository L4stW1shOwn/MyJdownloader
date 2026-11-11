namespace MyJdownloader.Downloads;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/downloadsV2</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<Priority>),
        typeof(TolerantEnumConverter<CleanupAction>),
        typeof(TolerantEnumConverter<CleanupMode>),
        typeof(TolerantEnumConverter<SelectionType>),
        typeof(TolerantEnumConverter<UrlDisplayType>),
        typeof(TolerantEnumConverter<SkipReason>),
        typeof(UnixMillisecondsConverter),
        typeof(SecondsDurationConverter),
    ])]
[JsonSerializable(typeof(DownloadLinkQuery))]
[JsonSerializable(typeof(DownloadPackageQuery))]
[JsonSerializable(typeof(List<DownloadLink>))]
[JsonSerializable(typeof(List<DownloadPackage>))]
[JsonSerializable(typeof(Priority))]
[JsonSerializable(typeof(CleanupAction))]
[JsonSerializable(typeof(CleanupMode))]
[JsonSerializable(typeof(SelectionType))]
[JsonSerializable(typeof(UrlDisplayType))]
[JsonSerializable(typeof(UrlDisplayType[]))]
[JsonSerializable(typeof(SkipReason))]
[JsonSerializable(typeof(DownloadLink))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool?))]
internal sealed partial class DownloadsJsonContext : JsonSerializerContext
{
}
