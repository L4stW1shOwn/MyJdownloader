namespace MyJdownloader.Dialogs;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/dialogs</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DialogInfo))]
[JsonSerializable(typeof(DialogTypeInfo))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal sealed partial class DialogsJsonContext : JsonSerializerContext
{
}
