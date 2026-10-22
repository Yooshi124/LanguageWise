using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Options;
using Microsoft.Extensions.Options;

namespace LanguageWise.MiniGamesService.Api.Clients;

/// <summary>Generates themed vocabulary word lists (with definitions) via the AI provider.</summary>
public interface IVocabularyCompletionClient
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    /// <summary>Raw text of the model's completion for the given prompt messages.</summary>
    Task<string> CompleteAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        int maxTokens,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}

public sealed class GarryVocabularyClient(HttpClient client, IHttpContextAccessor contextAccessor) : IVocabularyCompletionClient
{
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var request = CreateRequest(HttpMethod.Get, "api/availability");
            using var response = await client.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) return false;
            var status = await response.Content.ReadFromJsonAsync<GarryAvailability>(timeout.Token);
            return status?.AiAvailable == true;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<string> CompleteAsync(IReadOnlyList<OpenRouterChatMessage> messages, int maxTokens,
        double? temperature = null, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/completions");
        request.Content = JsonContent.Create(new
        {
            message = messages[^1].Content,
            history = messages.Skip(1).SkipLast(1),
            domainRules = messages[0].Content,
            canonicalContext = "{}",
            maxTokens,
            temperature
        });
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            throw new VocabularyProviderException("Garry could not generate vocabulary.", status);
        }
        await using var stream = new AssistantCompletionStream(response, await response.Content.ReadAsStreamAsync(cancellationToken), true);
        var result = new StringBuilder();
        try
        {
            await foreach (var entry in stream.ReadEventsAsync(cancellationToken))
            {
                if (entry.Type == "delta") result.Append(entry.Content);
                else if (entry.Type != "done" || entry.Reason != "stop")
                    throw new VocabularyProviderException("Garry could not finish generating vocabulary.");
            }
        }
        catch (AssistantProviderStreamException)
        {
            throw new VocabularyProviderException("Garry returned incomplete vocabulary.");
        }
        if (string.IsNullOrWhiteSpace(result.ToString())) throw new VocabularyProviderException("Garry returned empty vocabulary.");
        return result.ToString();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var context = contextAccessor.HttpContext ?? throw new InvalidOperationException("No vocabulary request context.");
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserTokenReader.Read(context.Request));
        return request;
    }

    private sealed record GarryAvailability(bool AiAvailable);
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
