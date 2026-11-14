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
    /// <summary>The maximum nesting depth accepted in a JSON answer.</summary>
    private const int MaxResponseDepth = 64;
    /// <summary>The maximum size read from the body of an error answer, in bytes, whatever the configured limit.</summary>
    private const int MaxErrorBodyLength = 1_048_576;
    /// <summary>The message of the protocol exception raised for an answer that cannot be read.</summary>
    private static readonly string _unreadableMessage = RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);

    /// <summary>The client that sends the requests; owned by the caller.</summary>
    private readonly HttpClient _httpClient;
    /// <summary>The validated endpoint every request is sent to.</summary>
    private readonly LocalEndpoint _endpoint;

    /// <summary>Initializes a new instance of the <see cref="LocalDeviceChannel"/> class.</summary>
    /// <param name="httpClient">The client that sends the requests; it is not disposed by the channel, and its <see cref="HttpClient.BaseAddress"/> is ignored.</param>
    /// <param name="endpoint">The validated endpoint of the local API.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> or <paramref name="endpoint"/> is <see langword="null"/>.</exception>
    public LocalDeviceChannel(HttpClient httpClient, LocalEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(endpoint);
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The arguments are sent as raw JSON values in <c>{"params":[...]}</c>. The answer must be a JSON object with a <c>data</c> member;
    /// a <see langword="null"/> <c>data</c> gives the default value. Arguments are validated and copied synchronously, before the task is returned.
    /// </remarks>
    public Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        ArgumentNullException.ThrowIfNull(responseType);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, (_, body) => ReadResponse(body, responseType), cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>The answer must still be a JSON object with a <c>data</c> member (the device sends an empty string for an action without return value); its value is ignored.</remarks>
    public Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, (_, body) => ReadResponse<object>(body, null), cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>The answer must have the <c>image/png</c> media type and start with the PNG signature; its bytes are untrusted (ADR-011).</remarks>
    public Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        DeviceCallGuard.ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return ExchangeAsync(path, snapshot, ReadPng, cancellationToken);
    }

    /// <summary>Copies the arguments as their raw JSON text, so that later changes to the list or its documents do not affect the call.</summary>
    /// <param name="args">The call arguments.</param>
    /// <returns>The raw JSON text of each argument, in order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// There are more than 1 024 arguments, an argument is undefined or belongs to a disposed document,
    /// or the arguments exceed <see cref="RequestArgsLimit.MaxLength"/> characters in total.
    /// </exception>
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

    /// <summary>Writes the request body <c>{"params":[...]}</c>, each argument as its raw JSON value.</summary>
    /// <param name="buffer">The buffer that receives the UTF-8 body; it may hold passwords passed as arguments.</param>
    /// <param name="args">The raw JSON text of each argument, as returned by <see cref="SnapshotArgs"/>.</param>
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

    /// <summary>Creates the protocol exception of an answer that carries no readable device error; its message holds the status code only.</summary>
    /// <param name="status">The HTTP status of the answer.</param>
    /// <returns>The exception to throw.</returns>
    private static MyJdProtocolException StatusOnly(HttpStatusCode status) =>
        new(string.Create(CultureInfo.InvariantCulture, $"The JDownloader local API answered HTTP {(int)status} without a readable error."), MyJdErrorSource.Device);

    /// <summary>Checks that a successful answer is a PNG image and returns its bytes.</summary>
    /// <param name="response">The answer, for its media type.</param>
    /// <param name="body">The body already read.</param>
    /// <returns>The PNG bytes, signature included.</returns>
    /// <exception cref="MyJdProtocolException">The media type is not <c>image/png</c> or the body does not start with the PNG signature.</exception>
    private static byte[] ReadPng(HttpResponseMessage response, byte[] body)
    {
        if (!RelayTransport.IsPng(response, body))
        {
            throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
        }

        return body;
    }

    /// <summary>Reads the <c>{"data": ...}</c> envelope of a successful answer.</summary>
    /// <typeparam name="T">The type of the <c>data</c> member.</typeparam>
    /// <param name="body">The body already read.</param>
    /// <param name="responseType">The JSON metadata of <typeparamref name="T"/>, or <see langword="null"/> to ignore the value.</param>
    /// <returns>The deserialized <c>data</c>, or the default value when it is <see langword="null"/> or ignored.</returns>
    /// <exception cref="MyJdProtocolException">
    /// The body is not a JSON object with a <c>data</c> member, is nested deeper than 64 levels, has duplicate properties,
    /// or its <c>data</c> does not match <typeparamref name="T"/>.
    /// </exception>
    private static T? ReadResponse<T>(byte[] body, JsonTypeInfo<T>? responseType)
    {
        try
        {
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = MaxResponseDepth, AllowDuplicateProperties = false });
            var root = document.RootElement;
            // The device always sends the data member, even for an action without return value (an empty string).
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data))
            {
                throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
            }

            if (responseType is null || data.ValueKind == JsonValueKind.Null)
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

    /// <summary>Runs one exchange, bounded as a whole (body included) by <see cref="HttpClient.Timeout"/>.</summary>
    /// <typeparam name="T">The type of the converted answer.</typeparam>
    /// <param name="path">The validated action path.</param>
    /// <param name="args">The raw JSON text of each argument.</param>
    /// <param name="convert">Converts the successful answer and its body.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The converted answer.</returns>
    /// <exception cref="TimeoutException">The device did not answer in time.</exception>
    /// <exception cref="OperationCanceledException">The call was cancelled through <paramref name="cancellationToken"/>.</exception>
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

    /// <summary>Sends the request and converts the answer: origin check first, then the device error or the bounded body.</summary>
    /// <typeparam name="T">The type of the converted answer.</typeparam>
    /// <param name="path">The validated action path.</param>
    /// <param name="args">The raw JSON text of each argument.</param>
    /// <param name="convert">Converts the successful answer and its body.</param>
    /// <param name="cancellationToken">A token to cancel the call, timeout included.</param>
    /// <returns>The converted answer.</returns>
    /// <exception cref="MyJdProtocolException">The answer comes from another origin, carries no readable error, exceeds the size limit or cannot be read.</exception>
    /// <exception cref="MyJdException">The device answered with an error.</exception>
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

    /// <summary>Maps an answer whose status is not 200 to an exception.</summary>
    /// <param name="response">The answer.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The exception of the device error when the body is JSON with a <c>type</c>; otherwise a protocol exception with the status code only.
    /// The body is never copied into the exception.
    /// </returns>
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
