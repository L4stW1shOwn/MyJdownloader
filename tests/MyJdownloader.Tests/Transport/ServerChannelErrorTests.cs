namespace MyJdownloader.Tests.Transport;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using MyJdownloader.Crypto;
using MyJdownloader.Transport;

[TestFixture]
public sealed class ServerChannelErrorTests
{
    private const string Email = "user@example.com";
    private const string Password = "password";
    private const string UnreadableMessage = "The MyJD server returned a response that could not be read.";
#pragma warning disable JSON002 // Probable JSON string: intentional protocol payloads used as plain string vectors
    private const string AuthFailedBody = """{"src":"SERVER","type":"AUTH_FAILED"}""";
    private const string TooManyRequestsBody = """{"src":"SERVER","type":"TOO_MANY_REQUESTS"}""";
#pragma warning restore JSON002

    private static readonly DateTimeOffset Now = new(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static ServerChannel CreateChannel(FakeMyJdServer server, TimeSpan? timeout = null)
    {
        var clock = new FakeTimeProvider { UtcNow = Now };
        var httpClient = new HttpClient(server);
        if (timeout is not null)
        {
            httpClient.Timeout = timeout.Value;
        }

        return new ServerChannel(httpClient, new MyJDownloaderOptions(), new RequestIdGenerator(clock), clock);
    }

    private static HttpResponseMessage TooManyRequests(string? retryAfter)
    {
        var response = FakeMyJdServer.Raw(TooManyRequestsBody, HttpStatusCode.TooManyRequests);
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        }

        return response;
    }

    private static async Task<MyJdRateLimitException> ConnectExpectingRateLimitAsync(string? retryAfter)
    {
        using var server = new FakeMyJdServer(_ => TooManyRequests(retryAfter));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdRateLimitException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        return exception!;
    }

    [Test]
    public async Task ConnectAsync_Forbidden_PlainAuthFailed_ThrowsAuthentication()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.ErrorType, Is.EqualTo("AUTH_FAILED"));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
    }

    [Test]
    public async Task ConnectAsync_EncryptedErrorBody_IsRead()
    {
        var loginSecret = MyJdCrypto.DeriveLoginSecret(Email, Password);
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.EncryptedJson(loginSecret, AuthFailedBody, HttpStatusCode.Forbidden));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.ErrorType, Is.EqualTo("AUTH_FAILED"));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ConnectAsync_ServiceUnavailableHtml_ThrowsApiUnknown()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw("<html><body>Service Unavailable</body></html>", HttpStatusCode.ServiceUnavailable));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdApiException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.ErrorType, Is.EqualTo("UNKNOWN"));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task ConnectAsync_TooManyRequests_RetryAfterSeconds_Carried()
    {
        var exception = await ConnectExpectingRateLimitAsync("30");

        Assert.That(exception.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(exception.ErrorType, Is.EqualTo("TOO_MANY_REQUESTS"));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public async Task ConnectAsync_RetryAfterHuge_ClampedToTenMinutes()
    {
        var exception = await ConnectExpectingRateLimitAsync("99999");

        Assert.That(exception.RetryAfter, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    [Test]
    public async Task ConnectAsync_RetryAfterPastDate_ClampedToZero()
    {
        var exception = await ConnectExpectingRateLimitAsync(Now.AddHours(-1).ToString("R", CultureInfo.InvariantCulture));

        Assert.That(exception.RetryAfter, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task ConnectAsync_RetryAfterFutureDate_RelativeToTimeProvider()
    {
        var exception = await ConnectExpectingRateLimitAsync(Now.AddSeconds(120).ToString("R", CultureInfo.InvariantCulture));

        Assert.That(exception.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(120)));
    }

    [TestCase("abc")]
    public async Task ConnectAsync_RetryAfterUnparsable_IsNull(string value)
    {
        var exception = await ConnectExpectingRateLimitAsync(value);

        Assert.That(exception.RetryAfter, Is.Null);
    }

    [Test]
    public async Task ConnectAsync_RetryAfterAbsent_IsNull()
    {
        var exception = await ConnectExpectingRateLimitAsync(null);

        Assert.That(exception.RetryAfter, Is.Null);
    }

    [Test]
    public async Task ConnectAsync_Cancelled_ThrowsOperationCanceled()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden));
        var channel = CreateChannel(server);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var exception = await Assert.CatchAsync<OperationCanceledException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), cts.Token));

        Assert.That(exception, Is.Not.InstanceOf<MyJdException>());
    }

    [Test]
    public async Task ConnectAsync_NetworkFailure_PropagatesHttpRequestException()
    {
        var failure = new HttpRequestException("network down");
        using var server = new FakeMyJdServer(_ => throw failure);
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception, Is.SameAs(failure));
    }

    [Test]
    public async Task ConnectAsync_ErrorStatus_MessageContainsNoEmail()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Does.Not.Contain(Email));
        Assert.That(exception.Message, Does.Not.Contain(Password));
    }

    [Test]
    public async Task ConnectAsync_BodyOverLimitWithoutContentLength_ThrowsUnreadable()
    {
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new UnknownLengthContent(1_048_577) });
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(UnreadableMessage));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public async Task ConnectAsync_ErrorBodyEmptyObject_ThrowsApiUnknown()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw("{}", HttpStatusCode.Forbidden));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdApiException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.ErrorType, Is.EqualTo("UNKNOWN"));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ConnectAsync_ErrorBodyReadFails_StatusStillMapped()
    {
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StreamContent(new ScriptedStream(_ => throw new IOException("connection reset"))),
        });
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdApiException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ConnectAsync_SlowBody_ThrowsTimeoutException()
    {
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ScriptedStream(token => Task.Delay(Timeout.Infinite, token))),
        });
        var channel = CreateChannel(server, TimeSpan.FromMilliseconds(200));

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("The MyJD server did not answer in time."));
    }

    [Test]
    public async Task ConnectAsync_SlowHeaders_ThrowsTimeoutException()
    {
        using var server = new SlowHandler();
        var clock = new FakeTimeProvider { UtcNow = Now };
        using var httpClient = new HttpClient(server) { Timeout = TimeSpan.FromMilliseconds(200) };
        var channel = new ServerChannel(httpClient, new MyJDownloaderOptions(), new RequestIdGenerator(clock), clock);

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("The MyJD server did not answer in time."));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public async Task ConnectAsync_CallerCancelsDuringBody_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ScriptedStream(async token =>
            {
                await cts.CancelAsync();
                await Task.Delay(Timeout.Infinite, token);
            })),
        });
        var channel = CreateChannel(server);

        var exception = await Assert.CatchAsync<OperationCanceledException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), cts.Token));

        Assert.That(exception, Is.Not.InstanceOf<MyJdException>());
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ScriptedStream(Func<CancellationToken, Task> onRead) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await onRead(cancellationToken);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class UnknownLengthContent(int size) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ArgumentNullException.ThrowIfNull(stream);
            await stream.WriteAsync(new byte[size]);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
