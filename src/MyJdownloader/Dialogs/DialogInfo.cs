namespace MyJdownloader.Dialogs;

/// <summary>A dialog currently open on the device; fields the device does not return are <see langword="null"/>.</summary>
/// <remarks>
/// <see cref="Type"/> and every key and value of <see cref="Properties"/> are raw strings supplied by the device (they may
/// contain file names, URLs or messages): treat them as untrusted and encode them before displaying them.
/// </remarks>
public sealed record DialogInfo
{
    /// <summary>Gets the dialog type reported by the device; its set of values is not documented.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the dialog properties (content, title, flags), or <see langword="null"/> when not returned or not requested.</summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
