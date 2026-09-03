using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api;

internal static class NotificationRules
{
    internal static int? GetUserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null;

    internal static bool IsValidEmail(string email) => MailAddress.TryCreate(email, out _);

    internal static Dictionary<string, string[]> ValidateEvent(EventRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Trigger is not ("lesson-completion" or "community-contribution" or "minigame-win" or "login-streak"))
        {
            errors["trigger"] = ["Trigger is not supported."];
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            errors["subject"] = ["Subject is required."];
        }

        if (request.RecipientUserId <= 0)
        {
            errors["recipientUserId"] = ["Recipient user ID must be positive."];
        }

        if (string.IsNullOrWhiteSpace(request.RecipientName))
        {
            errors["recipientName"] = ["Recipient name is required."];
        }

        if (request.Value < 0)
        {
            errors["value"] = ["Value cannot be negative."];
        }

        return errors;
    }

    internal static ProgressUpdate CalculateProgress(int oldProgress, int progressNeeded, int? value = null)
    {
        var target = value ?? (oldProgress < int.MaxValue ? oldProgress + 1 : int.MaxValue);
        var progress = progressNeeded < 0
            ? Math.Max(oldProgress, target)
            : Math.Max(oldProgress, Math.Min(target, progressNeeded));
        return new ProgressUpdate(
            progress,
            progressNeeded >= 0 && oldProgress < progressNeeded && progress >= progressNeeded);
    }

    internal static bool ShouldNotify(UserPreferences preferences, string trigger, bool newlyAttained)
    {
        if (!preferences.NotifyAll)
        {
            return false;
        }

        var eventEnabled = trigger switch
        {
            "community-contribution" => preferences.NotifyPostEngagement,
            "lesson-completion" => preferences.NotifyCourseCompletion,
            "minigame-win" => preferences.NotifyQuizResults,
            "login-streak" => preferences.NotifyStreaks,
            _ => false
        };

        return eventEnabled || (newlyAttained && preferences.NotifyAchievements);
    }
}

internal sealed record ProgressUpdate(int Progress, bool NewlyAttained);