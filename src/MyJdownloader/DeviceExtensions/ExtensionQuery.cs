namespace MyJdownloader.DeviceExtensions;

/// <summary>Selects the extensions and fields returned by <c>/extensions/list</c>; all fields are returned by default.</summary>
public sealed record ExtensionQuery
{
    /// <summary>Gets a value indicating whether the configuration interface name is returned.</summary>
    public bool ConfigInterface { get; init; } = true;

    /// <summary>Gets a value indicating whether the description is returned.</summary>
    public bool Description { get; init; } = true;

    /// <summary>Gets a value indicating whether the enabled flag is returned.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets a value indicating whether the icon key is returned.</summary>
    public bool IconKey { get; init; } = true;

    /// <summary>Gets a value indicating whether the installed flag is returned.</summary>
    public bool Installed { get; init; } = true;

    /// <summary>Gets a value indicating whether the name is returned.</summary>
    public bool Name { get; init; } = true;

    /// <summary>Gets a regular expression, evaluated on the device, that extensions must match, or <see langword="null"/> for all.</summary>
    public string? Pattern { get; init; }
}
