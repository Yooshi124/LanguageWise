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
                Username TEXT NOT NULL UNIQUE,
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

    [Test]
    public void Create_WithNewUsername_ReturnsAccountThatCanVerify()
    {
        var result = repository.Create("amber", "secret");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UserAccountStatus.Saved));
            Assert.That(result.Account!.Username, Is.EqualTo("amber"));
            Assert.That(repository.Verify("amber", "secret"), Is.EqualTo(result.Account.Id));
        });
    }

    [Test]
    public void Create_WithExistingUsername_ReturnsUsernameTaken()
    {
        Assert.That(repository.Create("justin", "other"), Is.EqualTo(new UserAccountResult(UserAccountStatus.UsernameTaken)));
    }

    [Test]
    public void Update_WithUsernameAndPassword_ChangesBoth()
    {
        var result = repository.Update(7, "justin2", "new");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new UserAccountResult(UserAccountStatus.Saved, new UserAccount(7, "justin2"))));
            Assert.That(repository.Verify("justin2", "new"), Is.EqualTo(7));
            Assert.That(repository.Verify("justin", "test"), Is.Null);
        });
    }

    [Test]
    public void Update_WithPasswordOnly_KeepsUsername()
    {
        var result = repository.Update(7, null, "new");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new UserAccountResult(UserAccountStatus.Saved, new UserAccount(7, "justin"))));
            Assert.That(repository.Verify("justin", "new"), Is.EqualTo(7));
        });
    }

    [Test]
    public void Update_WithUnknownUser_ReturnsNotFound()
    {
        Assert.That(repository.Update(99, "ghost", null), Is.EqualTo(new UserAccountResult(UserAccountStatus.NotFound)));
    }

    [Test]
    public void Update_WithTakenUsername_ReturnsUsernameTaken()
    {
        repository.Create("amber", "secret");

        Assert.That(repository.Update(7, "amber", null), Is.EqualTo(new UserAccountResult(UserAccountStatus.UsernameTaken)));
    }
}