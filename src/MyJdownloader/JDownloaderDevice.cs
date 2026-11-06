namespace MyJdownloader;

using System.Diagnostics;
using MyJdownloader.Downloads;
using MyJdownloader.LinkGrabber;

/// <summary>Entry point of the typed facade for one JDownloader device of the account.</summary>
/// <remarks>
/// The instance holds no session and no secret: it only carries the device id and its <see cref="IDeviceChannel"/>,
/// which reads the current session of the client on every call. Typed namespace facades are exposed as properties, for example <see cref="LinkGrabber"/> and <see cref="Downloads"/>.
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

    /// <summary>Gets the typed facade over the <c>/linkgrabberv2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public LinkGrabberApi LinkGrabber => field ??= new LinkGrabberApi(this.Channel);

    /// <summary>Gets the typed facade over the <c>/downloadsV2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DownloadsApi Downloads => field ??= new DownloadsApi(this.Channel);
}
