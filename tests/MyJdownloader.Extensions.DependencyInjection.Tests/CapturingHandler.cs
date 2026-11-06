namespace MyJdownloader.Extensions.DependencyInjection.Tests;

using System.Net;

internal sealed class CapturingHandler : DelegatingHandler
{
    public List<Uri> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.Requests.Add(request.RequestUri!);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
