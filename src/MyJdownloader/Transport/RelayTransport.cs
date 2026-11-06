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
    private const int MaxBodyLength = 1_048_576;
    private const string ServerUnreadableMessage = "The MyJD server returned a response that could not be read.";
    private const string ServerMismatchMessage = "The MyJD server response does not match the request.";
    private const string DeviceUnreadableMessage = "The MyJD device returned a response that could not be read.";
    private const string DeviceMismatchMessage = "The MyJD device response does not match the request.";
    private static readonly TimeSpan _maxRetryAfter = TimeSpan.FromMinutes(10);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;

    public RelayTransport(HttpClient httpClient, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this._httpClient = httpClient;
        this._timeProvider = timeProvider;
    }

    internal static string UnreadableMessageFor(MyJdErrorSource source) =>
        source == MyJdErrorSource.Device ? DeviceUnreadableMessage : ServerUnreadableMessage;

    internal static string MismatchMessageFor(MyJdErrorSource source) =>
        source == MyJdErrorSource.Device ? DeviceMismatchMessage : ServerMismatchMessage;

    /// <summary>Sends the request and returns the decrypted JSON body; the request stays owned by the caller.</summary>
    /// <param name="request">The prepared request.</param>
    /// <param name="key">The key used to decrypt the answer.</param>
    /// <param name="source">The party the call is addressed to, used to map errors.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The decrypted JSON answer.</returns>
    public async Task<string> ExchangeAsync(HttpRequestMessage request, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        // HttpClient.Timeout stops applying once the headers are read: bound the whole exchange, body included.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (this._httpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            linked.CancelAfter(this._httpClient.Timeout);
        }

        try
        {
            return await this.ExchangeCoreAsync(request, key, source, linked.Token).ConfigureAwait(false);
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

    private static async Task<byte[]> ReadBodyAsync(HttpContent content, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > MaxBodyLength)
        {
            throw new MyJdProtocolException(UnreadableMessageFor(source), source);
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
                    throw new MyJdProtocolException(UnreadableMessageFor(source), source);
                }

                buffer.Write(chunk.AsSpan(0, read));
            }

            return buffer.WrittenSpan.ToArray();
        }
    }

    // Decrypted is true when the body could only be read after decryption (its src claim is then not trustworthy for a device call).
    private static async Task<(ErrorResponse? Error, bool Decrypted)> ReadErrorAsync(HttpContent content, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        byte[] body;
        try
        {
            body = await ReadBodyAsync(content, source, cancellationToken).ConfigureAwait(false);
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
        var delay = header?.Delta ?? (header?.Date - this._timeProvider.GetUtcNow());
        if (delay is null)
        {
            return null;
        }

        return ClampRetryAfter(delay.Value);
    }

    private async Task<string> ExchangeCoreAsync(HttpRequestMessage request, byte[] key, MyJdErrorSource source, CancellationToken cancellationToken)
    {
        using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var (error, decrypted) = await ReadErrorAsync(response.Content, key, source, cancellationToken).ConfigureAwait(false);

            // Only the relay can answer in plain text: an encrypted device error cannot claim to come from the server.
            var src = decrypted && source == MyJdErrorSource.Device ? "DEVICE" : error?.Src;
            throw MyJdErrorMapper.Map(source, src, error?.Type, response.StatusCode, this.ParseRetryAfter(response));
        }

        var body = await ReadBodyAsync(response.Content, source, cancellationToken).ConfigureAwait(false);
        try
        {
            return MyJdCrypto.Decrypt(key, Encoding.UTF8.GetString(body));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new MyJdProtocolException(UnreadableMessageFor(source), source);
        }
    }
}
