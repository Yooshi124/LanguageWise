using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Options;
using Microsoft.Extensions.Options;

namespace LanguageWise.MiniGamesService.Api.Clients;

/// <summary>Generates themed vocabulary word lists (with definitions) via the AI provider.</summary>
public interface IVocabularyCompletionClient
{
    /// <summary>Raw text of the model's completion for the given prompt messages.</summary>
    Task<string> CompleteAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        int maxTokens,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// OpenRouter chat-completions client, modelled on the quizzes-courses assistant client but
/// non-streaming: vocabulary generation needs the full JSON payload in one shot.
/// </summary>
public sealed class OpenRouterVocabularyClient(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options) : IVocabularyCompletionClient
{
    private const int MaximumRateLimitRetries = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenRouterOptions options = options.Value;

    public async Task<string> CompleteAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        int maxTokens,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(new OpenRouterChatRequest(
                    options.Model,
                    messages,
                    Stream: false,
                    maxTokens,
                    temperature))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests &&
                attempt < MaximumRateLimitRetries)
            {
                var retryDelay = response.Headers.RetryAfter?.Delta ??
                    TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                await Task.Delay(
                    retryDelay > TimeSpan.FromSeconds(8)
                        ? TimeSpan.FromSeconds(8)
                        : retryDelay,
                    cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new VocabularyProviderException(
                    "The vocabulary provider rejected the request.",
                    response.StatusCode);
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(JsonOptions, cancellationToken);
            var content = payload?.Choices.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new VocabularyProviderException("The vocabulary provider returned an empty completion.");
            }

            return content;
        }
    }

    private sealed record OpenRouterChatResponse(IReadOnlyList<OpenRouterChoice> Choices);

    private sealed record OpenRouterChoice(OpenRouterResponseMessage Message);

    private sealed record OpenRouterResponseMessage(string Content);
}

public sealed class VocabularyProviderException(string message, System.Net.HttpStatusCode? statusCode = null)
    : Exception(message)
{
    public System.Net.HttpStatusCode? StatusCode { get; } = statusCode;
}
