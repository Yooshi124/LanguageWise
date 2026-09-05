using Microsoft.Data.Sqlite;

namespace LanguageWise.MiniGamesService.Db.Tests;

[TestFixture]
public class GameAttemptRepositoryTests : DatabaseTestFixture
{
    [Test]
    public void Create_StartsAnIncompleteAttempt()
    {
        var game = CreateGame();

        var attempt = Attempts.Create(game.Id, userId: 7);

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Id, Is.GreaterThan(0));
            Assert.That(attempt.GameId, Is.EqualTo(game.Id));
            Assert.That(attempt.UserId, Is.EqualTo(7));
            Assert.That(attempt.IsWon, Is.False);
            Assert.That(attempt.IsComplete, Is.False);
            Assert.That(attempt.Score, Is.EqualTo(0));
        });
    }

    [Test]
    public void GetByUserId_ReturnsOnlyThatUsersAttempts()
    {
        var game = CreateGame();
        Attempts.Create(game.Id, userId: 7);
        Attempts.Create(game.Id, userId: 8);

        var attempts = Attempts.GetByUserId(7);

        Assert.That(attempts, Has.Count.EqualTo(1));
        Assert.That(attempts[0].UserId, Is.EqualTo(7));
    }

    [Test]
    public void GetLatestByGameIdAndUserId_ReturnsTheNewestAttempt()
    {
        var game = CreateGame();
        var first = Attempts.Create(game.Id, userId: 7);
        var second = Attempts.Create(game.Id, userId: 7);

        var latest = Attempts.GetLatestByGameIdAndUserId(game.Id, 7);

        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Id, Is.EqualTo(second.Id));
        Assert.That(latest.Id, Is.Not.EqualTo(first.Id));
    }

    [Test]
    public void Update_OnlyAppliesTheProvidedFields()
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, userId: 7);

        var updated = Attempts.Update(attempt.Id, score: 42, isComplete: true);

        Assert.That(updated, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Score, Is.EqualTo(42));
            Assert.That(updated.IsComplete, Is.True);
            // Untouched fields keep their defaults.
            Assert.That(updated.IsWon, Is.False);
            Assert.That(updated.AttemptCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void Update_WithNoFields_ReturnsTheAttemptUnchanged()
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, userId: 7);

        var result = Attempts.Update(attempt.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(attempt.Id));
    }

    [Test]
    public void Create_RejectsAnAttemptForAMissingGame()
    {
        Assert.Throws<SqliteException>(() => Attempts.Create(gameId: 9999, userId: 7));
    }
}
