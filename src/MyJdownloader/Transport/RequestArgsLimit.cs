namespace MyJdownloader.Transport;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

/// <summary>The cap on the total length of the arguments of a device call, and a cheap lower bound to enforce it before materializing an argument.</summary>
internal static class RequestArgsLimit
{
    internal const int MaxLength = 1_048_576;

    /// <summary>The message of the exception thrown when the arguments of a call exceed <see cref="MaxLength"/> characters in total.</summary>
    internal static readonly string TotalMessage = string.Create(CultureInfo.InvariantCulture, $"The arguments must not exceed {MaxLength} characters in total.");

    /// <summary>The message of the exception thrown when a single value exceeds <see cref="MaxLength"/> characters.</summary>
    internal static readonly string ValueMessage = string.Create(CultureInfo.InvariantCulture, $"The value must not exceed {MaxLength} characters.");

    /// <summary>Returns a lower bound of the number of UTF-16 characters of an element once unescaped, from its raw UTF-8 size and without allocating.</summary>
    /// <param name="element">The element.</param>
    /// <returns>A value never greater than the real length (a <c>\uXXXX</c> escape is 6 UTF-8 bytes for one character; 2 covers the quotes).</returns>
    /// <exception cref="ObjectDisposedException">The document of the element was disposed.</exception>
    internal static int MinLength(JsonElement element)
        => Math.Max(0, JsonMarshal.GetRawUtf8Value(element).Length - 2) / 6;

    /// <summary>Returns the exact number of UTF-16 characters of the raw text of an element (<see cref="JsonElement.GetRawText"/>), without allocating.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The length of the raw text.</returns>
    /// <exception cref="ObjectDisposedException">The document of the element was disposed.</exception>
    internal static int RawTextLength(JsonElement element)
        => Encoding.UTF8.GetCharCount(JsonMarshal.GetRawUtf8Value(element));
}
