namespace MyJdownloader.Transport;

using System.Buffers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Crypto;
using MyJdownloader.Json;

/// <summary>
/// Stateless channel performing the signed calls to the MyJD server.
/// </summary>
/// <remarks>
/// The protocol puts the email and the session token in the query string. .NET 9+ redacts query strings in
/// <see cref="HttpClient"/> logs and traces: <c>DisableUriRedaction</c> must never be enabled.
/// No exception raised here carries the request, the response or the URI.
/// </remarks>
internal sealed class ServerChannel
{
    private const int MaxBodyLength = 1_048_576;
    private const int MaxEmailLength = 254;
    private const string UnreadableMessage = "The MyJD server returned a response that could not be read.";
    private const string MismatchMessage = "The MyJD server response does not match the request.";

    private readonly HttpClient httpClient;
    private readonly MyJDownloaderOptions options;
    private readonly RequestIdGenerator requestIds;

    public ServerChannel(HttpClient httpClient, MyJDownloaderOptions options, RequestIdGenerator requestIds, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requestIds);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.httpClient = httpClient;
        this.options = options;
        this.requestIds = requestIds;
    }

    /// <summary>Opens a session with <c>/my/connect</c>.</summary>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password; never kept.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new session.</returns>
    /// <exception cref="ArgumentException">The email or the password is not valid; no request is sent.</exception>
    public Task<MyJdSession> ConnectAsync(string email, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrEmpty(password);
        if (email.Length > MaxEmailLength || email.Any(char.IsControl))
        {
            throw new ArgumentException("The e-mail address is not valid.", nameof(email));
        }

        var loginSecret = MyJdCrypto.DeriveLoginSecret(email, password);
        var deviceSecret = MyJdCrypto.DeriveDeviceSecret(email, password);
        return this.ConnectCoreAsync(email, loginSecret, deviceSecret, cancellationToken);
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

    private async Task<MyJdSession> ConnectCoreAsync(string email, byte[] loginSecret, byte[] deviceSecret, CancellationToken cancellationToken)
    {
        byte[]? serverToken = null;
        byte[]? deviceToken = null;
        try
        {
            var response = await this.SendAsync(
                "/my/connect",
                [("email", email), ("appkey", this.options.AppKey)],
                loginSecret,
                MyJdJsonContext.Default.ConnectResponse,
                cancellationToken).ConfigureAwait(false);

            try
            {
                if (response.SessionToken is null || response.RegainToken is null)
                {
                    throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
                }

                serverToken = MyJdCrypto.DeriveToken(loginSecret, response.SessionToken);
                deviceToken = MyJdCrypto.DeriveToken(deviceSecret, response.SessionToken);
                return new MyJdSession(email, loginSecret, deviceSecret, response.SessionToken, response.RegainToken, serverToken, deviceToken);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(loginSecret);
            CryptographicOperations.ZeroMemory(deviceSecret);
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

    // The single request pipeline: sign, send, bounded read, decrypt, rid check, typed deserialization.
    private async Task<T> SendAsync<T>(
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
            throw new MyJdProtocolException(UnreadableMessage, MyJdErrorSource.Server);
        }

        var body = await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false);

        string json;
        try
        {
            json = MyJdCrypto.Decrypt(key, Encoding.UTF8.GetString(body));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
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
        catch (Exception ex) when (ex is JsonException or ArgumentException)
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
