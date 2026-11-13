namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
using NSubstitute;

/// <summary>
/// Pins the public facade constructors. This project has no InternalsVisibleTo access,
/// so it sees the library exactly as a consumer does.
/// </summary>
[TestFixture]
public sealed class PublicFacadeConstructorTests
{
    private static IDeviceChannel NewChannel() => Substitute.For<IDeviceChannel>();

    [Test]
    public void AccountsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new AccountsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void CaptchaApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new CaptchaApi(NewChannel()), Is.Not.Null);

    [Test]
    public void CaptchaForwardApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new CaptchaForwardApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ConfigApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ConfigApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ContentV2ApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ContentV2Api(NewChannel()), Is.Not.Null);

    [Test]
    public void DeviceApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new DeviceApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ExtensionsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ExtensionsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void DialogsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new DialogsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void DownloadControllerApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new DownloadControllerApi(NewChannel()), Is.Not.Null);

    [Test]
    public void DownloadEventsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new DownloadEventsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void DownloadsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new DownloadsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void EventsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new EventsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ExtractionApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ExtractionApi(NewChannel()), Is.Not.Null);

    [Test]
    public void JdApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new JdApi(NewChannel()), Is.Not.Null);

    [Test]
    public void LinkCrawlerApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new LinkCrawlerApi(NewChannel()), Is.Not.Null);

    [Test]
    public void LinkGrabberApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new LinkGrabberApi(NewChannel()), Is.Not.Null);

    [Test]
    public void LogApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new LogApi(NewChannel()), Is.Not.Null);

    [Test]
    public void PluginsApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new PluginsApi(NewChannel()), Is.Not.Null);

    [Test]
    public void PollingApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new PollingApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ReconnectApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ReconnectApi(NewChannel()), Is.Not.Null);

    [Test]
    public void SystemApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new SystemApi(NewChannel()), Is.Not.Null);

    [Test]
    public void ToolbarApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new ToolbarApi(NewChannel()), Is.Not.Null);

    [Test]
    public void UiApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new UiApi(NewChannel()), Is.Not.Null);

    [Test]
    public void UpdateApiConstructor_SubstituteChannel_CreatesInstance()
        => Assert.That(new UpdateApi(NewChannel()), Is.Not.Null);

    [Test]
    public void LinkGrabberApiConstructor_NullChannel_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _ = new LinkGrabberApi(null!));

    [Test]
    public async Task IsCrawlingAsync_FacadeOverSubstituteChannel_ForwardsCallAndReturnsAnswer()
    {
        var channel = NewChannel();
        channel.CallAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        var api = new LinkCrawlerApi(channel);

        var result = await api.IsCrawlingAsync();

        Assert.That(result, Is.True);
        await channel.Received(1).CallAsync(
            "/linkcrawler/isCrawling",
            Arg.Any<JsonTypeInfo<bool?>>(),
            Arg.Is<IReadOnlyList<JsonElement>>(args => args.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetIconAsync_ChannelWithoutBinarySupport_ThrowsNotSupportedException()
    {
        var api = new ContentV2Api(NewChannel());

        Assert.Throws<NotSupportedException>(() => api.GetIconAsync("kc.abc", 32));
    }

    [Test]
    public async Task GetIconDescriptionAsync_FacadeOverSubstituteChannel_ForwardsCallAndReturnsAnswer()
    {
        var channel = NewChannel();
        channel.CallAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<IconDescriptor>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IconDescriptor?>(null));
        var api = new ContentV2Api(channel);

        var result = await api.GetIconDescriptionAsync("kc.abc");

        Assert.That(result, Is.Null);
        await channel.Received(1).CallAsync(
            "/contentV2/getIconDescription",
            Arg.Any<JsonTypeInfo<IconDescriptor>>(),
            Arg.Is<IReadOnlyList<JsonElement>>(args => args.Count == 1),
            Arg.Any<CancellationToken>());
    }
}
