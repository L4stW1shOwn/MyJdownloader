namespace MyJdownloader;

using System.Diagnostics;
using MyJdownloader.Accounts;
using MyJdownloader.Captcha;
using MyJdownloader.CaptchaForward;
using MyJdownloader.Config;
using MyJdownloader.ContentV2;
using MyJdownloader.Device;
using MyJdownloader.DeviceExtensions;
using MyJdownloader.Dialogs;
using MyJdownloader.DownloadController;
using MyJdownloader.DownloadEvents;
using MyJdownloader.Downloads;
using MyJdownloader.Events;
using MyJdownloader.Extraction;
using MyJdownloader.Jd;
using MyJdownloader.LinkCrawler;
using MyJdownloader.LinkGrabber;
using MyJdownloader.Log;
using MyJdownloader.Plugins;
using MyJdownloader.Polling;
using MyJdownloader.Reconnect;
using MyJdownloader.SystemControl;
using MyJdownloader.Toolbar;
using MyJdownloader.Ui;
using MyJdownloader.Update;

/// <summary>Entry point of the typed facade for one JDownloader device of the account.</summary>
/// <remarks>
/// The instance holds no session and no secret: it only carries the device id and its <see cref="IDeviceChannel"/>,
/// which reads the current session of the client on every call. Typed namespace facades are exposed as properties, for example <see cref="LinkGrabber"/>, <see cref="Downloads"/>, <see cref="DownloadController"/>, <see cref="Jd"/>, <see cref="System"/>, <see cref="Config"/>, <see cref="Accounts"/>, <see cref="Captcha"/>, <see cref="Dialogs"/>, <see cref="Events"/>, <see cref="Extensions"/>, <see cref="Extraction"/>, <see cref="Plugins"/>, <see cref="Update"/>, <see cref="Device"/>, <see cref="LinkCrawler"/>, <see cref="Reconnect"/>, <see cref="Polling"/>, <see cref="Log"/>, <see cref="ContentV2"/>, <see cref="Ui"/>, <see cref="CaptchaForward"/>, <see cref="Toolbar"/> and <see cref="DownloadEvents"/>.
/// </remarks>
[DebuggerDisplay("Id = {Id}")]
public sealed class JDownloaderDevice
{
    internal JDownloaderDevice(string id, IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(channel);
        Id = id;
        Channel = channel;
    }

    /// <summary>Gets the device identifier, as supplied to <see cref="MyJDownloaderClient.GetDevice"/>.</summary>
    public string Id { get; }

    /// <summary>Gets the channel to the device, which can invoke any device action.</summary>
    public IDeviceChannel Channel { get; }

    /// <summary>Gets the typed facade over the <c>/linkgrabberv2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public LinkGrabberApi LinkGrabber => field ??= new LinkGrabberApi(Channel);

    /// <summary>Gets the typed facade over the <c>/downloadsV2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DownloadsApi Downloads => field ??= new DownloadsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/downloadcontroller</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DownloadControllerApi DownloadController => field ??= new DownloadControllerApi(Channel);

    /// <summary>Gets the typed facade over the <c>/jd</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public JdApi Jd => field ??= new JdApi(Channel);

    /// <summary>Gets the typed facade over the <c>/system</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public SystemApi System => field ??= new SystemApi(Channel);

    /// <summary>Gets the typed facade over the <c>/config</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ConfigApi Config => field ??= new ConfigApi(Channel);

    /// <summary>Gets the typed facade over the <c>/plugins</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public PluginsApi Plugins => field ??= new PluginsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/accountsV2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public AccountsApi Accounts => field ??= new AccountsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/captcha</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public CaptchaApi Captcha => field ??= new CaptchaApi(Channel);

    /// <summary>Gets the typed facade over the <c>/dialogs</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DialogsApi Dialogs => field ??= new DialogsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/events</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public EventsApi Events => field ??= new EventsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/extensions</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ExtensionsApi Extensions => field ??= new ExtensionsApi(Channel);

    /// <summary>Gets the typed facade over the <c>/extraction</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ExtractionApi Extraction => field ??= new ExtractionApi(Channel);

    /// <summary>Gets the typed facade over the <c>/update</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public UpdateApi Update => field ??= new UpdateApi(Channel);

    /// <summary>Gets the typed facade over the <c>/device</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DeviceApi Device => field ??= new DeviceApi(Channel);

    /// <summary>Gets the typed facade over the <c>/linkcrawler</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public LinkCrawlerApi LinkCrawler => field ??= new LinkCrawlerApi(Channel);

    /// <summary>Gets the typed facade over the <c>/reconnect</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ReconnectApi Reconnect => field ??= new ReconnectApi(Channel);

    /// <summary>Gets the typed facade over the <c>/polling</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public PollingApi Polling => field ??= new PollingApi(Channel);

    /// <summary>Gets the typed facade over the <c>/log</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public LogApi Log => field ??= new LogApi(Channel);

    /// <summary>Gets the typed facade over the <c>/contentV2</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ContentV2Api ContentV2 => field ??= new ContentV2Api(Channel);

    /// <summary>Gets the typed facade over the <c>/ui</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public UiApi Ui => field ??= new UiApi(Channel);

    /// <summary>Gets the typed facade over the <c>/captchaforward</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public CaptchaForwardApi CaptchaForward => field ??= new CaptchaForwardApi(Channel);

    /// <summary>Gets the typed facade over the <c>/toolbar</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public ToolbarApi Toolbar => field ??= new ToolbarApi(Channel);

    /// <summary>Gets the typed facade over the <c>/downloadevents</c> namespace of this device.</summary>
    /// <remarks>The facade is stateless and created on first access; concurrent first accesses may create more than one instance, which is harmless.</remarks>
    public DownloadEventsApi DownloadEvents => field ??= new DownloadEventsApi(Channel);
}
