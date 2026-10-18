using System.Text.Json;
using Npgsql;

namespace LanguageWise.MiniGamesService.Db.Tests;

[TestFixture]
public class GameRepositoryTests : DatabaseTestFixture
{
    [Test]
    public void Create_PersistsTheGameAndReadsItBack()
    {
        var created = CreateGame("word_search", "de");

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.GreaterThan(0));
            Assert.That(created.GameType, Is.EqualTo("word_search"));
            Assert.That(created.CourseCode, Is.EqualTo("de"));
            Assert.That(created.Solution, Is.EqualTo("APPLE"));
        });

        var fetched = Games.GetById(created.Id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.CourseCode, Is.EqualTo("de"));
    }

    [Test]
    public void Create_RejectsAnUnknownGameType()
    {
        var exception = Assert.Throws<PostgresException>(() => Games.Create("not-a-game", "en", "APPLE", ["APPLE"]));
        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
    }

    [Test]
    public void GetForAttemptUser_ReturnsOnlyGamesTheUserHasAttemptsFor()
    {
        var playedGame = CreateGame();
        var otherUserGame = CreateGame();
        var unplayedGame = CreateGame();

        Attempts.Create(playedGame.Id, userId: 7);
        Attempts.Create(otherUserGame.Id, userId: 8);
        // unplayedGame has no attempt rows at all.

        var games = Games.GetForAttemptUser(7);

        Assert.That(games.Select(game => game.Id), Is.EqualTo(new[] { playedGame.Id }));
    }

    [Test]
    public void GetForAttemptUser_ReturnsAGameOnceWhenTheUserHasSeveralAttempts()
    {
        var game = CreateGame();
        Attempts.Create(game.Id, userId: 7);
        Attempts.Create(game.Id, userId: 7);

        var games = Games.GetForAttemptUser(7);

        Assert.That(games.Count(gameEntry => gameEntry.Id == game.Id), Is.EqualTo(1));
    }

    [Test]
    public void Delete_RemovesTheGameAndCascadesToAttempts()
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, userId: 7);

        var deleted = Games.Delete(game.Id);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(Games.GetById(game.Id), Is.Null);
            Assert.That(Attempts.GetById(attempt.Id), Is.Null);
        });
    }

    [Test]
    public void Initialise_RepeatedlyPreservesRowsAndAllowsNewIdentities()
    {
        InitialiseDatabase();
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, 7);

        InitialiseDatabase();
        InitialiseDatabase();
        var nextGame = CreateGame();
        var nextAttempt = Attempts.Create(nextGame.Id, 8);

        Assert.Multiple(() =>
        {
            Assert.That(Games.GetById(game.Id), Is.Not.Null);
            Assert.That(Attempts.GetById(attempt.Id), Is.EqualTo(attempt));
            Assert.That(nextGame.Id, Is.GreaterThan(game.Id));
            Assert.That(nextAttempt.Id, Is.GreaterThan(attempt.Id));
            Assert.That(Games.Count(), Is.EqualTo(2));
            Assert.That(Attempts.Count(), Is.EqualTo(2));
        });
    }

    [Test]
    public void Initialise_RollsBackSchemaChangesWhenAScriptFails()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE GameAttempts; DROP TABLE Games; CREATE TABLE GameAttempts (Id INTEGER);";
        command.ExecuteNonQuery();

        var exception = Assert.Throws<PostgresException>(InitialiseDatabase);
        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.UndefinedColumn));
        command.CommandText = "SELECT to_regclass('games') IS NULL;";
        Assert.That(command.ExecuteScalar(), Is.True);

        command.CommandText = "DROP TABLE GameAttempts;";
        command.ExecuteNonQuery();
        InitialiseDatabase();
        Assert.That(CreateGame().Id, Is.GreaterThan(0));
    }

    [TestCase(null)]
    [TestCase("2030-01-02T03:04:05.0000000Z")]
    public void Create_RoundTripsJsonDifficultyAndNullableExpiry(string? expiresAt)
    {
        string[] words = ["A\"B", "C\\D", "caf\u00e9"];
        var game = Games.Create("associations", "fr", words[0], words, "hard", expiresAt);
        var fetched = Games.GetById(game.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(fetched.Words.Deserialize<string[]>(), Is.EqualTo(words));
            Assert.That(fetched.Difficulty, Is.EqualTo("hard"));
            Assert.That(fetched.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(fetched.CreatedAt, Is.EqualTo(game.CreatedAt));
            Assert.That(DateTime.Parse(fetched.CreatedAt).ToUniversalTime(), Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(10)));
        });
    }

    [Test]
    public void Create_RejectsAnUnknownDifficulty()
    {
        var exception = Assert.Throws<PostgresException>(() => Games.Create("word_search", "en", "APPLE", ["APPLE"], "unknown"));
        Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
        Assert.That(Games.Count(), Is.Zero);
    }

    [Test]
    public void Schema_RejectsInvalidJsonAndDuplicateIds()
    {
        var game = CreateGame();
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Games SET Words = 'not-json'::jsonb WHERE Id = @id;";
        command.Parameters.AddWithValue("id", game.Id);
        var jsonException = Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
        Assert.That(jsonException!.SqlState, Is.EqualTo(PostgresErrorCodes.InvalidTextRepresentation));

        command.CommandText = "INSERT INTO Games SELECT * FROM Games WHERE Id = @id;";
        var duplicateException = Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
        Assert.That(duplicateException!.SqlState, Is.EqualTo(PostgresErrorCodes.UniqueViolation));
        Assert.That(Games.Count(), Is.EqualTo(1));
    }

    [Test]
    public void DeleteExpired_DeletesOnlyPastExpiryAndCascadesAttempts()
    {
        var now = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expired = Games.Create("word_search", "en", "APPLE", ["APPLE"], expiresAt: now.AddSeconds(-1).ToString("O"));
        var current = Games.Create("word_search", "en", "APPLE", ["APPLE"], expiresAt: now.ToString("O"));
        var future = Games.Create("word_search", "en", "APPLE", ["APPLE"], expiresAt: now.AddSeconds(1).ToString("O"));
        var permanent = CreateGame();
        var expiredAttempt = Attempts.Create(expired.Id, 7);
        var currentAttempt = Attempts.Create(current.Id, 7);

        Assert.That(Games.DeleteExpired(now), Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(Games.GetById(expired.Id), Is.Null);
            Assert.That(Attempts.GetById(expiredAttempt.Id), Is.Null);
            Assert.That(Attempts.GetById(currentAttempt.Id), Is.Not.Null);
            Assert.That(Games.GetById(current.Id), Is.Not.Null);
            Assert.That(Games.GetById(future.Id), Is.Not.Null);
            Assert.That(Games.GetById(permanent.Id), Is.Not.Null);
            Assert.That(Games.Count(), Is.EqualTo(3));
            Assert.That(Games.DeleteExpired(now), Is.Zero);
        });
    }

    [Test]
    public void GetForAttemptUser_OrdersNewestFirstAndReturnsEmptyForUnknownUser()
    {
        var first = CreateGame();
        var second = CreateGame();
        Attempts.Create(first.Id, 7);
        Attempts.Create(second.Id, 7);

        Assert.Multiple(() =>
        {
            Assert.That(Games.GetForAttemptUser(7).Select(game => game.Id), Is.EqualTo(new[] { second.Id, first.Id }));
            Assert.That(Games.GetForAttemptUser(99), Is.Empty);
        });
    }

    [Test]
    public void MissingGame_ReturnsNullAndDeleteReturnsFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Games.GetById(9999), Is.Null);
            Assert.That(Games.Delete(9999), Is.False);
            Assert.That(Games.Count(), Is.Zero);
        });
    }
}
