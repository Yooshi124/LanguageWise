using LanguageWise.Shared.Db.Data;
using Microsoft.Data.Sqlite;

namespace LanguageWise.Shared.Api.Tests;

public sealed class UserRepositoryTests
{
    private string databasePath = null!;
    private UserRepository repository = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = Path.Combine(Path.GetTempPath(), $"languagewise-users-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                LastLogin TEXT,
                CurrentStreak INTEGER NOT NULL DEFAULT 0
            );
            INSERT INTO Users (Id, Username, Password) VALUES (7, 'justin', 'test');
            """;
        command.ExecuteNonQuery();
        repository = new UserRepository(connectionString);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    [Test]
    public void RecordLogin_OnFirstLogin_ReturnsZero()
    {
        Assert.That(repository.RecordLogin(7, new DateOnly(2026, 9, 3)), Is.Zero);
    }

    [Test]
    public void RecordLogin_OnSameDay_ReturnsNoEvent()
    {
        var today = new DateOnly(2026, 9, 3);
        repository.RecordLogin(7, today);

        Assert.That(repository.RecordLogin(7, today), Is.Null);
    }

    [Test]
    public void RecordLogin_OnFollowingDay_IncrementsCurrentStreak()
    {
        repository.RecordLogin(7, new DateOnly(2026, 9, 2));

        Assert.That(repository.RecordLogin(7, new DateOnly(2026, 9, 3)), Is.EqualTo(1));
    }

    [Test]
    public void RecordLogin_AfterGap_ResetsCurrentStreak()
    {
        repository.RecordLogin(7, new DateOnly(2026, 9, 1));

        Assert.That(repository.RecordLogin(7, new DateOnly(2026, 9, 3)), Is.Zero);
    }
}