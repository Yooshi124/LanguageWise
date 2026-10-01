using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Services;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class StreakCalculatorTests
{
    [Test]
    public void ReturnsZeroForNoAttempts()
    {
        Assert.That(StreakCalculator.CalculateStreak([]), Is.Zero);
    }

    [Test]
    public void ReturnsZeroWhenCompletedAtIsMissing()
    {
        var attempts = new[] { CompletedOn(null) };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.Zero);
    }

    [Test]
    public void CountsTodayAsAStreakOfOne()
    {
        var attempts = new[] { CompletedOn(DateTime.UtcNow) };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.EqualTo(1));
    }

    [Test]
    public void GraceDayKeepsStreakAliveWhenOnlyYesterdayWasPlayed()
    {
        var attempts = new[] { CompletedOn(DateTime.UtcNow.AddDays(-1)) };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.EqualTo(1));
    }

    [Test]
    public void ResetsToZeroAfterAGapOfMoreThanOneDay()
    {
        var attempts = new[] { CompletedOn(DateTime.UtcNow.AddDays(-2)) };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.Zero);
    }

    [Test]
    public void CountsConsecutiveDaysIncludingToday()
    {
        var attempts = new[]
        {
            CompletedOn(DateTime.UtcNow),
            CompletedOn(DateTime.UtcNow.AddDays(-1)),
            CompletedOn(DateTime.UtcNow.AddDays(-2)),
        };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.EqualTo(3));
    }

    [Test]
    public void StopsCountingAtTheFirstGap()
    {
        var attempts = new[]
        {
            CompletedOn(DateTime.UtcNow),
            CompletedOn(DateTime.UtcNow.AddDays(-1)),
            CompletedOn(DateTime.UtcNow.AddDays(-3)),
        };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.EqualTo(2));
    }

    [Test]
    public void MultipleAttemptsOnTheSameDayCountOnce()
    {
        var attempts = new[]
        {
            CompletedOn(DateTime.UtcNow),
            CompletedOn(DateTime.UtcNow.AddHours(-1)),
        };

        Assert.That(StreakCalculator.CalculateStreak(attempts), Is.EqualTo(1));
    }

    private static GameAttemptResponse CompletedOn(DateTime? completedAt) => new(
        Id: 1,
        GameId: 1,
        UserId: 1,
        Score: 0,
        IsWon: true,
        IsComplete: true,
        AttemptCount: 1,
        StartedAt: DateTime.UtcNow.ToString("O"),
        CompletedAt: completedAt?.ToString("O"),
        TimeSpentSeconds: 30);
}
