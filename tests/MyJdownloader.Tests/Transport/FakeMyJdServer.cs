namespace MyJdownloader.Tests.Transport;

using System.Net;
using System.Text;
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
