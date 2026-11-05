namespace MyJdownloader.Json;

using System.Text.Json.Serialization;
using MyJdownloader.Sessions;

/// <summary>Source-generated, strict JSON metadata for the session document.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MyJdSessionDocument))]
internal sealed partial class MyJdJsonContext : JsonSerializerContext
{
}
