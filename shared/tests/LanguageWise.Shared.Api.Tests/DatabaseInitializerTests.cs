using LanguageWise.Shared.Db.Data;
using LanguageWise.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace LanguageWise.Shared.Api.Tests;

public class DatabaseInitializerTests
{
    [Test]
    public void Initialise_RetiresSampleItemsAndSeedsUsers()
    {
        using var database = new PostgresTestDatabase();
        using var connection = new NpgsqlConnection(database.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE SampleItems (Id INTEGER PRIMARY KEY);";
        command.ExecuteNonQuery();

        CreateInitializer(database).Initialise();

        command.CommandText = "SELECT to_regclass('sampleitems') IS NULL;";
        Assert.That(command.ExecuteScalar(), Is.True);
        var repository = new UserRepository(database.ConnectionString);
        Assert.Multiple(() =>
        {
            Assert.That(repository.Count(), Is.EqualTo(5));
            Assert.That(repository.Verify("amber", "test"), Is.EqualTo(1));
            Assert.That(repository.Verify("lachlan", "password"), Is.EqualTo(2));
            Assert.That(repository.Verify("roan", "password"), Is.EqualTo(3));
            Assert.That(repository.Verify("justin", "password"), Is.EqualTo(4));
            Assert.That(repository.Verify("kyan", "password"), Is.EqualTo(5));
        });
    }

    [Test]
    public void Initialise_Repeatedly_PreservesUserChangesAndAdvancesIdentity()
    {
        using var database = new PostgresTestDatabase();
        var initializer = CreateInitializer(database);
        initializer.Initialise();
        var repository = new UserRepository(database.ConnectionString);
        var created = repository.Create("new-user", "secret").Account!;
        repository.Update(1, "amber-renamed", "changed");
        repository.RecordLogin(1, new DateOnly(2026, 9, 2));
        repository.RecordLogin(1, new DateOnly(2026, 9, 3));
        var picture = repository.SetProfilePicture(1, new ProfilePictureInput("key", "photo.png", "image/png", 42));

        initializer.Initialise();
        initializer.Initialise();

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.EqualTo(6));
            Assert.That(repository.Count(), Is.EqualTo(6));
            Assert.That(repository.Verify("amber-renamed", "changed"), Is.EqualTo(1));
            Assert.That(repository.Verify("amber", "test"), Is.Null);
            Assert.That(repository.GetProfilePicture(1), Is.EqualTo(picture));
            Assert.That(repository.RecordLogin(1, new DateOnly(2026, 9, 3)), Is.Null);
            Assert.That(repository.RecordLogin(1, new DateOnly(2026, 9, 4)), Is.EqualTo(2));
            Assert.That(repository.Create("after-restart", "secret").Account!.Id, Is.EqualTo(7));
        });
    }

    [Test]
    public void Initialise_WithExplicitHighId_AdvancesIdentityWithoutReusingDeletedIds()
    {
        using var database = new PostgresTestDatabase();
        var initializer = CreateInitializer(database);
        initializer.Initialise();
        using var connection = new NpgsqlConnection(database.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Id, Username, Password) VALUES (1000, 'imported', 'secret');";
        command.ExecuteNonQuery();

        initializer.Initialise();
        var repository = new UserRepository(database.ConnectionString);
        Assert.That(repository.Create("next", "secret").Account!.Id, Is.EqualTo(1001));
        command.CommandText = "DELETE FROM Users WHERE Id >= 1000;";
        command.ExecuteNonQuery();
        initializer.Initialise();
        Assert.That(repository.Create("after-delete", "secret").Account!.Id, Is.EqualTo(1002));
    }

    [Test]
    public void Initialise_WhenSeedFails_RollsBackSchemaAndSeed()
    {
        using var database = new PostgresTestDatabase();
        var directory = Path.Combine(Path.GetTempPath(), $"languagewise-shared-sql-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "sql", "schema.sql"), Path.Combine(directory, "schema.sql"));
            File.WriteAllText(Path.Combine(directory, "seed.sql"),
                "INSERT INTO Users (Username, Password) VALUES ('partial', 'secret'); SELECT missing_column FROM Users;");
            var initializer = new DatabaseInitializer(database.ConnectionString, directory, NullLogger<DatabaseInitializer>.Instance);

            Assert.Throws<PostgresException>(() => initializer.Initialise());

            using var connection = new NpgsqlConnection(database.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('users') IS NULL;";
            Assert.That(command.ExecuteScalar(), Is.True);
            CreateInitializer(database).Initialise();
            Assert.That(new UserRepository(database.ConnectionString).Count(), Is.EqualTo(5));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DatabaseInitializer CreateInitializer(PostgresTestDatabase database) => new(
        database.ConnectionString,
        Path.Combine(AppContext.BaseDirectory, "sql"),
        NullLogger<DatabaseInitializer>.Instance);
}
