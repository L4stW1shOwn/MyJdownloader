namespace MyJdownloader.Tests.Transport;

using System.Net;
using System.Text;
using System.Text.Json;
using MyJdownloader.Crypto;

internal sealed class FakeMyJdServer : HttpMessageHandler
{
    private const string SignatureMarker = "&signature=";
    private readonly List<HttpRequestMessage> _requests = [];
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeMyJdServer(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        ArgumentNullException.ThrowIfNull(responder);
        this._responder = responder;
    }

    public IReadOnlyList<HttpRequestMessage> Requests => this._requests;

    public static HttpResponseMessage EncryptedJson(byte[] key, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Raw(MyJdCrypto.Encrypt(key, json), status);
    }

    public static HttpResponseMessage Raw(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8) };

    /// <summary>A raw (unencrypted) binary answer; a <see langword="null"/> content type leaves the header unset.</summary>
    public static HttpResponseMessage Png(byte[] body, string? contentType = "image/png", HttpStatusCode status = HttpStatusCode.OK)
    {
        ArgumentNullException.ThrowIfNull(body);
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = contentType is null ? null : System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        return new HttpResponseMessage(status) { Content = content };
    }

    /// <summary>Returns the value of the <c>rid</c> query parameter, exactly as sent.</summary>
    public static long RidOf(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = request.RequestUri!.Query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            if (pair.StartsWith("rid=", StringComparison.Ordinal))
            {
                return long.Parse(pair["rid=".Length..], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException("No rid parameter in the request.");
    }

    /// <summary>Returns the request body decrypted with <paramref name="key"/>; call it while the request is still alive.</summary>
    public static string DecryptedBodyOf(HttpRequestMessage request, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(key);
        if (request.Content is null)
        {
            throw new InvalidOperationException("The request has no body.");
        }

        // The stream is cached by the content: keep it open and rewind so the body can be read again.
        var stream = request.Content.ReadAsStream();
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        return MyJdCrypto.Decrypt(key, reader.ReadToEnd());
    }

    /// <summary>Returns the <c>rid</c> member of the decrypted JSON request body.</summary>
    public static long BodyRidOf(HttpRequestMessage request, byte[] key)
    {
        using var document = JsonDocument.Parse(DecryptedBodyOf(request, key));
        return document.RootElement.GetProperty("rid").GetInt64();
    }

    /// <summary>
    /// Returns the signed string as sent: from the protocol path (base path removed) to the
    /// <c>rid</c> parameter included, with the original percent-encoding.
    /// </summary>
    public static string SignedStringOf(HttpRequestMessage request, string basePath = "")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(basePath);
        var pathAndQuery = request.RequestUri!.PathAndQuery;
        var end = pathAndQuery.LastIndexOf(SignatureMarker, StringComparison.Ordinal);
        var signed = pathAndQuery[..end];
        return signed[basePath.TrimEnd('/').Length..];
    }

    public static string SignatureOf(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pathAndQuery = request.RequestUri!.PathAndQuery;
        return pathAndQuery[(pathAndQuery.LastIndexOf(SignatureMarker, StringComparison.Ordinal) + SignatureMarker.Length)..];
    }

    public static bool HasValidSignature(HttpRequestMessage request, byte[] key, string basePath = "")
        => string.Equals(
            SignatureOf(request),
            MyJdCrypto.Sign(key, SignedStringOf(request, basePath)),
            StringComparison.Ordinal);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this._requests.Add(request);
        return Task.FromResult(this._responder(request));
    }
}
