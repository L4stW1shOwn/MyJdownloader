namespace MyJdownloader.Accounts;

using System.Text.Json.Serialization;

/// <summary>A hoster account stored on the device; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>The device never returns the account password, so this record holds no secret.</remarks>
public sealed record Account
{
    /// <summary>Gets the account identifier.</summary>
    [JsonPropertyName("uuid")]
    public long Uuid { get; init; }

    /// <summary>Gets the hoster the account belongs to.</summary>
    public string? Hostname { get; init; }

    /// <summary>Gets the user name.</summary>
    public string? Username { get; init; }

    /// <summary>Gets a value indicating whether the account is enabled.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets a value indicating whether the account is valid.</summary>
    public bool? Valid { get; init; }

    /// <summary>Gets the error type; this is the raw device value and its set of values is not documented.</summary>
    public string? ErrorType { get; init; }

    /// <summary>Gets the error message.</summary>
    public string? ErrorString { get; init; }

    /// <summary>Gets the remaining traffic in bytes, as reported by the device (raw value).</summary>
    public long? TrafficLeft { get; init; }

    /// <summary>Gets the maximum traffic in bytes, as reported by the device (raw value).</summary>
    public long? TrafficMax { get; init; }

    /// <summary>Gets the expiry date, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? ValidUntil { get; init; }
}
