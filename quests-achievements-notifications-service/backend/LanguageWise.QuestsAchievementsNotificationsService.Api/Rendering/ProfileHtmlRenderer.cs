using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Rendering;

public static class ProfileHtmlRenderer
{
    public static string Render(
        string username,
        UserPreferences preferences,
        IReadOnlyList<AchievementProgress> achievements)
    {
        var builder = new StringBuilder();
        builder.Append("<section class=\"lw-card\">")
            .Append("<h2 class=\"lw-card__title\">").Append(Encode(username)).Append("</h2>")
            .Append("<form hx-put=\"/api/preferences\" hx-swap=\"outerHTML\">")
            .Append("<label for=\"email\">Notification email</label>")
            .Append("<input id=\"email\" name=\"email\" type=\"email\" required value=\"")
            .Append(Encode(preferences.Email ?? string.Empty)).Append("\">")
            .Append(Checkbox("notifyAll", "All notifications", preferences.NotifyAll))
            .Append(Checkbox("notifyPostEngagement", "Post engagement", preferences.NotifyPostEngagement))
            .Append(Checkbox("notifyCourseCompletion", "Course completion", preferences.NotifyCourseCompletion))
            .Append(Checkbox("notifyQuizResults", "Quiz results", preferences.NotifyQuizResults))
            .Append(Checkbox("notifyStreaks", "Streaks", preferences.NotifyStreaks))
            .Append(Checkbox("notifyAchievements", "Achievements", preferences.NotifyAchievements))
            .Append("<button type=\"submit\">Save preferences</button></form></section>")
            .Append("<section class=\"lw-card\"><h2 class=\"lw-card__title\">Achievements</h2>");

        foreach (var achievement in achievements)
        {
            builder.Append("<article class=\"lw-achievement\">")
                .Append("<img src=\"").Append(Encode(achievement.Image)).Append("\" alt=\"\">")
                .Append("<h3>").Append(Encode(achievement.Name)).Append("</h3>")
                .Append("<progress value=\"")
                .Append(achievement.Progress.ToString(CultureInfo.InvariantCulture))
                .Append("\" max=\"")
                .Append(achievement.ProgressNeeded.ToString(CultureInfo.InvariantCulture))
                .Append("\"></progress><span>")
                .Append(achievement.Progress.ToString(CultureInfo.InvariantCulture))
                .Append(" / ")
                .Append(achievement.ProgressNeeded.ToString(CultureInfo.InvariantCulture))
                .Append("</span></article>");
        }

        return builder.Append("</section>").ToString();
    }

    public static string RenderSaved() =>
        "<p class=\"lw-notice\">Notification preferences saved.</p>";

    private static string Checkbox(string name, string label, bool isChecked) =>
        $"<label><input type=\"checkbox\" name=\"{name}\"{(isChecked ? " checked" : string.Empty)}> {label}</label>";

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}