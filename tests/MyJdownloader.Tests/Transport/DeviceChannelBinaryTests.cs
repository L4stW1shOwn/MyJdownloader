namespace MyJdownloader.Tests.Transport;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using MyJdownloader.Errors;
using MyJdownloader.Transport;

[TestFixture]
public sealed class DeviceChannelBinaryTests
{
    private const string SessionToken = "0011aabb";
    private const string NewSessionToken = "4455eeff";
    private const string NewRegainToken = "66778899";
    private const string IconPath = "/contentV2/getIcon";
    private const int OneMebibyte = 1_048_576;
    private const int SmallCap = 2_048;
    private const string DeviceTooLargeMessage = "The MyJD device response exceeds the configured size limit.";
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _png = [.. _pngSignature, 0x01, 0x02];

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), SessionToken, "ccdd2233", Key(3), Key(4));

    private static string DeviceUnreadable() => RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);

    private static IEnumerable<string?> WrongContentTypes()
    {
        yield return null;
        yield return "application/json";
        yield return "image/jpeg";
    }

    private static IEnumerable<byte[]> BadBodies()
    {
        yield return [];
        yield return _pngSignature[..7];
        yield return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0B];
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a handler disposed by the test")]
    private static IBinaryDeviceChannel Channel(HttpMessageHandler handler, TimeSpan? timeout = null, MyJDownloaderOptions? options = null)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false);
        if (timeout is not null)
        {
            httpClient.Timeout = timeout.Value;
        }

        var client = new MyJDownloaderClient(httpClient, options);
        client.RestoreSession(CreateSession());
        return (IBinaryDeviceChannel)client.CreateDeviceChannel("dev1");
    }

    private static void AssertDeviceUnreadable(MyJdProtocolException thrown) =>
        Assert.Multiple(() =>
        {
            Assert.That(thrown.Message, Is.EqualTo(DeviceUnreadable()));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        });

    private static void AssertDeviceTooLarge(MyJdProtocolException thrown) =>
        Assert.Multiple(() =>
        {
            Assert.That(thrown.Message, Is.EqualTo(DeviceTooLargeMessage));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        });

    [Test]
    public async Task CallBinaryAsync_PngResponse_ReturnsBodyUnchanged()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(_png));
        var channel = Channel(server);

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(_png));
    }

    [Test]
    public async Task CallBinaryAsync_Request_IsEncryptedWithPathArgsAndRid()
    {
        string? body = null;
        string? pathAndQuery = null;
        using var server = new FakeMyJdServer(request =>
        {
            body = FakeMyJdServer.DecryptedBodyOf(request, Key(4));
            pathAndQuery = request.RequestUri!.PathAndQuery;
            return FakeMyJdServer.Png(_png);
        });
        var channel = Channel(server);
        using var document = JsonDocument.Parse("\"icon-key\"");

        await channel.CallBinaryAsync(IconPath, [document.RootElement]).WaitAsync(_waitTimeout);

        using var parsed = JsonDocument.Parse(body!);
        Assert.Multiple(() =>
        {
            Assert.That(pathAndQuery, Is.EqualTo("/t_0011aabb_dev1/contentV2/getIcon"));
            Assert.That(parsed.RootElement.GetProperty("url").GetString(), Is.EqualTo(IconPath));
            Assert.That(parsed.RootElement.GetProperty("params")[0].GetString(), Is.EqualTo("icon-key"));
            Assert.That(parsed.RootElement.GetProperty("rid").GetInt64(), Is.Positive);
        });
    }

    [TestCase("image/png; charset=binary")]
    [TestCase("IMAGE/PNG")]
    public async Task CallBinaryAsync_ContentTypeWithParametersOrUpperCase_ReturnsBody(string contentType)
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(_png, contentType));
        var channel = Channel(server);

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(_png));
    }

    [TestCaseSource(nameof(WrongContentTypes))]
    public async Task CallBinaryAsync_MissingOrWrongContentType_ThrowsProtocolException(string? contentType)
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(_png, contentType));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [TestCaseSource(nameof(BadBodies))]
    public async Task CallBinaryAsync_BadOrShortSignature_ThrowsProtocolException(byte[] body)
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(body));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallBinaryAsync_DeclaredLengthOverOneMebibyte_ThrowsProtocolException()
    {
        using var server = new FakeMyJdServer(_ =>
        {
            var response = FakeMyJdServer.Png(OversizeBody());
            response.Content.Headers.ContentLength = OneMebibyte + 1;
            return response;
        });
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceTooLarge(thrown!);
    }

    [Test]
    public async Task CallBinaryAsync_StreamedBodyOverOneMebibyte_ThrowsProtocolException()
    {
        using var server = new FakeMyJdServer(_ =>
        {
            var content = new StreamContent(new NonSeekableStream(OversizeBody()));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceTooLarge(thrown!);
    }

    [Test]
    public async Task CallBinaryAsync_DeclaredLengthOverCustomCap_ThrowsProtocolException()
    {
        using var server = new FakeMyJdServer(_ =>
        {
            var response = FakeMyJdServer.Png(PngBody(SmallCap + 1));
            response.Content.Headers.ContentLength = SmallCap + 1;
            return response;
        });
        var channel = Channel(server, options: new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceTooLarge(thrown!);
    }

    [Test]
    public async Task CallBinaryAsync_StreamedBodyOverCustomCap_ThrowsProtocolException()
    {
        using var server = new FakeMyJdServer(_ =>
        {
            var content = new StreamContent(new NonSeekableStream(PngBody(SmallCap + 1)));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        var channel = Channel(server, options: new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        AssertDeviceTooLarge(thrown!);
    }

    [Test]
    public async Task CallBinaryAsync_BodyExactlyAtCustomCap_ReturnsBody()
    {
        var body = PngBody(SmallCap);
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(body));
        var channel = Channel(server, options: new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(body));
    }

    [Test]
    public async Task CallBinaryAsync_BodyOverOneMebibyteWithRaisedCap_ReturnsBody()
    {
        var body = PngBody(OneMebibyte + 1);
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(body));
        var channel = Channel(server, options: new MyJDownloaderOptions { MaxResponseBodyLength = 2 * OneMebibyte });

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(body));
    }

    [Test]
    public async Task CallBinaryAsync_EncryptedDeviceError_ThrowsApiException()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.EncryptedJson(
            Key(4),
            /*lang=json,strict*/ """{"src":"DEVICE","type":"FILE_NOT_FOUND"}""",
            HttpStatusCode.NotFound));
        var channel = Channel(server);

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorType, Is.EqualTo("FILE_NOT_FOUND"));
    }

    [Test]
    public async Task CallBinaryAsync_TokenInvalid_ReconnectsAndReplaysOnce()
    {
        var devicePaths = new List<string>();
        var reconnects = 0;
        using var server = new FakeMyJdServer(request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/my/reconnect", StringComparison.Ordinal))
            {
                reconnects++;
                var body = string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{NewSessionToken}}","regaintoken":"{{NewRegainToken}}","rid":{{FakeMyJdServer.RidOf(request)}}}""");
                return FakeMyJdServer.EncryptedJson(Key(3), body);
            }

            devicePaths.Add(path);
            return path.StartsWith("/t_" + SessionToken + "_", StringComparison.Ordinal)
                ? FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"SERVER","type":"TOKEN_INVALID"}""", HttpStatusCode.Forbidden)
                : FakeMyJdServer.Png(_png);
        });
        var channel = Channel(server);

        var result = await channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(_png));
            Assert.That(reconnects, Is.EqualTo(1));
            Assert.That(devicePaths, Has.Count.EqualTo(2));
            Assert.That(devicePaths[1], Does.StartWith("/t_" + NewSessionToken + "_"));
        });
    }

    [TestCase("")]
    [TestCase("x")]
    [TestCase("/a/../my")]
    [TestCase("/a?b")]
    public void CallBinaryAsync_InvalidPath_ThrowsArgumentExceptionSynchronously(string path)
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Png(_png));
        var channel = Channel(server);

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallBinaryAsync(path, []));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("path"));
            Assert.That(server.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task CallBinaryAsync_Timeout_ThrowsTimeoutException()
    {
        using var handler = new AwaitCancellationHandler();
        var channel = Channel(handler, TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TimeoutException>(() => channel.CallBinaryAsync(IconPath, []).WaitAsync(_waitTimeout));
    }

    private static byte[] OversizeBody() => PngBody(OneMebibyte + 1);

    private static byte[] PngBody(int length)
    {
        var body = new byte[length];
        _pngSignature.CopyTo(body, 0);
        return body;
    }

    private sealed class AwaitCancellationHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    /// <summary>A stream without length, so the content cannot declare a Content-Length.</summary>
    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
