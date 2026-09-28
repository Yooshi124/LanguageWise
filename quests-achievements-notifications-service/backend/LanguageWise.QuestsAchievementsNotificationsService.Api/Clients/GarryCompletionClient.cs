using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public sealed class GarryCompletionClient(HttpClient client, IHttpContextAccessor contextAccessor) : IAssistantCompletionClient
{
    public async Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext ?? throw new InvalidOperationException("No assistant request context.");
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : context.Request.Cookies["token"];

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/completions")
        {
            Content = JsonContent.Create(new
            {
                message = messages[^1].Content,
                history = messages.Skip(2).SkipLast(1),
                domainRules = messages[0].Content,
                canonicalContext = messages[1].Content
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new AssistantProviderException("Garry could not start the assistant.", status);
        }

        try
        {
            return new GarryEventStream(response, await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}

public sealed class GarryEventStream(HttpResponseMessage response, Stream body) : IAssistantEventStream
{
    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(body);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line[6..]);
            }
            catch (JsonException)
            {
                throw new AssistantProviderStreamException();
            }
            using (document)
            {
                var root = document.RootElement;
                if (root.TryGetProperty("code", out _))
                {
                    throw new AssistantProviderStreamException();
                }
                if (root.TryGetProperty("content", out var content))
                {
                    yield return ProviderStreamEvent.Delta(content.GetString()!);
                }
                else if (root.TryGetProperty("reason", out var reason))
                {
                    yield return ProviderStreamEvent.Done(reason.GetString() ?? "stop");
                    yield break;
                }
            }
        }
        throw new AssistantProviderStreamException();
    }

    public ValueTask DisposeAsync()
    {
        body.Dispose();
        response.Dispose();
        return ValueTask.CompletedTask;
    }
}