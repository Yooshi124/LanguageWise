using LanguageWise.Shared.Db.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.Shared.Api.Tests;

public class DatabaseInitializerTests
{
    [Test]
    public void Initialise_RetiresSampleItemsAndSeedsUsers()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"languagewise-shared-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(directory, "shared.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

        try
        {
            Directory.CreateDirectory(directory);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE SampleItems (Id INTEGER PRIMARY KEY);";
                command.ExecuteNonQuery();
            }

            var initializer = new DatabaseInitializer(
                connectionString,
                Path.Combine(AppContext.BaseDirectory, "sql"),
                NullLogger<DatabaseInitializer>.Instance);

            initializer.Initialise();

            using (var verificationConnection = new SqliteConnection(connectionString))
            {
                verificationConnection.Open();
                using var verificationCommand = verificationConnection.CreateCommand();
                verificationCommand.CommandText =
                    """
                    SELECT
                        EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'SampleItems'),
                        (SELECT COUNT(*) FROM Users);
                    """;

                using var reader = verificationCommand.ExecuteReader();
                Assert.That(reader.Read(), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(reader.GetInt64(0), Is.Zero);
                    Assert.That(reader.GetInt64(1), Is.EqualTo(5));
                });
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
