namespace MyJdownloader.Tests.Transport;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyJdownloader.Transport;

[TestFixture]
public sealed partial class DeviceChannelTests
{
    private const string Email = "user@example.com";
    private const string SessionToken = "0011aabb";
    private const string VersionPath = "/jd/version";
    private const string ReconnectPath = "/my/reconnect";
    private const string NotConnectedMessage = "The client is not connected.";
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static readonly string[] _twoParams = ["abc", /*lang=json,strict*/ "{\"a\":1}"];
    private static readonly string[] _threeParams = ["abc", /*lang=json,strict*/ "{\"a\":1}", "5"];

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static byte[] DeviceKey() => Key(4);

    private static MyJdSession CreateSession() =>
        new(Email, Key(1), Key(2), SessionToken, "ccdd2233", Key(3), DeviceKey());

    private static JsonElement Json(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    private static string DataBody(long rid, string dataJson) =>
        string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{dataJson},\"rid\":{rid}}}");

    private static HttpResponseMessage Ok(Call call, string dataJson) =>
        FakeMyJdServer.EncryptedJson(DeviceKey(), DataBody(call.Rid, dataJson));

    private static string DeviceUnreadable() => RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);

    private static string DeviceMismatch() => RelayTransport.MismatchMessageFor(MyJdErrorSource.Device);

    private static IEnumerable<string> InvalidPaths()
    {
        yield return string.Empty;
        yield return "x";
        yield return "/a/../my";
        yield return "/a-b";
        yield return "/a.b";
        yield return "/./a";
        yield return "/a/..";
        yield return "/a?b";
        yield return "/a b";
        yield return "//a";
        yield return "/a/";
        yield return "/a\n";
        yield return "/" + new string('a', 256);
    }

    private static IEnumerable<string?> InvalidDeviceIds()
    {
        yield return null;
        yield return string.Empty;
        yield return " ";
        yield return new string('a', 257);
        yield return "a\u0001";
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static Rig CreateRig(Func<Call, HttpResponseMessage> reply, bool restoreSession = true)
    {
        var calls = new List<Call>();
        var server = new FakeMyJdServer(request =>
        {
            var call = Call.From(request);
            calls.Add(call);
            return reply(call);
        });
        var client = new MyJDownloaderClient(new HttpClient(server));
        if (restoreSession)
        {
            client.RestoreSession(CreateSession());
        }

        return new Rig(client, calls);
    }

    [Test]
    public async Task CallAsync_TypedData_ReturnsDeserializedValue()
    {
        var rig = CreateRig(call => Ok(call, /*lang=json,strict*/ "{\"count\":3}"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(new Sample(3)));
    }

    [Test]
    public async Task CallAsync_NoResult_Completes()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(rig.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CallAsync_DataMissing_ReturnsDefault()
    {
        var rig = CreateRig(call => FakeMyJdServer.EncryptedJson(
            DeviceKey(),
            string.Create(CultureInfo.InvariantCulture, $"{{\"rid\":{call.Rid}}}")));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CallAsync_DataNull_ReturnsDefault()
    {
        var rig = CreateRig(call => Ok(call, "null"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var result = await channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CallAsync_Request_IsEncryptedPostToDevicePath()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        var call = rig.Calls.Single();
        Assert.Multiple(() =>
        {
            Assert.That(call.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(call.ContentType, Is.EqualTo("application/aesjson-jd; charset=utf-8"));
            Assert.That(call.PathAndQuery, Is.EqualTo("/t_0011aabb_dev1/jd/version"));
        });
    }

    [TestCase("dev/1 2", "/t_0011aabb_dev%2F1%202/jd/version")]
    [TestCase("../my/listdevices", "/t_0011aabb_..%2Fmy%2Flistdevices/jd/version")]
    public async Task CallAsync_DeviceIdWithSlashes_IsEscapedInPath(string deviceId, string expectedPath)
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel(deviceId);

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(rig.Calls.Single().PathAndQuery, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task CallAsync_Body_HasUrlParamsRidApiVer()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        var call = rig.Calls.Single();
        var expected = string.Create(CultureInfo.InvariantCulture, $"{{\"url\":\"/jd/version\",\"params\":[],\"rid\":{call.Rid},\"apiVer\":1}}");
        Assert.That(call.Body, Is.EqualTo(expected));
    }

    [Test]
    public async Task CallAsync_StringAndObjectArgs_AreEncodedPerReference()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        await channel.CallAsync(VersionPath, [Json("\"abc\""), Json(/*lang=json,strict*/ "{\"a\":1}"), Json("5")]).WaitAsync(_waitTimeout);

        Assert.That(ParamsOf(rig.Calls.Single().Body), Is.EqualTo(_threeParams));
    }

    [TestCaseSource(nameof(InvalidPaths))]
    public void CallAsync_InvalidPath_ThrowsWithoutRequest(string path)
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync(path, []));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("path"));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [TestCaseSource(nameof(InvalidDeviceIds))]
    public void CreateDeviceChannel_InvalidId_Throws(string? deviceId)
    {
        var rig = CreateRig(call => Ok(call, "true"));

        var thrown = Assert.Catch<ArgumentException>(() => rig.Client.CreateDeviceChannel(deviceId!));

        Assert.That(thrown!.ParamName, Is.EqualTo("deviceId"));
    }

    [Test]
    public void CreateDeviceChannel_InvalidUtf16Id_Throws()
    {
        var rig = CreateRig(call => Ok(call, "true"));

        var thrown = Assert.Catch<ArgumentException>(() => rig.Client.CreateDeviceChannel("a\uD800"));

        Assert.That(thrown!.ParamName, Is.EqualTo("deviceId"));
    }

    [Test]
    public void CallAsync_NullArgs_Throws()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync(VersionPath, null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("args"));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_UndefinedArg_Throws()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync(VersionPath, [default]));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("args"));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_DisposedArgDocument_ThrowsArgumentException()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");
        var document = JsonDocument.Parse("\"abc\"");
        var element = document.RootElement;
        document.Dispose();

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync(VersionPath, [element]));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("args"));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_NullResponseType_Throws()
    {
        var rig = CreateRig(call => Ok(call, "true"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = Assert.Catch<ArgumentException>(() => channel.CallAsync<Sample>(VersionPath, null!, []));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("responseType"));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [Test]
    public async Task CallAsync_NotConnected_ThrowsInvalidOperation()
    {
        var rig = CreateRig(call => Ok(call, "true"), restoreSession: false);
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Is.EqualTo(NotConnectedMessage));
            Assert.That(rig.Calls, Is.Empty);
        });
    }

    [Test]
    public async Task CallAsync_RidMismatch_ThrowsDeviceMismatch()
    {
        var rig = CreateRig(call => FakeMyJdServer.EncryptedJson(DeviceKey(), DataBody(call.Rid + 1, "true")));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Is.EqualTo(DeviceMismatch()));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        });
    }

    [Test]
    public async Task CallAsync_RidMissing_ThrowsDeviceMismatch()
    {
        var rig = CreateRig(_ => FakeMyJdServer.EncryptedJson(DeviceKey(), /*lang=json,strict*/ "{\"data\":true}"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Is.EqualTo(DeviceMismatch()));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        });
    }

    [Test]
    public async Task CallAsync_InvalidBase64_ThrowsUnreadable()
    {
        var rig = CreateRig(_ => FakeMyJdServer.Raw("!!! not base64 !!!"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallAsync_InvalidJson_ThrowsUnreadable()
    {
        var rig = CreateRig(_ => FakeMyJdServer.EncryptedJson(DeviceKey(), "{not json"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallAsync_DataOfWrongType_ThrowsUnreadable()
    {
        var rig = CreateRig(call => Ok(call, "\"text\""));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, SampleContext.Default.Sample, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallAsync_ResponseWithDuplicateRid_ThrowsUnreadable()
    {
        var rig = CreateRig(call => FakeMyJdServer.EncryptedJson(
            DeviceKey(),
            string.Create(CultureInfo.InvariantCulture, $"{{\"rid\":{call.Rid},\"rid\":{call.Rid},\"data\":1}}")));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallAsync_ResponseRootNotObject_ThrowsUnreadable()
    {
        var rig = CreateRig(_ => FakeMyJdServer.EncryptedJson(DeviceKey(), "[1]"));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        AssertDeviceUnreadable(thrown!);
    }

    [Test]
    public async Task CallAsync_EncryptedErrorClaimingServerTokenInvalid_DoesNotRenew()
    {
        var rig = CreateRig(_ => FakeMyJdServer.EncryptedJson(
            DeviceKey(),
            /*lang=json,strict*/ """{"src":"SERVER","type":"TOKEN_INVALID"}""",
            HttpStatusCode.Forbidden));
        var channel = rig.Client.CreateDeviceChannel("dev1");

        var thrown = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(rig.Calls.Count(c => c.PathAndQuery.StartsWith(ReconnectPath, StringComparison.Ordinal)), Is.Zero);
        });
    }

    private static string[] ParamsOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return [.. document.RootElement.GetProperty("params").EnumerateArray().Select(e => e.GetString()!)];
    }

    private static void AssertDeviceUnreadable(MyJdProtocolException thrown) =>
        Assert.Multiple(() =>
        {
            Assert.That(thrown.Message, Is.EqualTo(DeviceUnreadable()));
            Assert.That(thrown.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
            Assert.That(thrown.InnerException, Is.Null);
        });

    private sealed record Sample(int Count);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Sample))]
    private sealed partial class SampleContext : JsonSerializerContext;

    /// <summary>What the fake server saw of one request, captured while the request was still alive.</summary>
    private sealed record Call(HttpMethod Method, string PathAndQuery, string? ContentType, string Body, long Rid)
    {
        public static Call From(HttpRequestMessage request)
        {
            if (request.Content is null)
            {
                return new Call(request.Method, request.RequestUri!.PathAndQuery, null, string.Empty, 0);
            }

            return new Call(
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Content.Headers.ContentType?.ToString(),
                FakeMyJdServer.DecryptedBodyOf(request, Key(4)),
                FakeMyJdServer.BodyRidOf(request, Key(4)));
        }
    }

    private sealed record Rig(MyJDownloaderClient Client, List<Call> Calls);
}
