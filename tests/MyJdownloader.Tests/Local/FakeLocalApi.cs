namespace MyJdownloader.Tests.Local;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

/// <summary>A fake local JDownloader API: records each request with its body, then answers through a responder.</summary>
internal sealed class FakeLocalApi : HttpMessageHandler
{
    private readonly List<Sent> _requests = [];
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeLocalApi(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        ArgumentNullException.ThrowIfNull(responder);
        _responder = responder;
    }

    public IReadOnlyList<Sent> Requests => _requests;

    public static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => Raw(body, "application/json", status);

    public static HttpResponseMessage Raw(string body, string? mediaType, HttpStatusCode status = HttpStatusCode.OK)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = mediaType is null ? null : new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(status) { Content = content };
    }

    public static HttpResponseMessage Png(byte[] body, string? mediaType = "image/png", HttpStatusCode status = HttpStatusCode.OK)
    {
        ArgumentNullException.ThrowIfNull(body);
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = mediaType is null ? null : new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(status) { Content = content };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _requests.Add(new Sent(request.Method, request.RequestUri!.AbsoluteUri, request.Content?.Headers.ContentType?.MediaType, body));
        var response = _responder(request);
        response.RequestMessage ??= request;
        return response;
    }

    /// <summary>What the fake saw of one request.</summary>
    internal sealed record Sent(HttpMethod Method, string Uri, string? MediaType, string Body);
}
