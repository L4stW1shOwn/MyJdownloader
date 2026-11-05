namespace MyJdownloader.Transport;

using System.Text.Json.Serialization;

/// <summary>Decrypted body of a successful <c>/my/connect</c> call; unknown fields are ignored.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class ConnectResponse
{
    [JsonPropertyName("sessiontoken")]
    public string? SessionToken { get; set; }

    [JsonPropertyName("regaintoken")]
    public string? RegainToken { get; set; }
}
