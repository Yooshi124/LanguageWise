using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public interface IEmailContentGenerator
{
    Task<EmailContent> GenerateAsync(EmailContext context, CancellationToken cancellationToken = default);
}

public sealed class OpenRouterEmailGenerator(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterEmailGenerator> logger) : IEmailContentGenerator
{
    private const int MaximumSubjectLength = 120;
    private const int MaximumBodyLength = 4000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EmailContent> GenerateAsync(
        EmailContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = context.IsNotificationsWelcome
                ? "Write a warm, concise welcome email for a learner who just enabled all LanguageWise notifications. Address the learner by name and explain that they can receive updates about post engagement, course completion, quiz results, learning streaks, and achievements. Return only JSON matching the supplied schema. Do not include markdown, links, internal identifiers, trigger names, or claims not present in the event."
                : "Write one warm, concise LanguageWise notification for the event. Address the learner by name. Describe achievements using only their supplied English descriptions and highlight newly attained achievements; otherwise summarize progress. Never include achievement IDs, internal trigger names, or other identifiers. Return only JSON matching the supplied schema. Do not include markdown, links, or claims not present in the event.";
            var request = new
            {
                model = options.Value.Model,
                stream = false,
                max_tokens = 192,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = $"Learner name: {context.UserName}\nEvent description: {context.Subject}\nAchievement descriptions:\n{FormatAchievements(context.Achievements)}"
                    }
                },
                response_format = new
                {
                    type = "json_object"
                }
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
            }
            using var response = await httpClient.SendAsync(message, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(cancellationToken);
            var generated = JsonSerializer.Deserialize<GeneratedEmail>(
                payload?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty, JsonOptions);

            if (string.IsNullOrWhiteSpace(generated?.Subject) || string.IsNullOrWhiteSpace(generated.Body))
            {
                throw new JsonException("OpenRouter returned an incomplete email.");
            }

            return new EmailContent(
                Truncate(generated.Subject.ReplaceLineEndings(" ").Trim(), MaximumSubjectLength),
                Truncate(EnsureUserName(generated.Body.Trim(), context.UserName), MaximumBodyLength),
                false);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "OpenRouter email generation failed; using the fallback template.");
            return Fallback(context);
        }
    }

    private static EmailContent Fallback(EmailContext context)
    {
        if (context.IsNotificationsWelcome)
        {
            return new EmailContent(
                "Welcome to LanguageWise notifications",
            $"Hi {context.UserName}, you have enabled LanguageWise notifications. We will keep you updated about post engagement, course completions, quiz results, learning streaks, and achievements.",
                true);
        }

        var attained = context.Achievements.Where(item => item.NewlyAttained).ToList();
        var subject = attained.Count > 0
            ? "Achievement unlocked"
            : "Your LanguageWise progress";
        var body = attained.Count > 0
            ? $"Hi {context.UserName}, congratulations! You unlocked: {string.Join("; ", attained.Select(item => item.Description))}. "
            : $"Hi {context.UserName}, you made progress. ";
        body += string.Join(" ", context.Achievements.Select(item =>
            item.ProgressNeeded < 0
                ? $"{item.Description}: {item.Progress}."
                : $"{item.Description}: {item.Progress} of {item.ProgressNeeded}."));

        return new EmailContent(
            Truncate(subject, MaximumSubjectLength),
            Truncate(body, MaximumBodyLength),
            true);
    }

    private static string FormatAchievements(IEnumerable<AchievementUpdate> achievements) =>
        string.Join("\n", achievements.Select(item =>
            item.ProgressNeeded < 0
                ? $"- {item.Description}: progress {item.Progress}; newly attained: {item.NewlyAttained}"
                : $"- {item.Description}: progress {item.Progress} of {item.ProgressNeeded}; newly attained: {item.NewlyAttained}"));

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static string EnsureUserName(string body, string userName) =>
        body.Contains(userName, StringComparison.OrdinalIgnoreCase)
            ? body
            : $"Hi {userName}, {body}";

    private sealed record OpenRouterChatResponse(IReadOnlyList<OpenRouterChoice> Choices);
    private sealed record OpenRouterChoice(OpenRouterMessage Message);
    private sealed record OpenRouterMessage(string Content);
    private sealed record GeneratedEmail(string Subject, string Body);
}