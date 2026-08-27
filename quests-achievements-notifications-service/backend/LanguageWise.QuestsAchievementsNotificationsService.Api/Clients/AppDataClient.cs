using System.Net;
using System.Net.Http.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public sealed class AppDataClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<Achievement>>(
            "achievements?select=achievement_id,name,image,progress_needed&order=achievement_id.asc",
            cancellationToken) ?? [];

    public async Task<Achievement?> GetAchievementAsync(int achievementId, CancellationToken cancellationToken = default)
    {
        var achievements = await httpClient.GetFromJsonAsync<List<Achievement>>(
            $"achievements?achievement_id=eq.{achievementId}&limit=1",
            cancellationToken);
        return achievements?.SingleOrDefault();
    }

    public async Task<IReadOnlyList<UserAchievement>> GetUserAchievementsAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<UserAchievement>>(
            $"user_achievements?user_id=eq.{userId}&select=user_id,achievement_id,progress",
            cancellationToken) ?? [];

    public async Task<UserPreferences?> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var preferences = await httpClient.GetFromJsonAsync<List<UserPreferences>>(
            $"user_preferences?user_id=eq.{userId}&limit=1",
            cancellationToken);
        return preferences?.SingleOrDefault();
    }

    public async Task UpsertPreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default) =>
        await UpsertAsync("user_preferences?on_conflict=user_id", preferences, cancellationToken);

    public async Task UpsertUserAchievementAsync(
        UserAchievement achievement,
        CancellationToken cancellationToken = default) =>
        await UpsertAsync(
            "user_achievements?on_conflict=user_id,achievement_id",
            achievement,
            cancellationToken);

    public async Task<bool> CreateNotificationAsync(
        NotificationInput notification,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("notifications", notification, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task UpsertAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}