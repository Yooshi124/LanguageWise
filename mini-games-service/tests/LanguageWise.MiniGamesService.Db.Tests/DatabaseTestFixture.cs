using LanguageWise.MiniGamesService.Db.Data;
using LanguageWise.MiniGamesService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.MiniGamesService.Db.Tests;

/// <summary>
/// Base fixture that gives each test its own throwaway file-backed SQLite database with the
/// real schema applied. In-memory SQLite would need a single shared open connection, which the
/// repositories do not support, so a temp file is the simplest faithful setup. A fresh database
/// per test keeps row counts isolated between tests.
/// </summary>
public abstract class DatabaseTestFixture
{
    private string databasePath = string.Empty;
    private string connectionString = string.Empty;

    protected GameRepository Games { get; private set; } = null!;
    protected GameAttemptRepository Attempts { get; private set; } = null!;

    [SetUp]
    public void SetUpDatabase()
    {
        databasePath = Path.Combine(Path.GetTempPath(), $"mini-games-db-tests-{Guid.NewGuid():N}.db");
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();

        ApplySchema();
        Games = new GameRepository(connectionString);
        Attempts = new GameAttemptRepository(connectionString);
    }

    [TearDown]
    public void TearDownDatabase()
    {
        // Release pooled connections before deleting the file.
        SqliteConnection.ClearPool(new SqliteConnection(connectionString));
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    /// <summary>Create a game row and return it.</summary>
    protected Game CreateGame(string gameType = "guess_the_word", string courseCode = "en") =>
        Games.Create(gameType, courseCode, "APPLE", ["APPLE", "GRAPE"]);

    private void ApplySchema()
    {
        var schemaPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "sql", "schema.sql"));
        Assert.That(File.Exists(schemaPath), Is.True, $"schema.sql should exist at {schemaPath}");

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(schemaPath);
        command.ExecuteNonQuery();
    }
}
