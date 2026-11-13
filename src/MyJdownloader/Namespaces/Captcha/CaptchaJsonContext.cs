namespace MyJdownloader.Captcha;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/captcha</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<CaptchaSkipRequest>),
        typeof(UnixMillisecondsConverter),
    ])]
[JsonSerializable(typeof(List<CaptchaJob>))]
[JsonSerializable(typeof(CaptchaJob))]
[JsonSerializable(typeof(CaptchaSkipRequest))]
internal sealed partial class CaptchaJsonContext : JsonSerializerContext
{
}
