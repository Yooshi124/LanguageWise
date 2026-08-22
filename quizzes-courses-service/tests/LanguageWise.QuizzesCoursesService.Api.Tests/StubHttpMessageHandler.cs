using System.Net;
using System.Text;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

/// <summary>
/// A stand-in for the network so the tests never need a running database microservice.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            RequestMessage = request
        });
    }
}
