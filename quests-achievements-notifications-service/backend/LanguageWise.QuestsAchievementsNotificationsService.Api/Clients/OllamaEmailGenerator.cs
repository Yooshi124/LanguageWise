using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public interface IEmailContentGenerator
{
    Task<EmailContent> GenerateAsync(EmailContext context, CancellationToken cancellationToken = default);
}

public sealed class OllamaEmailGenerator(
    HttpClient httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaEmailGenerator> logger) : IEmailContentGenerator
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
            var request = new
            {
                model = options.Value.Model,
                stream = false,
                think = false,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Write one warm, concise LanguageWise notification for the event. Mention every affected achievement and highlight any marked as newly attained; otherwise summarize progress toward the listed tiers. Return only JSON matching the supplied schema. Do not include markdown, links, or claims not present in the event."
                    },
                    new
                    {
                        role = "user",
                        content = $"Trigger: {context.Trigger}\nSubject: {context.SubjectId}\nAchievements:\n{FormatAchievements(context.Achievements)}"
                    }
                },
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        subject = new { type = "string" },
                        body = new { type = "string" }
                    },
                    required = new[] { "subject", "body" }
                },
                options = new
                {
                    temperature = 1.0,
                    top_p = 0.95,
                    top_k = 64,
                    num_predict = 192
                }
            };

            using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            var generated = JsonSerializer.Deserialize<GeneratedEmail>(payload?.Message.Content ?? string.Empty, JsonOptions);

            if (string.IsNullOrWhiteSpace(generated?.Subject) || string.IsNullOrWhiteSpace(generated.Body))
            {
                throw new JsonException("Ollama returned an incomplete email.");
            }

            return new EmailContent(
                Truncate(generated.Subject.ReplaceLineEndings(" ").Trim(), MaximumSubjectLength),
                Truncate(generated.Body.Trim(), MaximumBodyLength),
                false);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Ollama email generation failed; using the fallback template.");
            return Fallback(context);
        }
    }

    private static EmailContent Fallback(EmailContext context)
    {
        var attained = context.Achievements.Where(item => item.NewlyAttained).ToList();
        var subject = attained.Count > 0
            ? $"Achievement unlocked: {string.Join(", ", attained.Select(item => item.Name))}"
            : "Your LanguageWise progress";
        var body = attained.Count > 0
            ? $"You unlocked {string.Join(", ", attained.Select(item => item.Name))}. Congratulations! "
            : "You made progress. ";
        body += string.Join(" ", context.Achievements.Select(item =>
            $"{item.Name}: {item.Progress} of {item.ProgressNeeded}."));

        return new EmailContent(
            Truncate(subject, MaximumSubjectLength),
            Truncate(body, MaximumBodyLength),
            true);
    }

    private static string FormatAchievements(IEnumerable<AchievementUpdate> achievements) =>
        string.Join("\n", achievements.Select(item =>
            $"- {item.Name}: {item.Progress}/{item.ProgressNeeded}; newly attained: {item.NewlyAttained}"));

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record OllamaChatResponse(OllamaMessage Message);
    private sealed record OllamaMessage(string Content);
    private sealed record GeneratedEmail(string Subject, string Body);
}