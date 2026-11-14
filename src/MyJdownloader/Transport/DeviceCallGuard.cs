namespace MyJdownloader.Transport;

using System.Text.RegularExpressions;

/// <summary>Argument checks shared by the device channels, whatever their transport.</summary>
internal static partial class DeviceCallGuard
{
    /// <summary>The maximum number of arguments of one call.</summary>
    internal const int MaxRequestArgsCount = 1_024;

    /// <summary>The maximum length of an action path, in characters.</summary>
    private const int MaxPathLength = 256;

    /// <summary>Checks that the path is a sequence of <c>/segment</c> parts made of letters, digits and underscores.</summary>
    /// <param name="path">The device API path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The path is not valid.</exception>
    internal static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length > MaxPathLength || !PathPattern().IsMatch(path))
        {
            throw new ArgumentException("The path is not valid.", nameof(path));
        }
    }

    /// <summary>Matches an action path: one or more segments of ASCII letters, digits and underscores, each preceded by a slash.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A(?:/[A-Za-z0-9_]+)+\z", RegexOptions.CultureInvariant)]
    private static partial Regex PathPattern();
}
