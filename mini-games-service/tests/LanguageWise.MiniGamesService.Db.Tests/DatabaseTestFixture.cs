using LanguageWise.MiniGamesService.Db.Data;
using LanguageWise.MiniGamesService.Db.Models;
using LanguageWise.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.MiniGamesService.Db.Tests;

/// <summary>
/// Gives each test its own PostgreSQL schema with the production initializer applied.
/// </summary>
public abstract class DatabaseTestFixture
{
    private PostgresTestDatabase database = null!;

    protected string ConnectionString => database.ConnectionString;
    protected string SqlDirectory => Path.Combine(AppContext.BaseDirectory, "sql");

    protected GameRepository Games { get; private set; } = null!;
    protected GameAttemptRepository Attempts { get; private set; } = null!;

    [SetUp]
    public void SetUpDatabase()
    {
        database = new PostgresTestDatabase();
        InitialiseDatabase();
        Games = new GameRepository(ConnectionString);
        Attempts = new GameAttemptRepository(ConnectionString);
    }

    [TearDown]
    public void TearDownDatabase()
    {
        database?.Dispose();
    }

    /// <summary>Create a game row and return it.</summary>
    protected Game CreateGame(string gameType = "guess_the_word", string courseCode = "en") =>
        Games.Create(gameType, courseCode, "APPLE", ["APPLE", "GRAPE"]);

    protected void InitialiseDatabase() =>
        new DatabaseInitializer(ConnectionString, SqlDirectory, NullLogger<DatabaseInitializer>.Instance).Initialise();
}
