namespace MyJdownloader.Config;

/// <summary>An option of an enumeration-typed configuration entry.</summary>
public sealed record ConfigEnumOption
{
    /// <summary>Gets the option name, as sent to the device.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the human-readable label, or <see langword="null"/> when none is provided.</summary>
    public string? Label { get; init; }
}
