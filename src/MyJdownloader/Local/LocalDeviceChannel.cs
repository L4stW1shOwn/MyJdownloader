namespace MyJdownloader.Local;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Errors;
using MyJdownloader.Json;
using MyJdownloader.Transport;

/// <summary>Plain JSON channel to the local API of one JDownloader device; see <see cref="IDeviceChannel"/>.</summary>
/// <remarks>No exception raised here carries the path, the URL, the response body or the arguments.</remarks>
internal sealed class LocalDeviceChannel : IDeviceChannel, IBinaryDeviceChannel
{
    private const int MaxResponseDepth = 64;
    private const int MaxErrorBodyLength = 1_048_576;
    private static readonly string _unreadableMessage = RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);

    private readonly HttpClient _httpClient;
    private readonly LocalEndpoint _endpoint;

    public LocalDeviceChannel(HttpClient httpClient, LocalEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(endpoint);
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    public Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        ArgumentNullException.ThrowIfNull(responseType);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, (_, body) => ReadResponse(body, responseType), cancellationToken);
    }

    public Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, (_, body) => ReadResponse<object>(body, null), cancellationToken);
    }

    public Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, ReadPng, cancellationToken);
    }

    private static string[] SnapshotArgs(IReadOnlyList<JsonElement> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count > DeviceCallGuard.MaxRequestArgsCount)
        {
            throw new ArgumentException("The number of arguments must not exceed 1024.", nameof(args));
        }

        var snapshot = new string[args.Count];
        long total = 0;
        try
        {
            for (var i = 0; i < snapshot.Length; i++)
            {
                var element = args[i];
                if (element.ValueKind == JsonValueKind.Undefined)
                {
                    throw new ArgumentException("An argument is undefined.", nameof(args));
                }

                // Checked before allocating: the exact length of the raw text, plus one separator.
                if (total + RequestArgsLimit.RawTextLength(element) + 1 > RequestArgsLimit.MaxLength)
                {
                    throw new ArgumentException(RequestArgsLimit.TotalMessage, nameof(args));
                }

                snapshot[i] = element.GetRawText();
                total += snapshot[i].Length + 1L;
            }
        }
        catch (InvalidOperationException)
        {
            // Includes ObjectDisposedException: the document of an argument was disposed, or its text is not valid.
            throw new ArgumentException("An argument is not valid.", nameof(args));
        }

        return snapshot;
    }

    private static void WriteBody(PooledBufferWriter buffer, string[] args)
    {
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteStartArray("params");
        foreach (var arg in args)
        {
            writer.WriteRawValue(arg, skipInputValidation: true);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static MyJdProtocolException StatusOnly(HttpStatusCode status) =>
        new(string.Create(CultureInfo.InvariantCulture, $"The JDownloader local API answered HTTP {(int)status} without a readable error."), MyJdErrorSource.Device);

    private static byte[] ReadPng(HttpResponseMessage response, byte[] body)
    {
        if (!RelayTransport.IsPng(response, body))
        {
            throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
        }

        return body;
    }

    private static T? ReadResponse<T>(byte[] body, JsonTypeInfo<T>? responseType)
    {
        try
        {
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = MaxResponseDepth, AllowDuplicateProperties = false });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
            }

            if (responseType is null || !root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
            {
                return default;
            }

            return data.Deserialize(responseType);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
        }
    }

    private async Task<T> ExchangeAsync<T>(string path, string[] args, Func<HttpResponseMessage, byte[], T> convert, CancellationToken cancellationToken)
    {
        // HttpClient.Timeout stops applying once the headers are read: bound the whole exchange, body included.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_httpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            linked.CancelAfter(_httpClient.Timeout);
        }

        try
        {
            return await ExchangeCoreAsync(path, args, convert, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The JDownloader local API did not answer in time.");
        }
    }

    private async Task<T> ExchangeCoreAsync<T>(string path, string[] args, Func<HttpResponseMessage, byte[], T> convert, CancellationToken cancellationToken)
    {
        // The buffer is cleared on dispose: keep it alive until the exchange is over.
        using var buffer = new PooledBufferWriter();
        WriteBody(buffer, args);
        using var content = new ReadOnlyMemoryContent(buffer.WrittenMemory);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint.BuildUri(path)) { Content = content };
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!_endpoint.IsSameOrigin(response.RequestMessage?.RequestUri))
        {
            throw StatusOnly(response.StatusCode);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var body = await RelayTransport.ReadBodyAsync(response.Content, _endpoint.MaxResponseBodyLength, MyJdErrorSource.Device, cancellationToken).ConfigureAwait(false);
        return convert(response, body);
    }

    private async Task<MyJdException> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await RelayTransport.ReadBodyAsync(response.Content, Math.Min(_endpoint.MaxResponseBodyLength, MaxErrorBodyLength), MyJdErrorSource.Device, cancellationToken).ConfigureAwait(false);
            var error = JsonSerializer.Deserialize(body, MyJdJsonContext.Default.ErrorResponse);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Type))
            {
                // The src claim of the body is ignored: this channel only talks to the device.
                return MyJdErrorMapper.Map(MyJdErrorSource.Device, "DEVICE", error.Type, response.StatusCode, null);
            }
        }
        catch (Exception ex) when (ex is MyJdProtocolException or IOException or JsonException)
        {
            // Not a readable error body: the status alone is reported below.
        }

        return StatusOnly(response.StatusCode);
    }
}
