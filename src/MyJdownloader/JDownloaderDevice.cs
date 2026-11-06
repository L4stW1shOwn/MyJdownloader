namespace MyJdownloader;

using System.Diagnostics;

/// <summary>Entry point of the typed facade for one JDownloader device of the account.</summary>
/// <remarks>
/// The instance holds no session and no secret: it only carries the device id and its <see cref="IDeviceChannel"/>,
/// which reads the current session of the client on every call. Typed namespace properties are added in later versions.
/// </remarks>
[DebuggerDisplay("Id = {Id}")]
public sealed class JDownloaderDevice
{
    internal JDownloaderDevice(string id, IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(channel);
        this.Id = id;
        this.Channel = channel;
    }

    /// <summary>Gets the device identifier, as supplied to <see cref="MyJDownloaderClient.GetDevice"/>.</summary>
    public string Id { get; }

    /// <summary>Gets the channel to the device, which can invoke any device action.</summary>
    public IDeviceChannel Channel { get; }
}
