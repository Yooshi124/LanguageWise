using LanguageWise.Shared.Db.Data;
using LanguageWise.Testing;
using Npgsql;

namespace LanguageWise.Shared.Api.Tests;

public sealed class UserRepositoryTests
{
    private PostgresTestDatabase database = null!;
    private UserRepository repository = null!;

    [SetUp]
    public void SetUp()
    {
        database = new PostgresTestDatabase();
        using var connection = new NpgsqlConnection(database.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "schema.sql"));
        command.ExecuteNonQuery();
        command.CommandText = """
            INSERT INTO Users (Id, Username, Password) VALUES (7, 'justin', 'test');
            SELECT setval(pg_get_serial_sequence('users', 'id'), 7, true);
            """;
        command.ExecuteNonQuery();
        repository = new UserRepository(database.ConnectionString);
    }

    [TearDown]
    public void TearDown()
    {
        database.Dispose();
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
    public void GetUsernames_ReturnsOnlyTheUsersThatExist()
    {
        var amber = repository.Create("amber", "secret").Account!;

        var accounts = repository.GetUsernames([7, amber.Id, 404]);

        Assert.That(accounts, Is.EqualTo(new[] { new UserAccount(7, "justin"), amber }));
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

    [TestCase(2026, 1, 31, 2026, 2, 1)]
    [TestCase(2026, 12, 31, 2027, 1, 1)]
    [TestCase(2028, 2, 28, 2028, 2, 29)]
    [TestCase(2028, 2, 29, 2028, 3, 1)]
    public void RecordLogin_AcrossCalendarBoundary_IncrementsStreak(
        int firstYear, int firstMonth, int firstDay, int nextYear, int nextMonth, int nextDay)
    {
        repository.RecordLogin(7, new DateOnly(firstYear, firstMonth, firstDay));

        Assert.That(repository.RecordLogin(7, new DateOnly(nextYear, nextMonth, nextDay)), Is.EqualTo(1));
    }

    [Test]
    public void RecordLogin_WithUnknownUser_ReturnsNoEvent()
    {
        Assert.That(repository.RecordLogin(404, new DateOnly(2026, 9, 3)), Is.Null);
    }

    [Test]
    public void RecordLogin_WithEarlierDate_ResetsStreak()
    {
        repository.RecordLogin(7, new DateOnly(2026, 9, 3));
        repository.RecordLogin(7, new DateOnly(2026, 9, 4));

        Assert.That(repository.RecordLogin(7, new DateOnly(2026, 9, 2)), Is.Zero);
    }

    [Test]
    public void GetUsernames_WithEmptyOrDuplicateIds_ReturnsDistinctOrderedAccounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetUsernames([]), Is.Empty);
            Assert.That(repository.GetUsernames([404, 7, 7]), Is.EqualTo(new[] { new UserAccount(7, "justin") }));
        });
    }

    [Test]
    public void Verify_WithWrongCredentials_ReturnsNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.Verify("justin", "wrong"), Is.Null);
            Assert.That(repository.Verify("missing", "test"), Is.Null);
            Assert.That(repository.Verify("Justin", "test"), Is.Null);
        });
    }

    [Test]
    public void Update_WithTakenUsername_DoesNotChangePassword()
    {
        repository.Create("amber", "secret");

        var result = repository.Update(7, "amber", "changed");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UserAccountStatus.UsernameTaken));
            Assert.That(repository.Verify("justin", "test"), Is.EqualTo(7));
            Assert.That(repository.Verify("justin", "changed"), Is.Null);
        });
    }

    [TestCase(null, null, "", "")]
    [TestCase(" photo.png ", " image/png ", "photo.png", "image/png")]
    public void ProfilePicture_RoundTripsMetadataAndClears(
        string? fileName, string? contentType, string expectedFileName, string expectedContentType)
    {
        var before = DateTime.UtcNow;
        var picture = repository.SetProfilePicture(7,
            new ProfilePictureInput("storage-key", fileName, contentType, (long)int.MaxValue + 1))!;

        Assert.Multiple(() =>
        {
            Assert.That(picture.StorageKey, Is.EqualTo("storage-key"));
            Assert.That(picture.FileName, Is.EqualTo(expectedFileName));
            Assert.That(picture.ContentType, Is.EqualTo(expectedContentType));
            Assert.That(picture.SizeBytes, Is.EqualTo((long)int.MaxValue + 1));
            Assert.That(picture.UploadedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(picture.UploadedAt, Is.InRange(before.AddMilliseconds(-1), DateTime.UtcNow));
            Assert.That(repository.GetProfilePicture(7), Is.EqualTo(picture));
            Assert.That(repository.ClearProfilePicture(7), Is.True);
            Assert.That(repository.GetProfilePicture(7), Is.Null);
            Assert.That(repository.ClearProfilePicture(7), Is.False);
        });
    }

    [Test]
    public void ProfilePicture_WithUnknownUser_ReturnsNotFound()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetProfilePicture(404), Is.Null);
            Assert.That(repository.SetProfilePicture(404, new ProfilePictureInput("key", null, null, 0)), Is.Null);
            Assert.That(repository.ClearProfilePicture(404), Is.False);
        });
    }
}