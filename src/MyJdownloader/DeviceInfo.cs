namespace MyJdownloader;

/// <summary>A device registered on the MyJD account, as returned by the MyJD server.</summary>
/// <param name="Id">The device identifier.</param>
/// <param name="Name">The device display name.</param>
/// <param name="Type">The device type reported by the server.</param>
public sealed record DeviceInfo(string Id, string Name, string Type);
