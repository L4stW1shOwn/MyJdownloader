namespace MyJdownloader;

/// <summary>A device registered on the MyJD account, as returned by the MyJD server.</summary>
/// <remarks>
/// The values come from the MyJD server and are untrusted: do not log or display them unfiltered,
/// and escape <see cref="Id"/> when it is used in a URL.
/// </remarks>
public sealed record DeviceInfo
{
    /// <summary>Gets the device identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the device display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the device type reported by the server.</summary>
    public required string Type { get; init; }
}
