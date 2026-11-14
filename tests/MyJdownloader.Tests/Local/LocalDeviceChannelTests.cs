namespace MyJdownloader.Tests.Local;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyJdownloader.Local;

[TestFixture]
[SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a handler disposed by the test")]
public sealed partial class LocalDeviceChannelTests
{
    private const string VersionPath = "/jd/version";
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static JsonElement Json(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    private static LocalDeviceChannel Channel(HttpMessageHandler handler, Action<HttpClient>? configure = null, LocalJDownloaderOptions? options = null)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false);
        configure?.Invoke(httpClient);
        return new LocalDeviceChannel(httpClient, LocalEndpoint.Create(options ?? new LocalJDownloaderOptions()));
    }

    private static FakeLocalApi Ok(string body = /*lang=json,strict*/ """{"data":true}""") => new(_ => FakeLocalApi.Json(body));

    [Test]
    public async Task CallAsync_PostsRawJsonParams()
    {
        using var server = Ok();
        var channel = Channel(server);
        JsonElement[] args = [Json(/*lang=json,strict*/ """{"maxResults":1,"name":true}"""), Json("\"a \\\"q\\\" \\\\ é\""), Json("32"), Json("null"), Json("true")];

        await channel.CallAsync("/downloadsV2/queryPackages", args).WaitAsync(_waitTimeout);

        var sent = server.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(sent.Uri, Is.EqualTo("http://127.0.0.1:3128/downloadsV2/queryPackages"));
            Assert.That(sent.MediaType, Is.EqualTo("application/json"));
            Assert.That(sent.Body, Is.EqualTo(/*lang=json,strict*/ """{"params":[{"maxResults":1,"name":true},"a \"q\" \\ é",32,null,true]}"""));
        });
    }

    [Test]
    public async Task CallAsync_NoArgs_PostsEmptyParams()
    {
        using var server = Ok();
        var channel = Channel(server);

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Body, Is.EqualTo(/*lang=json,strict*/ """{"params":[]}"""));
    }

    [Test]
    public async Task CallAsync_BaseAddressWithPrefix_KeepsThePrefix()
    {
        using var server = Ok();
        var channel = Channel(server, options: new LocalJDownloaderOptions { BaseAddress = new Uri("http://127.0.0.1:8080/jd/") });

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Uri, Is.EqualTo("http://127.0.0.1:8080/jd/jd/version"));
    }

    [Test]
    public async Task CallAsync_ReadsData()
    {
        using var server = Ok(/*lang=json,strict*/ """{"data":48637}""");
        var channel = Channel(server);

        var result = await channel.CallAsync(VersionPath, TestJsonContext.Default.Int64, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.EqualTo(48637L));
    }

    [TestCase(/*lang=json,strict*/ """{"data":null}""")]
    [TestCase("{}")]
    public async Task CallAsync_NullOrMissingData_ReturnsDefault(string body)
    {
        using var server = Ok(body);
        var channel = Channel(server);

        var result = await channel.CallAsync(VersionPath, TestJsonContext.Default.String, []).WaitAsync(_waitTimeout);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void CallAsync_Untyped_Completes()
    {
        using var server = Ok(/*lang=json,strict*/ """{"data":true}""");
        var channel = Channel(server);

        Assert.That(async () => await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout), Throws.Nothing);
    }

    [Test]
    public async Task CallAsync_IgnoresHttpClientBaseAddress()
    {
        using var server = Ok();
        var channel = Channel(server, client => client.BaseAddress = new Uri("http://example.org/other/"));

        await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Uri, Is.EqualTo("http://127.0.0.1:3128/jd/version"));
    }

    [TestCase("jd/version")]
    [TestCase("/jd/../x")]
    [TestCase("/jd/version?x")]
    public void CallAsync_InvalidPath_ThrowsSynchronously(string path)
    {
        using var server = Ok();
        var channel = Channel(server);

        Assert.Multiple(() =>
        {
            Assert.That(() => channel.CallAsync(path, []), Throws.TypeOf<ArgumentException>());
            Assert.That(server.Requests, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_NullArgs_ThrowsSynchronously()
    {
        using var server = Ok();
        var channel = Channel(server);

        Assert.That(() => channel.CallAsync(VersionPath, null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void CallAsync_TooManyArgs_ThrowsSynchronously()
    {
        using var server = Ok();
        var channel = Channel(server);
        var args = Enumerable.Repeat(Json("1"), 1_025).ToArray();

        Assert.Multiple(() =>
        {
            var thrown = Assert.Throws<ArgumentException>(() => channel.CallAsync(VersionPath, args));
            Assert.That(thrown!.Message, Does.StartWith("The number of arguments must not exceed 1024."));
            Assert.That(server.Requests, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_ArgsTooLong_ThrowsSynchronously()
    {
        using var server = Ok();
        var channel = Channel(server);
        var args = new[] { Json("\"" + new string('a', 1_048_576) + "\"") };

        Assert.Multiple(() =>
        {
            var thrown = Assert.Throws<ArgumentException>(() => channel.CallAsync(VersionPath, args));
            Assert.That(thrown!.Message, Does.StartWith("The arguments must not exceed 1048576 characters in total."));
            Assert.That(server.Requests, Is.Empty);
        });
    }

    [Test]
    public void CallAsync_UndefinedArg_ThrowsSynchronously()
    {
        using var server = Ok();
        var channel = Channel(server);

        Assert.That(() => channel.CallAsync(VersionPath, [default]), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void CallAsync_DisposedArg_ThrowsSynchronously()
    {
        using var server = Ok();
        var channel = Channel(server);
        var document = JsonDocument.Parse("{}");
        var element = document.RootElement;
        document.Dispose();

        Assert.That(() => channel.CallAsync(VersionPath, [element]), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task CallAsync_ArgsMutatedAfterTheCall_SendsTheSnapshot()
    {
        using var server = Ok();
        var channel = Channel(server);
        JsonElement[] args = [Json("1")];

        var task = channel.CallAsync(VersionPath, args);
        args[0] = Json("2");
        await task.WaitAsync(_waitTimeout);

        Assert.That(server.Requests.Single().Body, Is.EqualTo(/*lang=json,strict*/ """{"params":[1]}"""));
    }

    [Test]
    public void CallAsync_CallerCancellation_ThrowsOperationCanceled()
    {
        using var handler = new WaitingHandler();
        var channel = Channel(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(async () => await channel.CallAsync(VersionPath, [], cts.Token).WaitAsync(_waitTimeout), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task CallAsync_HttpClientTimeout_ThrowsTimeoutException()
    {
        using var handler = new WaitingHandler();
        var channel = Channel(handler, client => client.Timeout = TimeSpan.FromMilliseconds(50));

        var thrown = await Assert.ThrowsAsync<TimeoutException>(async () => await channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.That(thrown!.Message, Is.EqualTo("The JDownloader local API did not answer in time."));
    }

    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(string))]
    private sealed partial class TestJsonContext : JsonSerializerContext;

    private sealed class WaitingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("unreachable");
        }
    }
}
