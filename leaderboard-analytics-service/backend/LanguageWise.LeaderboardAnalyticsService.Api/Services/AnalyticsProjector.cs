using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Services;

internal static class AnalyticsProjector
{
    internal const int LessonsCompletedWindowDays = 30;

    public static IReadOnlyList<LanguageRanking> BuildLanguageRankings(
        int userId,
        IReadOnlyList<Milestone> myMilestones,
        IReadOnlyList<Milestone> allMilestones,
        IReadOnlyList<Course> courses,
        IReadOnlyDictionary<int, int> lessonToCourse)
    {
        var coursesById = courses.ToDictionary(c => c.Id);

        var scoresByCourse = ResolveLessonCourses(allMilestones, lessonToCourse)
            .GroupBy(x => x.CourseId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.Milestone.UserId).Select(u => u.Count()).ToList());

        var myByCourse = ResolveLessonCourses(myMilestones, lessonToCourse)
            .GroupBy(x => x.CourseId);

        var rankings = new List<LanguageRanking>();
        var syntheticId = 1;

        foreach (var group in myByCourse)
        {
            if (!coursesById.TryGetValue(group.Key, out var course))
            {
                continue;
            }

            var score = group.Count();
            var updatedAt = group.Max(x => x.Milestone.CompletedAt).UtcDateTime;
            var globalScores = scoresByCourse.GetValueOrDefault(group.Key) ?? new List<int>();
            var rank = 1 + globalScores.Count(s => s > score);

            rankings.Add(new LanguageRanking(
                syntheticId++,
                userId,
                course.Title,
                score,
                rank,
                updatedAt));
        }

        return rankings
            .OrderBy(r => r.Rank)
            .ThenByDescending(r => r.Score)
            .ThenBy(r => r.Language)
            .ToList();
    }

    public static LessonsCompletedResponse BuildLessonsCompleted(
        int userId,
        IReadOnlyList<Milestone> myMilestones,
        IReadOnlyList<Course> courses,
        IReadOnlyDictionary<int, int> lessonToCourse,
        DateOnly today)
    {
        var from = today.AddDays(-(LessonsCompletedWindowDays - 1));
        var to = today;
        var coursesById = courses.ToDictionary(c => c.Id);

        var series = ResolveLessonCourses(myMilestones, lessonToCourse)
            .Where(x =>
            {
                var day = DateOnly.FromDateTime(x.Milestone.CompletedAt.UtcDateTime);
                return day >= from && day <= to;
            })
            .GroupBy(x => x.CourseId)
            .Where(g => coursesById.ContainsKey(g.Key))
            .Select(g =>
            {
                var course = coursesById[g.Key];
                var byDate = g
                    .GroupBy(x => DateOnly.FromDateTime(x.Milestone.CompletedAt.UtcDateTime))
                    .ToDictionary(gg => gg.Key, gg => gg.Count());

                var points = new List<LessonsCompletedPoint>(LessonsCompletedWindowDays);
                var cumulative = 0;
                for (var i = 0; i < LessonsCompletedWindowDays; i++)
                {
                    var date = from.AddDays(i);
                    cumulative += byDate.GetValueOrDefault(date, 0);
                    points.Add(new LessonsCompletedPoint(date, cumulative));
                }

                return new LessonsCompletedSeries(course.Code, course.Title, points);
            })
            .OrderBy(s => s.CourseCode)
            .ToList();

        return new LessonsCompletedResponse(userId, from, to, series);
    }

    private static IEnumerable<(Milestone Milestone, int CourseId)> ResolveLessonCourses(
        IReadOnlyList<Milestone> milestones,
        IReadOnlyDictionary<int, int> lessonToCourse)
    {
        foreach (var milestone in milestones)
        {
            if (milestone.LessonId is not int lessonId)
            {
                continue;
            }

            if (!lessonToCourse.TryGetValue(lessonId, out var courseId))
            {
                continue;
            }

            yield return (milestone, courseId);
        }
    }
}
