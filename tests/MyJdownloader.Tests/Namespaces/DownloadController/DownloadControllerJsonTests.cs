namespace MyJdownloader.Tests.DownloadController;

using System.Text.Json;
using MyJdownloader.DownloadController;

[TestFixture]
public sealed class DownloadControllerJsonTests
{
    [TestCase("IDLE", DownloadState.Idle)]
    [TestCase("RUNNING", DownloadState.Running)]
    [TestCase("PAUSE", DownloadState.Pause)]
    [TestCase("STOPPING", DownloadState.Stopping)]
    [TestCase("STOPPED", DownloadState.Stopped)]
    public void DownloadState_KnownLabels_Read(string label, DownloadState expected)
    {
        var state = JsonSerializer.Deserialize("\"" + label + "\"", DownloadControllerJsonContext.Default.NullableDownloadState);

        Assert.That(state, Is.EqualTo(expected));
    }

    [Test]
    public void DownloadState_UnknownLabel_ReadsUnknown()
    {
        var state = JsonSerializer.Deserialize("\"SOMETHING_NEW\"", DownloadControllerJsonContext.Default.NullableDownloadState);

        Assert.That(state, Is.EqualTo(DownloadState.Unknown));
    }

    [Test]
    public void DownloadState_Write_UsesUpperSnake()
    {
        var json = JsonSerializer.Serialize(DownloadState.Running, DownloadControllerJsonContext.Default.DownloadState);

        Assert.That(json, Is.EqualTo("\"RUNNING\""));
    }

    [Test]
    public void DownloadState_WriteUnknown_Throws() =>
        Assert.That(
            () => JsonSerializer.Serialize(DownloadState.Unknown, DownloadControllerJsonContext.Default.DownloadState),
            Throws.TypeOf<ArgumentException>());
}
