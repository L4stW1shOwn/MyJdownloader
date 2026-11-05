namespace MyJdownloader.Transport;

using System.Text.Json.Serialization;

/// <summary>Body of a MyJD error response; unknown fields are ignored.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class ErrorResponse
{
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
