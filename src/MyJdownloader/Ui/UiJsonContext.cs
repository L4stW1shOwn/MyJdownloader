namespace MyJdownloader.Ui;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/ui</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<MenuContext>),
        typeof(TolerantEnumConverter<MenuItemType>),
    ])]
[JsonSerializable(typeof(MenuItem))]
[JsonSerializable(typeof(MenuContext))]
[JsonSerializable(typeof(System.Text.Json.JsonElement?))]
internal sealed partial class UiJsonContext : JsonSerializerContext
{
}
