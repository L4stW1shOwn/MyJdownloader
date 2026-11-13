namespace MyJdownloader.Transport;

using System.Buffers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyJdownloader.Crypto;
using MyJdownloader.Errors;
using MyJdownloader.Json;

/// <summary>
/// Sends one prepared request to the MyJD relay and returns the decrypted JSON answer: timeout, bounded read,
/// error mapping and decryption. Shared by the server and device channels.
/// </summary>
/// <remarks>
/// No exception raised here carries the request, the response or the URI.
/// Propagated unchanged: <see cref="HttpRequestException"/>, <see cref="IOException"/> (body read failure on a
/// successful response), <see cref="OperationCanceledException"/> (caller cancellation) and
/// <see cref="TimeoutException"/> (no complete answer within <see cref="HttpClient.Timeout"/>).
/// </remarks>
internal sealed class RelayTransport
{
    private const string ServerUnreadableMessage = "The MyJD server returned a response that could not be read.";
    private const string ServerMismatchMessage = "The MyJD server response does not match the request.";
    private const string DeviceUnreadableMessage = "The MyJD device returned a response that could not be read.";
    private const string DeviceMismatchMessage = "The MyJD device response does not match the request.";
    private const string ServerTooLargeMessage = "The MyJD server response exceeds the configured size limit.";
    private const string DeviceTooLargeMessage = "The MyJD device response exceeds the configured size limit.";
    // An error body is small and the status code already carries the information: a raised cap must not make a failing peer cost more.
    private const int MaxErrorBodyLength = 1_048_576;
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly TimeSpan _maxRetryAfter = TimeSpan.FromMinutes(10);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxResponseBodyLength;

    public RelayTransport(HttpClient httpClient, TimeProvider timeProvider, int maxResponseBodyLength)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _httpClient = httpClient;
        _timeProvider = timeProvider;
        _maxResponseBodyLength = maxResponseBodyLength;
    }

    internal static string UnreadableMessageFor(MyJdErrorSource source) =>
        source == MyJdErrorSource.Device ? DeviceUnreadableMessage : ServerUnreadableMessage;

    internal static string TooLargeMessageFor(MyJdErrorSource source) =>
        source == MyJdErrorSource.Device ? DeviceTooLargeMessage : ServerTooLargeMessage;

    internal static string MismatchMessageFor(MyJdErrorSource source) =>
        source == MyJdErrorSource.Device ? DeviceMismatchMessage : ServerMismatchMessage;

    /// <summary>Sends the request and returns the decrypted JSON body; the request stays owned by the caller.</summary>
    /// <param name="request">The prepared request.</param>
    /// <param name="key">The key used to decrypt the answer.</param>
    /// <param name="source">The party the call is addressed to, used to map errors.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The decrypted JSON answer.</returns>
    public Task<string> ExchangeAsync(HttpRequestMessage request, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken) =>
        ExchangeWithTimeoutAsync(request, key, source, DecryptBody, cancellationToken);

    /// <summary>
    /// Sends the request and returns the raw PNG body of a successful answer; the request stays owned by the caller.
    /// The body is neither encrypted nor tied to the request id: see ADR-011.
    /// </summary>
    /// <param name="request">The prepared request.</param>
    /// <param name="key">The key used to decrypt an error answer.</param>
    /// <param name="source">The party the call is addressed to, used to map errors.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The PNG bytes, signature included.</returns>
    public Task<byte[]> ExchangeBinaryAsync(HttpRequestMessage request, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken) =>
        ExchangeWithTimeoutAsync(request, key, source, ReadPng, cancellationToken);

    private static string DecryptBody(HttpResponseMessage response, byte[] body, byte[] key, MyJdErrorSource source)
    {
        try
        {
            return MyJdCrypto.Decrypt(key, Encoding.UTF8.GetString(body));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new MyJdProtocolException(UnreadableMessageFor(source), source);
        }
    }

    private static byte[] ReadPng(HttpResponseMessage response, byte[] body, byte[] key, MyJdErrorSource source)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "image/png", StringComparison.OrdinalIgnoreCase) || !body.AsSpan().StartsWith(_pngSignature))
        {
            throw new MyJdProtocolException(UnreadableMessageFor(source), source);
        }

        return body;
    }

    private async Task<T> ExchangeWithTimeoutAsync<T>(HttpRequestMessage request, byte[] key, MyJdErrorSource source, Func<HttpResponseMessage, byte[], byte[], MyJdErrorSource, T> convert, CancellationToken cancellationToken)
    {
        // HttpClient.Timeout stops applying once the headers are read: bound the whole exchange, body included.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_httpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            linked.CancelAfter(_httpClient.Timeout);
        }

        try
        {
            return await ExchangeCoreAsync(request, key, source, convert, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The MyJD server did not answer in time.");
        }
    }

    private static TimeSpan ClampRetryAfter(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return delay > _maxRetryAfter ? _maxRetryAfter : delay;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpContent content, int limit, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > limit)
        {
            throw new MyJdProtocolException(TooLargeMessageFor(source), source);
        }

        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var buffer = new ArrayBufferWriter<byte>();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (read > limit - buffer.WrittenCount)
                {
                    throw new MyJdProtocolException(TooLargeMessageFor(source), source);
                }

                buffer.Write(chunk.AsSpan(0, read));
            }

            return buffer.WrittenSpan.ToArray();
        }
    }

    // Decrypted is true when the body could only be read after decryption (its src claim is then not trustworthy for a device call).
    private async Task<(ErrorResponse? Error, bool Decrypted)> ReadErrorAsync(HttpContent content, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        byte[] body;
        try
        {
            body = await ReadBodyAsync(content, Math.Min(_maxResponseBodyLength, MaxErrorBodyLength), source, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is MyJdProtocolException or IOException)
        {
            return (null, false);
        }

        var text = Encoding.UTF8.GetString(body);
        try
        {
            return (JsonSerializer.Deserialize(text, MyJdJsonContext.Default.ErrorResponse), false);
        }
        catch (JsonException)
        {
            // Not a plain error body: try the encrypted form below.
        }

        try
        {
            return (JsonSerializer.Deserialize(MyJdCrypto.Decrypt(key, text), MyJdJsonContext.Default.ErrorResponse), true);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or CryptographicException)
        {
            return (null, false);
        }
    }

    private TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        var delay = header?.Delta ?? (header?.Date - _timeProvider.GetUtcNow());
        if (delay is null)
        {
            return null;
        }

        return ClampRetryAfter(delay.Value);
    }

    private async Task<T> ExchangeCoreAsync<T>(HttpRequestMessage request, byte[] key, MyJdErrorSource source, Func<HttpResponseMessage, byte[], byte[], MyJdErrorSource, T> convert, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var (error, decrypted) = await ReadErrorAsync(response.Content, key, source, cancellationToken).ConfigureAwait(false);

            // Only the relay can answer in plain text: an encrypted device error cannot claim to come from the server.
            var src = decrypted && source == MyJdErrorSource.Device ? "DEVICE" : error?.Src;
            throw MyJdErrorMapper.Map(source, src, error?.Type, response.StatusCode, ParseRetryAfter(response));
        }

        var body = await ReadBodyAsync(response.Content, _maxResponseBodyLength, source, cancellationToken).ConfigureAwait(false);
        return convert(response, body, key, source);
    }
}
