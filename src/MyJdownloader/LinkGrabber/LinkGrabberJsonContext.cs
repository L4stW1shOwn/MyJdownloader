namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/linkgrabberv2</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AddLinksQueryDto))]
[JsonSerializable(typeof(LinkQuery))]
[JsonSerializable(typeof(PackageQuery))]
[JsonSerializable(typeof(List<CrawledLink>))]
[JsonSerializable(typeof(List<CrawledPackage>))]
[JsonSerializable(typeof(LinkCollectingJob))]
[JsonSerializable(typeof(long[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Priority))]
internal sealed partial class LinkGrabberJsonContext : JsonSerializerContext
{
}
