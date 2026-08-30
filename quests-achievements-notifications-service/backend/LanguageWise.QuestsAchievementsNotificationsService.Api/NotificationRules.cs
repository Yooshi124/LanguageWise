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

        if (request.Trigger is not ("post-engagement" or "course-completion" or "quiz-result" or "streak"))
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

        return errors;
    }

    internal static ProgressUpdate CalculateProgress(int oldProgress, int progressNeeded)
    {
        var progress = oldProgress >= progressNeeded ? progressNeeded : oldProgress + 1;
        return new ProgressUpdate(
            progress,
            oldProgress < progressNeeded && progress >= progressNeeded);
    }

    internal static bool ShouldNotify(UserPreferences preferences, string trigger, bool newlyAttained)
    {
        if (!preferences.NotifyAll)
        {
            return false;
        }

        var eventEnabled = trigger switch
        {
            "post-engagement" => preferences.NotifyPostEngagement,
            "course-completion" => preferences.NotifyCourseCompletion,
            "quiz-result" => preferences.NotifyQuizResults,
            "streak" => preferences.NotifyStreaks,
            _ => false
        };

        return eventEnabled || (newlyAttained && preferences.NotifyAchievements);
    }
}

internal sealed record ProgressUpdate(int Progress, bool NewlyAttained);