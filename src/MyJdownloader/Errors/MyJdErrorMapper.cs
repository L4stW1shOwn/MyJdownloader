namespace MyJdownloader.Errors;

using System.Globalization;
using System.Net;

/// <summary>Maps an error reported by the MyJDownloader service to the matching <see cref="MyJdException"/>.</summary>
internal static class MyJdErrorMapper
{
    private const string UnknownCode = "UNKNOWN";
    private const int MaxCodeLength = 64;

    /// <summary>Builds the exception matching an error reported by the service.</summary>
    /// <param name="channelSource">The side the response came through, used when <paramref name="src"/> is missing or unrecognized.</param>
    /// <param name="src">The raw <c>src</c> field of the error payload.</param>
    /// <param name="type">The raw <c>type</c> field of the error payload.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="retryAfter">The retry delay provided by the service, only used for rate-limit errors.</param>
    /// <returns>The mapped exception.</returns>
    public static MyJdException Map(
        MyJdErrorSource channelSource,
        string? src,
        string? type,
        HttpStatusCode statusCode,
        TimeSpan? retryAfter)
    {
        var trimmedSrc = src?.Trim();
        var source = string.Equals(trimmedSrc, "SERVER", StringComparison.OrdinalIgnoreCase) ? MyJdErrorSource.Server
            : string.Equals(trimmedSrc, "DEVICE", StringComparison.OrdinalIgnoreCase) ? MyJdErrorSource.Device
            : channelSource;

        var code = type?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            code = UnknownCode;
        }

        var shownCode = IsSafeCode(code) ? code : UnknownCode;
        var sourceText = source == MyJdErrorSource.Device ? "device" : "server";
        var message = string.Create(CultureInfo.InvariantCulture, $"MyJD {sourceText} error {shownCode} (HTTP {(int)statusCode}).");

        return code switch
        {
            "AUTH_FAILED" or "EMAIL_INVALID" or "EMAIL_FORBIDDEN" or "TOKEN_INVALID" or "ERROR_EMAIL_NOT_CONFIRMED"
                => new MyJdAuthenticationException(message, source, code, statusCode),
            "OFFLINE" => new MyJdDeviceOfflineException(message, source, code, statusCode),
            "TOO_MANY_REQUESTS" or "OVERLOAD" or "MAINTENANCE"
                => new MyJdRateLimitException(message, source, code, statusCode, retryAfter),
            _ => new MyJdApiException(message, source, code, statusCode),
        };
    }

    private static bool IsSafeCode(string code)
    {
        if (code.Length is < 1 or > MaxCodeLength)
        {
            return false;
        }

        foreach (var c in code)
        {
            if (!(char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
