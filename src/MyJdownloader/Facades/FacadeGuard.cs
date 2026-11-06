namespace MyJdownloader.Facades;

using System.Globalization;
using System.Text.Json;

/// <summary>Argument validation and serialization helpers shared by the device namespace facades.</summary>
internal static class FacadeGuard
{
    internal static bool IsKnown<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => !value.Equals(default(TEnum)) && Enum.IsDefined(value);

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

    internal static void ValidateOptionalText(string? text, string paramName)
    {
        if (text is not null)
        {
            ValidateText(text, paramName);
        }
    }

    internal static void ValidateText(string text, string paramName)
    {
        ArgumentNullException.ThrowIfNull(text, paramName);
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
