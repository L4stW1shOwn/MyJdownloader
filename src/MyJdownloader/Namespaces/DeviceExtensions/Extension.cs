namespace MyJdownloader.DeviceExtensions;

/// <summary>A JDownloader extension; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>Text fields are supplied by the device and are untrusted: encode them before displaying them.</remarks>
public sealed record Extension
{
    /// <summary>Gets the extension identifier (the class name used by the other actions).</summary>
    public string? Id { get; init; }

    /// <summary>Gets the display name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the name of the configuration interface of the extension.</summary>
    public string? ConfigInterface { get; init; }

    /// <summary>Gets the icon key.</summary>
    public string? IconKey { get; init; }

    /// <summary>Gets a value indicating whether the extension is enabled, or <see langword="null"/> when not requested.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Gets a value indicating whether the extension is installed, or <see langword="null"/> when not requested.</summary>
    public bool? Installed { get; init; }
}
