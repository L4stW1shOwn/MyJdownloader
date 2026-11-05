namespace MyJdownloader.Json;

using System.Text.Json.Serialization;
using MyJdownloader.Sessions;
using MyJdownloader.Transport;

/// <summary>Source-generated, strict JSON metadata for the session document and server responses.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MyJdSessionDocument))]
[JsonSerializable(typeof(ConnectResponse))]
internal sealed partial class MyJdJsonContext : JsonSerializerContext
{
}
