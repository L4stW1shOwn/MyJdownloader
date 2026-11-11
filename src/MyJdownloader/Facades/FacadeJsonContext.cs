namespace MyJdownloader.Facades;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated JSON metadata for primitive arguments and results shared by the device namespace facades.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(TolerantEnumConverter<UrlDisplayType>)])]
[JsonSerializable(typeof(long[]))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(long?))]
[JsonSerializable(typeof(UrlDisplayType[]))]
[JsonSerializable(typeof(Dictionary<string, List<long>>))]
internal sealed partial class FacadeJsonContext : JsonSerializerContext
{
}
