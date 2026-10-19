using Npgsql;

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
        var exception = Assert.Throws<PostgresException>(() => Attempts.Create(gameId: 9999, userId: 7));
        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.ForeignKeyViolation));
    }

    [Test]
    public void Update_RoundTripsAllFieldsAndCanResetBooleans()
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, 7);
        const string completedAt = "2030-01-02T03:04:05.0000000Z";

        Assert.That(attempt.CompletedAt, Is.Null);
        var updated = Attempts.Update(attempt.Id, 42, true, true, 3, completedAt, 17)!;
        Assert.Multiple(() =>
        {
            Assert.That(updated.Score, Is.EqualTo(42));
            Assert.That(updated.IsWon, Is.True);
            Assert.That(updated.IsComplete, Is.True);
            Assert.That(updated.AttemptCount, Is.EqualTo(3));
            Assert.That(updated.CompletedAt, Is.EqualTo(completedAt));
            Assert.That(updated.TimeSpentSeconds, Is.EqualTo(17));
            Assert.That(updated.StartedAt, Is.EqualTo(attempt.StartedAt));
            Assert.That(Attempts.GetById(attempt.Id), Is.EqualTo(updated));
        });

        var reset = Attempts.Update(attempt.Id, score: 0, isWon: false, isComplete: false, attemptCount: 0, timeSpentSeconds: 0)!;
        Assert.Multiple(() =>
        {
            Assert.That(reset.Score, Is.Zero);
            Assert.That(reset.IsWon, Is.False);
            Assert.That(reset.IsComplete, Is.False);
            Assert.That(reset.AttemptCount, Is.Zero);
            Assert.That(reset.TimeSpentSeconds, Is.Zero);
            Assert.That(reset.CompletedAt, Is.EqualTo(completedAt));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_RejectsNonpositiveUserIds(int userId)
    {
        var game = CreateGame();
        var exception = Assert.Throws<PostgresException>(() => Attempts.Create(game.Id, userId));
        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
        Assert.That(Attempts.Count(), Is.Zero);
    }

    [TestCase("score")]
    [TestCase("attemptCount")]
    [TestCase("timeSpentSeconds")]
    public void Update_RejectsNegativeValuesWithoutChangingTheAttempt(string field)
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, 7);
        var exception = Assert.Throws<PostgresException>(() => Attempts.Update(
            attempt.Id,
            score: field == "score" ? -1 : 42,
            isWon: true,
            attemptCount: field == "attemptCount" ? -1 : null,
            timeSpentSeconds: field == "timeSpentSeconds" ? -1 : null));

        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
        Assert.That(Attempts.GetById(attempt.Id), Is.EqualTo(attempt));
    }

    [Test]
    public void GetByGameId_OrdersOldestFirstAndFiltersOtherGames()
    {
        var game = CreateGame();
        var otherGame = CreateGame();
        var first = Attempts.Create(game.Id, 7);
        var second = Attempts.Create(game.Id, 8);
        Attempts.Create(otherGame.Id, 7);

        Assert.Multiple(() =>
        {
            Assert.That(Attempts.GetByGameId(game.Id).Select(attempt => attempt.Id), Is.EqualTo(new[] { first.Id, second.Id }));
            Assert.That(Attempts.GetByGameId(9999), Is.Empty);
        });
    }

    [Test]
    public void GetByUserId_OrdersNewestFirstAndLatestFiltersGameAndUser()
    {
        var game = CreateGame();
        var otherGame = CreateGame();
        var first = Attempts.Create(game.Id, 7);
        var second = Attempts.Create(game.Id, 7);
        var otherGameAttempt = Attempts.Create(otherGame.Id, 7);
        Attempts.Create(game.Id, 8);

        Assert.Multiple(() =>
        {
            Assert.That(Attempts.GetByUserId(7).Select(attempt => attempt.Id), Is.EqualTo(new[] { otherGameAttempt.Id, second.Id, first.Id }));
            Assert.That(Attempts.GetByUserId(99), Is.Empty);
            Assert.That(Attempts.GetLatestByGameIdAndUserId(game.Id, 7), Is.EqualTo(second));
            Assert.That(Attempts.Count(), Is.EqualTo(4));
        });
    }

    [Test]
    public void Delete_RemovesOnlyTheAttemptAndPreservesTheGame()
    {
        var game = CreateGame();
        var first = Attempts.Create(game.Id, 7);
        var second = Attempts.Create(game.Id, 8);

        Assert.That(Attempts.Delete(first.Id), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(Attempts.GetById(first.Id), Is.Null);
            Assert.That(Attempts.GetById(second.Id), Is.EqualTo(second));
            Assert.That(Games.GetById(game.Id), Is.Not.Null);
            Assert.That(Attempts.Count(), Is.EqualTo(1));
            Assert.That(Attempts.Delete(first.Id), Is.False);
        });
    }

    [Test]
    public void MissingAttempt_ReadUpdateAndDeleteReturnMissingResults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Attempts.GetById(9999), Is.Null);
            Assert.That(Attempts.GetLatestByGameIdAndUserId(9999, 7), Is.Null);
            Assert.That(Attempts.Update(9999), Is.Null);
            Assert.That(Attempts.Update(9999, score: 42), Is.Null);
            Assert.That(Attempts.Delete(9999), Is.False);
            Assert.That(Attempts.Count(), Is.Zero);
        });
    }
}
