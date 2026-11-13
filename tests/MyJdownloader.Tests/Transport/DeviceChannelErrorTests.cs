namespace MyJdownloader.Tests.Transport;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Crypto;

public sealed partial class DeviceChannelTests
{
    private const string NewSessionToken = "4455eeff";
    private const string NewRegainToken = "66778899";
    private const string LeakDeviceId = "devsecret99";
    private const int SmallCap = 2_048;
    private const string DeviceTooLargeMessage = "The MyJD device response exceeds the configured size limit.";

    private static IEnumerable<TestCaseData> FailureScenarios()
    {
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"OFFLINE"}""", HttpStatusCode.ServiceUnavailable))).SetName("Offline");
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.EncryptedJson(DeviceKey(), /*lang=json,strict*/ """{"src":"DEVICE","type":"AUTH_FAILED"}""", HttpStatusCode.Forbidden))).SetName("EncryptedError");
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.Raw("[]", HttpStatusCode.InternalServerError))).SetName("NotAnObject");
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"TOO_MANY_REQUESTS"}""", HttpStatusCode.TooManyRequests))).SetName("RateLimited");
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"TOKEN_INVALID"}""", HttpStatusCode.Forbidden))).SetName("DeviceTokenInvalid");
        yield return new TestCaseData(Scenario(_ => FakeMyJdServer.Raw(new string('A', 1_048_577)))).SetName("Oversize");
        yield return new TestCaseData(Scenario(rid => FakeMyJdServer.EncryptedJson(DeviceKey(), DataBody(rid + 1, "true")))).SetName("RidMismatch");
    }

    private static Func<long, HttpResponseMessage> Scenario(Func<long, HttpResponseMessage> reply) => reply;

    private static string ReconnectBody(long rid) =>
        string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{NewSessionToken}}","regaintoken":"{{NewRegainToken}}","rid":{{rid}}}""");

    private static Sent Capture(HttpRequestMessage request, byte[] key)
    {
        var body = FakeMyJdServer.DecryptedBodyOf(request, key);
        return new Sent(request.RequestUri!.PathAndQuery, body, FakeMyJdServer.BodyRidOf(request, key));
    }

    [Test]
    public async Task CallAsync_DeviceOffline_ThrowsDeviceOffline()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"OFFLINE"}""", HttpStatusCode.ServiceUnavailable));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdDeviceOfflineException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task CallAsync_EncryptedErrorBody_IsMapped()
    {
        var rig = CreateRig(_ => FakeMyJdServer.EncryptedJson(DeviceKey(), /*lang=json,strict*/ """{"src":"DEVICE","type":"AUTH_FAILED"}""", HttpStatusCode.Forbidden));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public async Task CallAsync_ErrorBodyNotAnObject_MapsStatus()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw("[]", HttpStatusCode.InternalServerError));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorType, Is.EqualTo("UNKNOWN"));
            Assert.That(thrown.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        });
    }

    [Test]
    public async Task CallAsync_RateLimited_ExposesClampedRetryAfter()
    {
        var rig = CreateRig(_ =>
        {
            var response = FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"TOO_MANY_REQUESTS"}""", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdRateLimitException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public async Task CallAsync_ServerTokenInvalid_ReplaysWithNewSessionAndKey()
    {
        using var handler = new RenewalHandler(null);
        using var httpClient = new HttpClient(handler);
        var client = new MyJDownloaderClient(httpClient);
        client.RestoreSession(CreateSession());
        var channel = client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        var devices = handler.Sent.Where(s => s.Path.StartsWith("/t_", StringComparison.Ordinal)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new Sample(3)));
            Assert.That(handler.Sent.Count(s => s.Path.StartsWith(ReconnectPath, StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(devices[1].Path, Does.StartWith("/t_" + NewSessionToken + "_"));
            Assert.That(devices[1].Rid, Is.Not.EqualTo(devices[0].Rid));
        });
    }

    [Test]
    public async Task CallAsync_ArgsDocumentDisposedBeforeReplay_ReplaysSnapshot()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new RenewalHandler(gate.Task);
        using var httpClient = new HttpClient(handler);
        var client = new MyJDownloaderClient(httpClient);
        client.RestoreSession(CreateSession());
        var channel = client.CreateDeviceChannel("dev1");
        var document = JsonDocument.Parse(/*lang=json,strict*/ "[\"abc\",{\"a\":1}]");
        var args = new[] { document.RootElement[0], document.RootElement[1] };

        var task = channel.CallAsync(VersionPath, args);
        await handler.FirstAttemptEntered.WaitAsync(_waitTimeout);
        document.Dispose();
        gate.SetResult();
        await task.WaitAsync(_waitTimeout);

        var devices = handler.Sent.Where(s => s.Path.StartsWith("/t_", StringComparison.Ordinal)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(ParamsOf(devices[1].Body), Is.EqualTo(_twoParams));
            Assert.That(devices[1].Rid, Is.Not.EqualTo(devices[0].Rid));
        });
    }

    [Test]
    public async Task CallAsync_DeviceTokenInvalid_DoesNotRenew()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"DEVICE","type":"TOKEN_INVALID"}""", HttpStatusCode.Forbidden));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(rig.Calls, Has.Count.EqualTo(1));
            Assert.That(rig.Calls.Count(c => c.PathAndQuery.StartsWith(ReconnectPath, StringComparison.Ordinal)), Is.Zero);
        });
    }

    [Test]
    public async Task CallAsync_BodyOverOneMebibyte_ThrowsProtocol()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw(new string('A', 1_048_577)));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(thrown.Message, Is.EqualTo(DeviceTooLargeMessage));
        });
    }

    [Test]
    public async Task CallAsync_DeclaredLengthOverCustomCap_ThrowsProtocolWithSizeLimitMessage()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw(new string('A', SmallCap + 1)), new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(thrown.Message, Is.EqualTo(DeviceTooLargeMessage));
        });
    }

    [Test]
    public async Task CallAsync_StreamedBodyOverCustomCap_ThrowsProtocolWithSizeLimitMessage()
    {
        var rig = CreateRig(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new UnknownLengthContent(SmallCap + 1) },
            new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(thrown.Message, Is.EqualTo(DeviceTooLargeMessage));
        });
    }

    [Test]
    public async Task CallAsync_BodyExactlyAtCustomCap_IsNotRejectedForSize()
    {
        // Not a valid encrypted payload: it must fail as unreadable, which proves the size check let it through.
        var rig = CreateRig(_ => FakeMyJdServer.Raw(new string('A', SmallCap)), new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo(DeviceUnreadable()));
    }

    [Test]
    public async Task CallAsync_ResponseOverOneMebibyteWithRaisedCap_ReturnsData()
    {
        // The encrypted (Base64) body is about 1.4 MiB: over the default cap, within the raised one.
        var padding = new string('a', 1_000_000);
        var rig = CreateRig(
            call => FakeMyJdServer.EncryptedJson(DeviceKey(), string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{{\"count\":3,\"pad\":\"{padding}\"}},\"rid\":{call.Rid}}}")),
            new MyJDownloaderOptions { MaxResponseBodyLength = 4 * 1_048_576 });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(new Sample(3)));
    }

    [Test]
    public async Task CallAsync_ErrorBodyOverCustomCap_MapsFromStatusOnly()
    {
        var rig = CreateRig(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new UnknownLengthContent(SmallCap + 1) },
            new MyJDownloaderOptions { MaxResponseBodyLength = SmallCap });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(thrown.ErrorType, Is.EqualTo("UNKNOWN"));
            Assert.That(thrown.Message, Is.Not.EqualTo(DeviceTooLargeMessage));
        });
    }

    [Test]
    public async Task CallAsync_CapAtIntMaxValue_ReturnsData()
    {
        var rig = CreateRig(
            call => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new UnknownLengthTextContent(MyJdCrypto.Encrypt(DeviceKey(), DataBody(call.Rid, /*lang=json,strict*/ "{\"count\":3}"))),
            },
            new MyJDownloaderOptions { MaxResponseBodyLength = int.MaxValue });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(new Sample(3)));
    }

    [Test]
    public async Task CallAsync_ErrorBodyOverOneMebibyteWithRaisedCap_MapsFromStatusOnly()
    {
        // A syntactically valid OFFLINE error padded past 1 MiB (unknown members are skipped, so it parses today).
        var padding = new string('a', 1_048_577);
        var body = string.Create(CultureInfo.InvariantCulture, $"{{\"src\":\"DEVICE\",\"type\":\"OFFLINE\",\"pad\":\"{padding}\"}}");
        var rig = CreateRig(
            _ => FakeMyJdServer.Raw(body, HttpStatusCode.ServiceUnavailable),
            new MyJDownloaderOptions { MaxResponseBodyLength = 4 * 1_048_576 });
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdApiException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(thrown.ErrorType, Is.EqualTo("UNKNOWN"));
        });
    }

    [Test]
    public async Task CallAsync_Timeout_ThrowsTimeoutException()
    {
        using var handler = new AwaitCancellationHandler();
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        var client = new MyJDownloaderClient(httpClient);
        client.RestoreSession(CreateSession());
        var channel = client.CreateDeviceChannel("dev1");

        await Assert.ThrowsAsync<TimeoutException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));
    }

    [Test]
    public async Task CallAsync_Cancelled_ThrowsOperationCanceled()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.CatchAsync<OperationCanceledException>(() => channel.CallAsync(VersionPath, [], cts.Token).WaitAsync(_waitTimeout));
    }

    [TestCaseSource(nameof(FailureScenarios))]
    public async Task CallAsync_Failures_DoNotLeakIdentifiers(Func<long, HttpResponseMessage> reply)
    {
        var rig = CreateRig(call => reply(call.Rid));
        var channel = rig.Client.CreateDeviceChannel(LeakDeviceId);

        var thrown = await Assert.CatchAsync<Exception>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Does.Not.Contain(SessionToken));
            Assert.That(thrown.Message, Does.Not.Contain(LeakDeviceId));
            Assert.That(thrown.Message, Does.Not.Contain(VersionPath));
            Assert.That(thrown.InnerException, Is.Null);
        });
    }

    [Test]
    public void CallAsync_ResponseTypeNotSupported_PropagatesNotSupportedException()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver(), Converters = { new ThrowingSampleConverter() } };
        var typeInfo = (JsonTypeInfo<Sample>)options.GetTypeInfo(typeof(Sample));
        var rig = CreateRig(call => Ok(call, /*lang=json,strict*/ "{\"count\":3}"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        Assert.ThrowsAsync<NotSupportedException>(() => channel.CallAsync(VersionPath, typeInfo, []).WaitAsync(_waitTimeout));
    }

    [Test]
    public void CallAsync_LoneSurrogateStringArg_ThrowsArgumentException()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");
        using var document = JsonDocument.Parse(/*lang=json,strict*/ "\"\\uD800\"");

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync(VersionPath, [document.RootElement]));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("args"));
            Assert.That(rig.Calls, Is.Empty);
        });
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

    private sealed class UnknownLengthTextContent(string text) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ArgumentNullException.ThrowIfNull(stream);
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(text));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed record Sent(string Path, string Body, long Rid);

    private sealed class ThrowingSampleConverter : JsonConverter<Sample>
    {
        public override Sample Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Not supported by design.");

        public override void Write(Utf8JsonWriter writer, Sample value, JsonSerializerOptions options) =>
            throw new NotSupportedException("Not supported by design.");
    }

    private sealed class AwaitCancellationHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// First device attempt (old session key) answers a plain server TOKEN_INVALID once the optional gate opens;
    /// <c>/my/reconnect</c> returns new tokens; the replay (new device key) answers a valid sample.
    /// </summary>
    private sealed class RenewalHandler(Task? gate) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<Sent> Sent { get; } = [];

        public Task FirstAttemptEntered => _entered.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith(ReconnectPath, StringComparison.Ordinal))
            {
                Sent.Add(new Sent(path, string.Empty, 0));
                return FakeMyJdServer.EncryptedJson(Key(3), ReconnectBody(FakeMyJdServer.RidOf(request)));
            }

            if (path.StartsWith("/t_" + SessionToken + "_", StringComparison.Ordinal))
            {
                Sent.Add(Capture(request, DeviceKey()));
                _entered.TrySetResult();
                if (gate is not null)
                {
                    await gate.WaitAsync(cancellationToken);
                }

                return FakeMyJdServer.Raw(/*lang=json,strict*/ """{"src":"SERVER","type":"TOKEN_INVALID"}""", HttpStatusCode.Forbidden);
            }

            var newKey = MyJdCrypto.DeriveToken(Key(2), NewSessionToken);
            var sent = Capture(request, newKey);
            Sent.Add(sent);
            return FakeMyJdServer.EncryptedJson(newKey, DataBody(sent.Rid, /*lang=json,strict*/ "{\"count\":3}"));
        }
    }
}
