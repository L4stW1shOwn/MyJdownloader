namespace MyJdownloader.Tests.Local;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using MyJdownloader.Errors;
using MyJdownloader.Local;
using MyJdownloader.Transport;

[TestFixture]
[SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a handler disposed by the test")]
public sealed class LocalDeviceChannelErrorTests
{
    private const string Path = "/jd/version";
    private const int SmallCap = 1_024;
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static LocalDeviceChannel Channel(HttpMessageHandler handler, int maxResponseBodyLength = 1_048_576)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false);
        return new LocalDeviceChannel(httpClient, LocalEndpoint.Create(new LocalJDownloaderOptions { MaxResponseBodyLength = maxResponseBodyLength }));
    }

    private static string StatusOnly(int code) => $"The JDownloader local API answered HTTP {code} without a readable error.";

    private static string DeviceUnreadable() => RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);

    private static IEnumerable<TestCaseData> NonJsonErrors()
    {
        yield return new TestCaseData(501, string.Empty, null, null).SetName("EmptyBody");
        yield return new TestCaseData(500, "java.lang.RuntimeException: boom", "text/plain", null).SetName("PlainText");
        yield return new TestCaseData(502, "<html><body>Bad gateway</body></html>", "text/html", null).SetName("Html");
        yield return new TestCaseData(400, /*lang=json,strict*/ """{"src":"DEVICE","data":"detail"}""", "application/json", null).SetName("NoType");
        yield return new TestCaseData(302, string.Empty, null, "http://example.org/login").SetName("Redirect");
    }

    private static IEnumerable<string> NotEnvelopes()
    {
        yield return "<html><body>JDownloader</body></html>";
        yield return string.Empty;
        yield return "[1]";
        yield return """{"data":""";
        yield return new StringBuilder().Insert(0, "[", 70).Append(new string(']', 70)).ToString();
    }

    [TestCaseSource(nameof(NonJsonErrors))]
    public async Task CallAsync_NonJsonError_ThrowsProtocolWithStatusOnly(int status, string body, string? mediaType, string? location)
    {
        using var server = new FakeLocalApi(_ =>
        {
            var response = FakeLocalApi.Raw(body, mediaType, (HttpStatusCode)status);
            if (location is not null)
            {
                response.Headers.Location = new Uri(location);
            }

            return response;
        });
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Is.EqualTo(StatusOnly(status)));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        });
    }

    [Test]
    public async Task CallAsync_DeviceError_MapsToApiException()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(/*lang=json,strict*/ """{"src":"DEVICE","data":null,"type":"API_COMMAND_NOT_FOUND"}""", HttpStatusCode.NotFound));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorType, Is.EqualTo("API_COMMAND_NOT_FOUND"));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(thrown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task CallAsync_BadParameters_DoesNotLeakTheEchoedValue()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(/*lang=json,strict*/ """{"src":"DEVICE","data":"secret-arg","type":"BAD_PARAMETERS"}""", HttpStatusCode.BadRequest));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Does.Not.Contain("secret-arg"));
    }

    [Test]
    public async Task CallAsync_ErrorClaimingServer_IsStillDevice()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(/*lang=json,strict*/ """{"src":"SERVER","type":"API_COMMAND_NOT_FOUND"}""", HttpStatusCode.NotFound));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [TestCaseSource(nameof(NotEnvelopes))]
    public async Task CallAsync_Ok_NotAnEnvelope_ThrowsProtocol(string body)
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Json(body));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(DeviceUnreadable()));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CallAsync_Ok_BodyOverLimit_ThrowsProtocol(bool withContentLength)
    {
        using var server = new FakeLocalApi(_ => withContentLength
            ? FakeLocalApi.Png(new byte[SmallCap + 1], "application/json")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new UnknownLengthContent(new byte[SmallCap + 1]) });
        var channel = Channel(server, SmallCap);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(RelayTransport.TooLargeMessageFor(MyJdErrorSource.Device)));
    }

    [Test]
    public async Task CallAsync_ErrorBodyOverLimit_ThrowsProtocolWithStatusOnly()
    {
        using var server = new FakeLocalApi(_ => FakeLocalApi.Raw(
            string.Concat("{\"type\":\"API_COMMAND_NOT_FOUND\",\"data\":\"", new string('x', SmallCap), "\"}"),
            "application/json",
            HttpStatusCode.NotFound));
        var channel = Channel(server, SmallCap);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(StatusOnly(404)));
    }

    [Test]
    public async Task CallAsync_RedirectFollowedToAnotherHost_ThrowsProtocol()
    {
        using var server = new FakeLocalApi(_ =>
        {
            var response = FakeLocalApi.Json(/*lang=json,strict*/ """{"data":true}""");
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/");
            return response;
        });
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(StatusOnly(200)));
    }

    [Test]
    public void CallAsync_TransportFailure_PropagatesHttpRequestException()
    {
        using var server = new FakeLocalApi(_ => throw new HttpRequestException("boom"));
        var channel = Channel(server);

        Assert.That(async () => await channel.CallAsync(Path, []).WaitAsync(_waitTimeout), Throws.TypeOf<HttpRequestException>());
    }

    // Content without a known length: the size limit must be enforced while reading.
    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
