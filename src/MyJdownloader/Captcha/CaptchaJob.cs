namespace MyJdownloader.Captcha;

using System.Text.Json.Serialization;

/// <summary>A captcha challenge pending on the device; fields the device does not return are <see langword="null"/>.</summary>
public sealed record CaptchaJob
{
    /// <summary>Gets the captcha job identifier.</summary>
    public long Id { get; init; }

    /// <summary>Gets the identifier of the link the captcha belongs to.</summary>
    [JsonPropertyName("link")]
    public long LinkId { get; init; }

    /// <summary>Gets the hoster that requires the captcha.</summary>
    public string? Hoster { get; init; }

    /// <summary>Gets the challenge type; this is the raw device value and its set of values is not documented.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the captcha category.</summary>
    public string? CaptchaCategory { get; init; }

    /// <summary>Gets the explanation shown to the user.</summary>
    public string? Explain { get; init; }

    /// <summary>Gets the creation date, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>Gets the timeout as reported by the device (raw value, unit not documented).</summary>
    public int? Timeout { get; init; }
}
