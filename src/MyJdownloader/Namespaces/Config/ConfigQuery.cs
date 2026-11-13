namespace MyJdownloader.Config;

/// <summary>Selects the entries and fields returned by <c>/config/query</c>; current values are not returned by default.</summary>
public sealed record ConfigQuery
{
    /// <summary>Gets the name of the configuration interface to restrict the query to, or <see langword="null"/> for all.</summary>
    public string? ConfigInterface { get; init; }

    /// <summary>Gets a regular expression, evaluated on the device, that entries must match, or <see langword="null"/> for all.</summary>
    public string? Pattern { get; init; }

    /// <summary>Gets a value indicating whether the documentation text is returned.</summary>
    public bool Description { get; init; } = true;

    /// <summary>Gets a value indicating whether the default values are returned.</summary>
    public bool DefaultValues { get; init; } = true;

    /// <summary>Gets a value indicating whether the enumeration options are returned.</summary>
    public bool EnumInfo { get; init; } = true;

    /// <summary>Gets a value indicating whether the entries of extensions are included.</summary>
    public bool IncludeExtensions { get; init; } = true;

    /// <summary>Gets a value indicating whether the current values are returned; <see langword="false"/> by default because they may hold secrets.</summary>
    public bool Values { get; init; }
}
