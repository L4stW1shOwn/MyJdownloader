namespace MyJdownloader.Tests.Local;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using MyJdownloader.Errors;
using MyJdownloader.Local;
using MyJdownloader.Transport;

[TestFixture]
[SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a handler disposed by the test")]
public sealed class LocalDeviceChannelBinaryTests
{
    private const string IconPath = "/contentV2/getIcon";
    private const int SmallCap = 1_024;
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _png = [.. _pngSignature, 0x01, 0x02];

    private static LocalDeviceChannel Channel(HttpMessageHandler handler, int maxResponseBodyLength = 1_048_576)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false);
        return new LocalDeviceChannel(httpClient, LocalEndpoint.Create(new LocalJDownloaderOptions { MaxResponseBodyLength = maxResponseBodyLength }));
    }

    private static JsonElement Json(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    private static IEnumerable<TestCaseData> NotPngAnswers()
    {
        yield return new TestCaseData("application/json", /*lang=json,strict*/ "{\"data\":true}"u8.ToArray()).SetName("JsonBody");
        yield return new TestCaseData("image/png", "not a png"u8.ToArray()).SetName("NoSignature");
        yield return new TestCaseData(null, _png).SetName("MissingContentType");
        yield return new TestCaseData("image/png", Array.Empty<byte>()).SetName("EmptyBody");
    }

    [Test]
    public async Task CallBinaryAsync_ReturnsPngBytes()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Png(_png));
        var channel = Channel(server);

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(_png));
    }

    [Test]
    public async Task CallBinaryAsync_PostsRawJsonParams()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Png(_png));
        var channel = Channel(server);

        await channel.CallBinaryAsync(IconPath, [Json("\"download\""), Json("32")]).WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Body, Is.EqualTo(/*lang=json,strict*/ """{"params":["download",32]}"""));
    }

    [TestCaseSource(nameof(NotPngAnswers))]
    public async Task CallBinaryAsync_NotPng_ThrowsProtocol(string? mediaType, byte[] body)
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Png(body, mediaType));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task CallBinaryAsync_OverLimit_ThrowsProtocol()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Png([.. _pngSignature, .. new byte[SmallCap]]));
        var channel = Channel(server, SmallCap);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(RelayTransport.TooLargeMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task CallBinaryAsync_DeviceError_MapsToApiException()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(/*lang=json,strict*/ """{"src":"DEVICE","type":"API_COMMAND_NOT_FOUND"}""", HttpStatusCode.NotFound));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(async () => await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorType, Is.EqualTo("API_COMMAND_NOT_FOUND"));
    }
}
