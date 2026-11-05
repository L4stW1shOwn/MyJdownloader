namespace MyJdownloader.Transport;

using System.Text.Json.Serialization;

/// <summary>Decrypted body that carries no data besides the request id; unknown fields are ignored.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class RidResponse
{
}
