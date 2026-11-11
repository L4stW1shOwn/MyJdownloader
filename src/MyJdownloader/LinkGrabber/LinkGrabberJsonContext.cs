namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/linkgrabberv2</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<Priority>),
        typeof(TolerantEnumConverter<AvailableLinkState>),
        typeof(TolerantEnumConverter<CleanupAction>),
        typeof(TolerantEnumConverter<CleanupMode>),
        typeof(TolerantEnumConverter<SelectionType>),
    ])]
[JsonSerializable(typeof(AddLinksQueryDto))]
[JsonSerializable(typeof(LinkQuery))]
[JsonSerializable(typeof(PackageQuery))]
[JsonSerializable(typeof(List<CrawledLink>))]
[JsonSerializable(typeof(List<CrawledPackage>))]
[JsonSerializable(typeof(List<LinkVariant>))]
[JsonSerializable(typeof(LinkCollectingJob))]
[JsonSerializable(typeof(Priority))]
[JsonSerializable(typeof(CleanupAction))]
[JsonSerializable(typeof(CleanupMode))]
[JsonSerializable(typeof(SelectionType))]
internal sealed partial class LinkGrabberJsonContext : JsonSerializerContext
{
}
