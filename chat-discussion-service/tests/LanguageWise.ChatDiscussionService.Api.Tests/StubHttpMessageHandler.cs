using System.Net;
using System.Text;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

/// <summary>
/// A stand-in for the network so the tests never need a running database microservice.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }
    public HttpMethod? LastRequestMethod { get; private set; }
    public string? LastRequestBody { get; private set; }
    public string? LastAuthorization { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastRequestMethod = request.Method;
        LastAuthorization = request.Headers.Authorization?.ToString();
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            RequestMessage = request
        };
    }
}

/// <summary>Fails every call, so the endpoints' 503 handling can be exercised.</summary>
internal sealed class FailingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        throw new HttpRequestException("The database microservice is down.");
}
