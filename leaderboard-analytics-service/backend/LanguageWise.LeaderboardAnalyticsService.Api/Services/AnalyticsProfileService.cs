using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Services;

public sealed class AnalyticsProfileService(QuizzesCoursesClient client)
{
    public async Task<AnalyticsProfile> GetAsync(
        int userId,
        string username,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var myMilestonesTask = client.GetAllMyMilestonesAsync(bearerToken, cancellationToken);
        var allMilestonesTask = client.GetAllMilestonesAsync(bearerToken, cancellationToken);
        var coursesTask = client.GetCoursesAsync(bearerToken, cancellationToken);
        var lessonMapTask = client.GetLessonToCourseMapAsync(bearerToken, cancellationToken);
        await Task.WhenAll(myMilestonesTask, allMilestonesTask, coursesTask, lessonMapTask);

        var rankings = AnalyticsProjector.BuildLanguageRankings(
            userId,
            myMilestonesTask.Result,
            allMilestonesTask.Result,
            coursesTask.Result,
            lessonMapTask.Result);
        var lessonsCompleted = AnalyticsProjector.BuildLessonsCompleted(
            userId,
            myMilestonesTask.Result,
            coursesTask.Result,
            lessonMapTask.Result,
            DateOnly.FromDateTime(DateTime.UtcNow));

        return new AnalyticsProfile(username, rankings, lessonsCompleted);
    }
}
