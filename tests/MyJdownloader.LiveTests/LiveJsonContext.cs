namespace MyJdownloader.LiveTests;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Source-generated JSON metadata for raw channel calls made by live tests.</summary>
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
public sealed partial class LiveJsonContext : JsonSerializerContext;
