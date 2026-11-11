namespace MyJdownloader.Transport;

using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using MyJdownloader.Crypto;

/// <summary>Encrypted channel to one device through the relay; see <see cref="IDeviceChannel"/>.</summary>
/// <remarks>No exception raised here carries the path, the device id, the session token or the arguments.</remarks>
internal sealed partial class DeviceChannel : IDeviceChannel, IBinaryDeviceChannel
{
    private const int MaxPathLength = 256;
    private const int MaxDeviceIdLength = 256;
    private const int MaxResponseDepth = 64;
    private const int MaxRequestArgsCount = 1_024;
    private static readonly string _unreadableMessage = RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device);
    private static readonly string _mismatchMessage = RelayTransport.MismatchMessageFor(MyJdErrorSource.Device);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly MyJDownloaderClient _client;
    private readonly RelayTransport _transport;
    private readonly MyJDownloaderOptions _options;
    private readonly RequestIdGenerator _requestIds;
    private readonly string _escapedDeviceId;

    public DeviceChannel(MyJDownloaderClient client, RelayTransport transport, MyJDownloaderOptions options, RequestIdGenerator requestIds, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requestIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        if (deviceId.Length > MaxDeviceIdLength || deviceId.Any(char.IsControl) || !IsValidUtf16(deviceId))
        {
            throw new ArgumentException("The device id is not valid.", nameof(deviceId));
        }

        _client = client;
        _transport = transport;
        _options = options;
        _requestIds = requestIds;
        _escapedDeviceId = Uri.EscapeDataString(deviceId);
    }

    public Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        ArgumentNullException.ThrowIfNull(responseType);
        var snapshot = SnapshotArgs(args);
        return CallCoreAsync(path, snapshot, responseType, cancellationToken);
    }

    public Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return CallCoreAsync<object>(path, snapshot, null, cancellationToken);
    }

    public Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        var snapshot = SnapshotArgs(args);
        return _client.ExecuteAsync((session, ct) => SendBinaryAsync(session, path, snapshot, ct), cancellationToken);
    }

    [GeneratedRegex(@"\A(?:/[A-Za-z0-9_]+)+\z", RegexOptions.CultureInvariant)]
    private static partial Regex PathPattern();

    private static bool IsValidUtf16(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length > MaxPathLength || !PathPattern().IsMatch(path))
        {
            throw new ArgumentException("The path is not valid.", nameof(path));
        }
    }

    private static string[] SnapshotArgs(IReadOnlyList<JsonElement> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count > MaxRequestArgsCount)
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

                // Checked before allocating: exact length for raw text, a conservative lower bound for strings (escapes are not counted).
                var minLength = element.ValueKind == JsonValueKind.String ? RequestArgsLimit.MinLength(element) : RequestArgsLimit.RawTextLength(element);
                if (total + minLength + 3 > RequestArgsLimit.MaxLength)
                {
                    throw new ArgumentException(RequestArgsLimit.TotalMessage, nameof(args));
                }

                snapshot[i] = element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();

                // Three characters of JSON overhead per argument: two quotes and a separator.
                total += snapshot[i].Length + 3L;
                if (total > RequestArgsLimit.MaxLength)
                {
                    throw new ArgumentException(RequestArgsLimit.TotalMessage, nameof(args));
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Includes ObjectDisposedException: the document of an argument was disposed, or its text is not valid.
            throw new ArgumentException("An argument is not valid.", nameof(args));
        }

        foreach (var value in snapshot)
        {
            if (!IsValidUtf16(value))
            {
                throw new ArgumentException("An argument is not valid UTF-16 text.", nameof(args));
            }
        }

        return snapshot;
    }

    // Writes the JSON body into a pooled buffer, encrypts it, then clears every buffer that held the plaintext.
    private static string EncryptBody(byte[] key, string path, string[] args, long rid, int apiVersion)
    {
        using var buffer = new PooledBufferWriter();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("url", path);
            writer.WriteStartArray("params");
            foreach (var arg in args)
            {
                writer.WriteStringValue(arg);
            }

            writer.WriteEndArray();
            writer.WriteNumber("rid", rid);
            writer.WriteNumber("apiVer", apiVersion);
            writer.WriteEndObject();
        }

        return MyJdCrypto.Encrypt(key, buffer.WrittenSpan);
    }

    private Task<T?> CallCoreAsync<T>(string path, string[] args, JsonTypeInfo<T>? responseType, CancellationToken cancellationToken) =>
        _client.ExecuteAsync((session, ct) => SendAsync(session, path, args, responseType, ct), cancellationToken);

    private async Task<T?> SendAsync<T>(MyJdSession session, string path, string[] args, JsonTypeInfo<T>? responseType, CancellationToken cancellationToken)
    {
        var rid = _requestIds.Next();
        var json = await ExchangeAsync(
            session,
            path,
            args,
            rid,
            (request, key) => _transport.ExchangeAsync(request, key, MyJdErrorSource.Device, cancellationToken)).ConfigureAwait(false);
        return ReadResponse(json, rid, responseType);
    }

    private Task<byte[]> SendBinaryAsync(MyJdSession session, string path, string[] args, CancellationToken cancellationToken) =>
        ExchangeAsync(
            session,
            path,
            args,
            _requestIds.Next(),
            (request, key) => _transport.ExchangeBinaryAsync(request, key, MyJdErrorSource.Device, cancellationToken));

    // Builds the encrypted request, runs the exchange and clears the key copy whatever the outcome.
    private async Task<TResult> ExchangeAsync<TResult>(MyJdSession session, string path, string[] args, long rid, Func<HttpRequestMessage, byte[], Task<TResult>> exchange)
    {
        var url = _options.BaseAddress.AbsoluteUri.TrimEnd('/') + "/t_" + session.SessionToken + "_" + _escapedDeviceId + path;
        var key = session.DeviceEncryptionToken.ToArray();
        try
        {
            using var content = new StringContent(EncryptBody(key, path, args, rid, _options.ApiVersion), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/aesjson-jd") { CharSet = "utf-8" };
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute)) { Content = content };
            return await exchange(request, key).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static T? ReadResponse<T>(string json, long rid, JsonTypeInfo<T>? responseType)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = MaxResponseDepth, AllowDuplicateProperties = false });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new MyJdProtocolException(_unreadableMessage, MyJdErrorSource.Device);
            }

            if (!root.TryGetProperty("rid", out var ridElement)
                || ridElement.ValueKind != JsonValueKind.Number
                || !ridElement.TryGetInt64(out var actual)
                || actual != rid)
            {
                throw new MyJdProtocolException(_mismatchMessage, MyJdErrorSource.Device);
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

    /// <summary>Growable pooled buffer that clears every array it held when it grows or is disposed.</summary>
    private sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        // Holds the plaintext request, which may carry passwords passed as call arguments.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte[] _buffer = ArrayPool<byte>.Shared.Rent(512);
        private int _written;

        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        public void Advance(int count) => _written += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _buffer.AsSpan(_written);
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_buffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = [];
        }

        private void Ensure(int sizeHint)
        {
            var needed = Math.Max(sizeHint, 1);
            if (_buffer.Length - _written >= needed)
            {
                return;
            }

            var bigger = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _written + needed));
            _buffer.AsSpan(0, _written).CopyTo(bigger);
            CryptographicOperations.ZeroMemory(_buffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = bigger;
        }
    }
}
