using Microsoft.Data.Sqlite;

namespace LanguageWise.LeaderboardAnalyticsService.Db.Data;

public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        EnsureDatabaseDirectoryExists();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Execute(connection, ReadSqlFile("schema.sql"));
        logger.LogInformation("Schema applied to {ConnectionString}.", connectionString);

        if (CountItems(connection) == 0)
        {
            Execute(connection, ReadSqlFile("seed.sql"));
            logger.LogInformation("Seeded {Count} sample items.", CountItems(connection));
        }
    }

    private void EnsureDatabaseDirectoryExists()
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private string ReadSqlFile(string fileName)
    {
        var path = Path.Combine(sqlDirectory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required SQL script '{fileName}' was not found.", path);
        }

        return File.ReadAllText(path);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long CountItems(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM LanguageRanking;";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
