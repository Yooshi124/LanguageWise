using System.Net;
using System.Net.Http.Json;
using LanguageWise.MiniGamesService.Api.Clients;

namespace LanguageWise.MiniGamesService.Api.Tests;

public sealed class RagClientTests
{
    [Test]
    public async Task QueryAsync_SendsQueryAndParsesResults()
    {
        var handler = new RecordingHandler();
        var client = new RagClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://rag/")
        });

        var response = await client.QueryAsync("how does the leaderboard work", nResults: 3);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Path, Is.EqualTo("/query"));
            Assert.That(handler.Body, Does.Contain("\"query\":\"how does the leaderboard work\""));
            Assert.That(handler.Body, Does.Contain("\"n_results\":3"));
            Assert.That(response.ResultCount, Is.EqualTo(1));
            Assert.That(response.Results, Has.Count.EqualTo(1));
            Assert.That(response.Results[0].Source, Is.EqualTo("leaderboard-analytics-service"));
            Assert.That(response.Results[0].Heading, Is.EqualTo("For users"));
            Assert.That(response.Results[0].Relevance, Is.EqualTo(0.565));
            Assert.That(response.Results[0].Text, Is.EqualTo("Global analytics comparing you against other students."));
        });
    }

    [Test]
    public async Task QueryAsync_OmittedNResults_SendsNullNResults()
    {
        var handler = new RecordingHandler();
        var client = new RagClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://rag/")
        });

        await client.QueryAsync("mini games", nResults: null);

        Assert.That(handler.Body, Does.Contain("\"n_results\":null"));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    results = new[]
                    {
                        new
                        {
                            source = "leaderboard-analytics-service",
                            heading = "For users",
                            relevance = 0.565,
                            text = "Global analytics comparing you against other students."
                        }
                    },
                    resultCount = 1
                })
            };
        }
    }
}
