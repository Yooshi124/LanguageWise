using System.Net;
using System.Text;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

/// <summary>
/// A stand-in for the network so the tests never need a running database microservice.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory;

    internal StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        : this((request, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            RequestMessage = request
        }))
    {
    }

    internal StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    {
        this.responseFactory = responseFactory;
    }

    public Uri? LastRequestUri { get; private set; }
    public HttpMethod? LastRequestMethod { get; private set; }
    public string? LastRequestBody { get; private set; }
    public string? LastAuthorizationScheme { get; private set; }
    public string? LastAuthorizationParameter { get; private set; }
    public IReadOnlyList<string> LastAcceptMediaTypes { get; private set; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastRequestMethod = request.Method;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
        LastAuthorizationParameter = request.Headers.Authorization?.Parameter;
        LastAcceptMediaTypes = request.Headers.Accept
            .Select(header => header.MediaType ?? string.Empty)
            .ToArray();

        return await responseFactory(request, cancellationToken);
    }
}
