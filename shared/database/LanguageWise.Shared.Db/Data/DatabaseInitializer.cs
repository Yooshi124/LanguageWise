using Microsoft.Data.Sqlite;

namespace LanguageWise.Shared.Db.Data;

/// <summary>
/// Applies the schema and idempotent development user seed on every start-up.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        EnsureDatabaseDirectoryExists();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Execute(connection, ReadSqlFile("schema.sql"));
        EnsureColumn(connection, "LastLogin", "TEXT");
        EnsureColumn(connection, "CurrentStreak", "INTEGER NOT NULL DEFAULT 0");
        logger.LogInformation("Schema applied to {ConnectionString}.", connectionString);

        Execute(connection, ReadSqlFile("seed.sql"));
        logger.LogInformation("Development users are available.");
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

    private static void EnsureColumn(SqliteConnection connection, string columnName, string definition)
    {
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Users') WHERE name = $name;";
        query.Parameters.AddWithValue("$name", columnName);
        if (Convert.ToInt64(query.ExecuteScalar()) > 0)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE Users ADD COLUMN {columnName} {definition};";
        alter.ExecuteNonQuery();
    }
}
