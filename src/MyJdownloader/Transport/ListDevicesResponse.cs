namespace MyJdownloader.Transport;

using System.Text.Json.Serialization;

/// <summary>Decrypted body of a successful <c>/my/listdevices</c> call; unknown fields are ignored.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class ListDevicesResponse
{
    [JsonPropertyName("list")]
    public DeviceResponse[]? List { get; set; }
}

/// <summary>A device entry of the <c>/my/listdevices</c> response; unknown fields are ignored.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class DeviceResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
