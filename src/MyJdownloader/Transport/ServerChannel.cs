namespace MyJdownloader.Transport;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Crypto;
using MyJdownloader.Errors;
using MyJdownloader.Json;

/// <summary>
/// Stateless channel performing the signed calls to the MyJD server.
/// </summary>
/// <remarks>
/// The protocol puts the email and the session token in the query string. .NET 9+ redacts query strings in
/// <see cref="HttpClient"/> logs and traces: <c>DisableUriRedaction</c> must never be enabled.
/// No exception raised here carries the request, the response or the URI.
/// Propagated unchanged: <see cref="HttpRequestException"/>, <see cref="IOException"/> (body read failure on a
/// successful response), <see cref="OperationCanceledException"/> (caller cancellation) and
/// <see cref="TimeoutException"/> (no complete answer within <see cref="HttpClient.Timeout"/>).
/// </remarks>
internal sealed class ServerChannel
{
    private const int MaxBodyLength = 1_048_576;
    private const int MaxEmailLength = 254;
    private const int MaxDeviceIdLength = 256;
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromMinutes(10);
    private const string UnreadableMessage = "The MyJD server returned a response that could not be read.";
    private const string MismatchMessage = "The MyJD server response does not match the request.";

    private readonly HttpClient httpClient;
    private readonly MyJDownloaderOptions options;
    private readonly RequestIdGenerator requestIds;
    private readonly TimeProvider timeProvider;

    public ServerChannel(HttpClient httpClient, MyJDownloaderOptions options, RequestIdGenerator requestIds, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requestIds);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.httpClient = httpClient;
        this.options = options;
        this.requestIds = requestIds;
        this.timeProvider = timeProvider;
    }

    /// <summary>Validates the credentials and derives the secrets of a <c>/my/connect</c> call.</summary>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password; never kept.</param>
    /// <returns>A single-use request to pass to <see cref="ConnectAsync"/>.</returns>
    /// <exception cref="ArgumentException">The email or the password is not valid; no request is sent.</exception>
    [SuppressMessage("Performance", "CA1822", Justification = "Instance member by contract: the client reaches it through its channel")]
    public ConnectRequest CreateConnectRequest(string email, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrEmpty(password);
        if (email.Length > MaxEmailLength || email.Any(char.IsControl))
        {
            throw new ArgumentException("The e-mail address is not valid.", nameof(email));
        }

        var loginSecret = MyJdCrypto.DeriveLoginSecret(email, password);
        byte[] deviceSecret;
        try
        {
            deviceSecret = MyJdCrypto.DeriveDeviceSecret(email, password);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(loginSecret);
            throw;
        }

        try
        {
            return new ConnectRequest(email, loginSecret, deviceSecret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(loginSecret);
            CryptographicOperations.ZeroMemory(deviceSecret);
        }
    }

    /// <summary>Opens a session with <c>/my/connect</c>.</summary>
    /// <param name="request">The single-use request; its secrets are cleared once the call ends.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new session.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The request has already been used.</exception>
    public async Task<MyJdSession> ConnectAsync(ConnectRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.MarkUsed();
        try
        {
            var response = await this.SendAsync(
                "/my/connect",
                [("email", request.Email), ("appkey", this.options.AppKey)],
                request.LoginSecret,
                MyJdJsonContext.Default.ConnectResponse,
                cancellationToken).ConfigureAwait(false);

            return BuildSession(request.Email, request.LoginSecret, request.DeviceSecret, request.LoginSecret, response);
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>Renews the session with <c>/my/reconnect</c>.</summary>
    /// <param name="session">The session to renew; left unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A new session with fresh tokens.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public async Task<MyJdSession> ReconnectAsync(MyJdSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var key = session.ServerEncryptionToken.ToArray();
        var deviceSecret = session.DeviceSecret.ToArray();
        var loginSecret = session.LoginSecret.ToArray();
        try
        {
            var response = await this.SendAsync(
                "/my/reconnect",
                [("sessiontoken", session.SessionToken), ("regaintoken", session.RegainToken)],
                key,
                MyJdJsonContext.Default.ConnectResponse,
                cancellationToken).ConfigureAwait(false);

            return BuildSession(session.Email, loginSecret, deviceSecret, key, response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(deviceSecret);
            CryptographicOperations.ZeroMemory(loginSecret);
        }
    }

    /// <summary>Closes the session with <c>/my/disconnect</c>.</summary>
    /// <param name="session">The session to close.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the server has acknowledged the request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public async Task DisconnectAsync(MyJdSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var key = session.ServerEncryptionToken.ToArray();
        try
        {
            await this.SendAsync(
                "/my/disconnect",
                [("sessiontoken", session.SessionToken)],
                key,
                MyJdJsonContext.Default.RidResponse,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>Lists the devices of the account with <c>/my/listdevices</c>.</summary>
    /// <param name="session">The session to use.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The devices registered on the account.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public async Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(MyJdSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var key = session.ServerEncryptionToken.ToArray();
        ListDevicesResponse response;
        try
        {
            response = await this.SendAsync(
                "/my/listdevices",
                [("sessiontoken", session.SessionToken)],
                key,
                MyJdJsonContext.Default.ListDevicesResponse,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        if (response.List is null)
        {
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        var devices = new List<DeviceInfo>(response.List.Length);
        foreach (var device in response.List)
        {
            if (device?.Id is null || device.Name is null || device.Type is null
                || device.Id.Length > MaxDeviceIdLength || device.Id.Any(char.IsControl))
            {
                throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
            }

            devices.Add(new DeviceInfo { Id = device.Id, Name = device.Name, Type = device.Type });
        }

        return devices;
    }

    private static string BuildQuery(string path, IReadOnlyList<(string Name, string Value)> parameters, long rid)
    {
        var builder = new StringBuilder(path);
        var separator = '?';
        foreach (var (name, value) in parameters)
        {
            builder.Append(separator).Append(name).Append('=').Append(Uri.EscapeDataString(value));
            separator = '&';
        }

        return builder.Append(separator).Append("rid=").Append(rid).ToString();
    }

    private static async Task<byte[]> ReadBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > MaxBodyLength)
        {
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var buffer = new ArrayBufferWriter<byte>();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.WrittenCount + read > MaxBodyLength)
                {
                    throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
                }

                buffer.Write(chunk.AsSpan(0, read));
            }

            return buffer.WrittenSpan.ToArray();
        }
    }

    private static async Task<ErrorResponse?> ReadErrorAsync(HttpContent content, byte[] key, CancellationToken cancellationToken)
    {
        byte[] body;
        try
        {
            body = await ReadBodyAsync(content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is MyJdProtocolException or IOException)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(body);
        try
        {
            return JsonSerializer.Deserialize(text, MyJdJsonContext.Default.ErrorResponse);
        }
        catch (JsonException)
        {
            // Not a plain error body: try the encrypted form below.
        }

        try
        {
            return JsonSerializer.Deserialize(MyJdCrypto.Decrypt(key, text), MyJdJsonContext.Default.ErrorResponse);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or CryptographicException)
        {
            return null;
        }
    }

    private static bool RidMatches(string json, long rid)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        var depth = 0;
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                depth++;
            }
            else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
            {
                depth--;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName && depth == 1 && reader.ValueTextEquals("rid"u8))
            {
                return reader.Read() && reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value) && value == rid;
            }
        }

        return false;
    }

    // Derives the per-session tokens from the new session token; the ctor copies its inputs, temporaries are zeroed here.
    private static MyJdSession BuildSession(string email, byte[] loginSecret, byte[] deviceSecret, byte[] baseKey, ConnectResponse response)
    {
        if (response.SessionToken is null || response.RegainToken is null)
        {
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        byte[]? serverToken = null;
        byte[]? deviceToken = null;
        try
        {
            serverToken = MyJdCrypto.DeriveToken(baseKey, response.SessionToken);
            deviceToken = MyJdCrypto.DeriveToken(deviceSecret, response.SessionToken);
            return new MyJdSession(email, loginSecret, deviceSecret, response.SessionToken, response.RegainToken, serverToken, deviceToken);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            // FormatException: non-hexadecimal token; ArgumentException: rejected by the MyJdSession constructor.
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }
        finally
        {
            if (serverToken is not null)
            {
                CryptographicOperations.ZeroMemory(serverToken);
            }

            if (deviceToken is not null)
            {
                CryptographicOperations.ZeroMemory(deviceToken);
            }
        }
    }

    private static TimeSpan ClampRetryAfter(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return delay > MaxRetryAfter ? MaxRetryAfter : delay;
    }

    private TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        var delay = header?.Delta ?? (header?.Date - this.timeProvider.GetUtcNow());
        if (delay is null)
        {
            return null;
        }

        return ClampRetryAfter(delay.Value);
    }

    // The single request pipeline: sign, send, bounded read, decrypt, rid check, typed deserialization.
    private async Task<T> SendAsync<T>(
        string path,
        IReadOnlyList<(string Name, string Value)> parameters,
        byte[] key,
        JsonTypeInfo<T> responseType,
        CancellationToken cancellationToken)
        where T : class
    {
        // HttpClient.Timeout stops applying once the headers are read: bound the whole exchange, body included.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (this.httpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            linked.CancelAfter(this.httpClient.Timeout);
        }

        try
        {
            return await this.SendCoreAsync(path, parameters, key, responseType, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The MyJD server did not answer in time.");
        }
    }

    private async Task<T> SendCoreAsync<T>(
        string path,
        IReadOnlyList<(string Name, string Value)> parameters,
        byte[] key,
        JsonTypeInfo<T> responseType,
        CancellationToken cancellationToken)
        where T : class
    {
        var rid = this.requestIds.Next();
        var signed = BuildQuery(path, parameters, rid);
        var url = this.options.BaseAddress.AbsoluteUri.TrimEnd('/') + signed + "&signature=" + MyJdCrypto.Sign(key, signed);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
        using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await ReadErrorAsync(response.Content, key, cancellationToken).ConfigureAwait(false);
            throw MyJdErrorMapper.Map(MyJdErrorSource.Server, error?.Src, error?.Type, response.StatusCode, this.ParseRetryAfter(response));
        }

        var body = await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false);

        string json;
        try
        {
            json = MyJdCrypto.Decrypt(key, Encoding.UTF8.GetString(body));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        bool ridMatches;
        T? result;
        try
        {
            ridMatches = RidMatches(json, rid);
            result = ridMatches ? JsonSerializer.Deserialize(json, responseType) : null;
        }
        catch (JsonException)
        {
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        if (!ridMatches)
        {
            throw new MyJdProtocolException(MismatchMessage, MyJdErrorSource.Server);
        }

        return result ?? throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
    }
}
