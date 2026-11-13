namespace MyJdownloader.SystemControl;

/// <summary>Storage information of one root of a device, as returned by <c>/system/getStorageInfos</c>.</summary>
/// <remarks>All content returned by the device is untrusted.</remarks>
public sealed record StorageInfo
{
    /// <summary>Gets the path of the storage root.</summary>
    public string? Path { get; init; }

    /// <summary>Gets the total size, in bytes.</summary>
    public long Size { get; init; }

    /// <summary>Gets the free space, in bytes.</summary>
    public long Free { get; init; }

    /// <summary>Gets the device-side error reported for this root, or <see langword="null"/> when there is none.</summary>
    public string? Error { get; init; }
}
