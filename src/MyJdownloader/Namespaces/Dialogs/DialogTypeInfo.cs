namespace MyJdownloader.Dialogs;

/// <summary>Describes the properties of a dialog type: what the device shows (<see cref="In"/>) and what an answer may contain (<see cref="Out"/>).</summary>
/// <remarks>Every key and value is a raw string supplied by the device: treat it as untrusted and encode it before displaying it.</remarks>
public sealed record DialogTypeInfo
{
    /// <summary>Gets the properties the device sets on a dialog of this type (name to type description), or <see langword="null"/> when not returned.</summary>
    public IReadOnlyDictionary<string, string>? In { get; init; }

    /// <summary>Gets the properties an answer may carry (name to type description), or <see langword="null"/> when not returned.</summary>
    public IReadOnlyDictionary<string, string>? Out { get; init; }
}
