using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
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

        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            errors["eventId"] = ["Event ID is required."];
        }

        if (request.Trigger is not ("post-engagement" or "course-completion" or "quiz-result" or "streak"))
        {
            errors["trigger"] = ["Trigger is not supported."];
        }

        if (string.IsNullOrWhiteSpace(request.SubjectId))
        {
            errors["subjectId"] = ["Subject ID is required."];
        }

        if (request.RecipientUserId <= 0)
        {
            errors["recipientUserId"] = ["Recipient user ID must be positive."];
        }

        if (request.OccurredAt == default)
        {
            errors["occurredAt"] = ["Occurrence time is required."];
        }

        if (request.Value <= 0)
        {
            errors["value"] = ["Value must be positive."];
        }

        if (request.Metadata is { ValueKind: not JsonValueKind.Object })
        {
            errors["metadata"] = ["Metadata must be a JSON object."];
        }

        return errors;
    }

    internal static ProgressUpdate CalculateProgress(int oldProgress, int value, int progressNeeded)
    {
        var progress = (int)Math.Min((long)oldProgress + value, progressNeeded);
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