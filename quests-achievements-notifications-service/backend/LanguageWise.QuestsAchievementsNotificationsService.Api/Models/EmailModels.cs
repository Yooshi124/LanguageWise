namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

public sealed record EmailContent(string Subject, string Body, bool UsedFallback);

public sealed record EmailContext(
    string Trigger,
    string SubjectId,
    IReadOnlyList<AchievementUpdate> Achievements);

public sealed class OllamaOptions
{
    public string Model { get; set; } = "gemma4:12b";
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "LanguageWise";
}