namespace MyJdownloader.Facades;

using System.Globalization;
using System.Text.Json;

/// <summary>Argument validation and serialization helpers shared by the device namespace facades.</summary>
internal static class FacadeGuard
{
    /// <summary>Maximum length of identifiers, keys, names, formats and host names, in UTF-16 code units.</summary>
    internal const int MaxIdentifierLength = 1_024;

    /// <summary>Maximum length of file system paths, in UTF-16 code units.</summary>
    internal const int MaxPathLength = 32_767;

    /// <summary>Maximum length of URLs, tokens, passwords, patterns and free text, in UTF-16 code units.</summary>
    internal const int MaxLongTextLength = 65_536;

    internal static bool IsKnown<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => !value.Equals(default(TEnum)) && Enum.IsDefined(value);

    internal static void ThrowIfNegative(long value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "The identifier must not be negative.");
        }
    }

    internal static void RequireNotEmpty(IReadOnlyList<long> ids, string paramName)
    {
        if (ids.Count == 0)
        {
            throw new ArgumentException("The list must contain at least one identifier.", paramName);
        }
    }

    internal static JsonElement UrlDisplayTypeArg(IReadOnlyList<UrlDisplayType> urlDisplayTypes, string paramName)
    {
        ArgumentNullException.ThrowIfNull(urlDisplayTypes, paramName);
        if (urlDisplayTypes.Count == 0)
        {
            throw new ArgumentException("The list must contain at least one URL display type.", paramName);
        }

        var seen = new HashSet<UrlDisplayType>();
        foreach (var type in urlDisplayTypes)
        {
            if (!IsKnown(type) || !seen.Add(type))
            {
                throw new ArgumentException("The URL display types must be valid and distinct.", paramName);
            }
        }

        return JsonSerializer.SerializeToElement([.. urlDisplayTypes], FacadeJsonContext.Default.UrlDisplayTypeArray);
    }

    internal static JsonElement[] IdArgs(IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds)
    {
        ArgumentNullException.ThrowIfNull(linkIds);
        ArgumentNullException.ThrowIfNull(packageIds);
        return
        [
            Ids(linkIds, nameof(linkIds)),
            Ids(packageIds, nameof(packageIds)),
        ];
    }

    internal static JsonElement Ids(IReadOnlyList<long> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids, paramName);
        return JsonSerializer.SerializeToElement([.. ids], FacadeJsonContext.Default.Int64Array);
    }

    internal static void ValidateOptionalText(string? text, string paramName, int maxLength)
    {
        if (text is not null)
        {
            ValidateText(text, paramName, maxLength);
        }
    }

    internal static void ValidateText(string text, string paramName, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(text, paramName);
        if (text.Length > maxLength)
        {
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"A text value must not exceed {maxLength} characters."), paramName);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A text value must not be blank.", paramName);
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var category = char.GetUnicodeCategory(c);
            if (char.IsControl(c) || category is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                throw new ArgumentException("A text value must not contain control characters.", paramName);
            }

            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                throw new ArgumentException("A text value must not contain invalid surrogate characters.", paramName);
            }
        }
    }

    internal static void ValidateUrlSafeToken(string text, string paramName, int maxLength)
    {
        ValidateText(text, paramName, maxLength);
        foreach (var c in text)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            {
                throw new ArgumentException("A token must contain only ASCII letters, digits, '-' and '_'.", paramName);
            }
        }
    }

    internal static void ValidateHostName(string text, string paramName)
    {
        ValidateText(text, paramName, MaxIdentifierLength);
        if (Uri.CheckHostName(text) is not (UriHostNameType.Dns or UriHostNameType.IPv4))
        {
            throw new ArgumentException("A host name is expected (internationalized names must be sent as punycode).", paramName);
        }
    }

    internal static void ValidateRange(int maxResults, int startAt, string paramName)
    {
        if (maxResults < -1)
        {
            throw new ArgumentException("MaxResults must be -1 or greater.", paramName);
        }

        if (startAt < 0)
        {
            throw new ArgumentException("StartAt must not be negative.", paramName);
        }
    }
}
