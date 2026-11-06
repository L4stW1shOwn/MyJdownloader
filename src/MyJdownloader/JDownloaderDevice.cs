namespace MyJdownloader;

using System.Diagnostics;
using MyJdownloader.Config;
using MyJdownloader.DownloadController;
using MyJdownloader.Downloads;
using MyJdownloader.Jd;
using MyJdownloader.LinkGrabber;
using MyJdownloader.SystemControl;

/// <summary>Entry point of the typed facade for one JDownloader device of the account.</summary>
/// <remarks>
/// The instance holds no session and no secret: it only carries the device id and its <see cref="IDeviceChannel"/>,
/// which reads the current session of the client on every call. Typed namespace facades are exposed as properties, for example <see cref="LinkGrabber"/>, <see cref="Downloads"/>, <see cref="DownloadController"/>, <see cref="Jd"/>, <see cref="System"/> and <see cref="Config"/>.
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

    /// <summary>Gets the typed facade over the <c>/downloadcontroller</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DownloadControllerApi DownloadController => field ??= new DownloadControllerApi(this.Channel);

    /// <summary>Gets the typed facade over the <c>/jd</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public JdApi Jd => field ??= new JdApi(this.Channel);

    /// <summary>Gets the typed facade over the <c>/system</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public SystemApi System => field ??= new SystemApi(this.Channel);

    /// <summary>Gets the typed facade over the <c>/config</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ConfigApi Config => field ??= new ConfigApi(this.Channel);
}
