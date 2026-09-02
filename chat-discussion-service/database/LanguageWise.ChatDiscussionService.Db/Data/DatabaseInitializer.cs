using Microsoft.Data.Sqlite;

namespace LanguageWise.ChatDiscussionService.Db.Data;

/// <summary>
/// Applies the schema on every start-up and seeds the table the first time it is empty,
/// so that a brand new Docker volume always comes up with usable data.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        EnsureDatabaseDirectoryExists();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        DropLegacyImagesTable(connection);

        Execute(connection, ReadSqlFile("schema.sql"));
        logger.LogInformation("Schema applied to {ConnectionString}.", connectionString);

        if (CountItems(connection) == 0)
        {
            Execute(connection, ReadSqlFile("seed.sql"));
            logger.LogInformation("Seeded {Count} sample items.", CountItems(connection));
        }
    }

    /// <summary>
    /// A volume created before uploads existed carries an Images table with the wrong
    /// columns, holding placeholder rows that pointed at files never written. Dropping it
    /// lets schema.sql, which can only CREATE TABLE IF NOT EXISTS, recreate it.
    /// </summary>
    private void DropLegacyImagesTable(SqliteConnection connection)
    {
        using var inspect = connection.CreateCommand();
        inspect.CommandText = """
            SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Images')
               AND NOT EXISTS (SELECT 1 FROM pragma_table_info('Images') WHERE name = 'StorageKey');
            """;

        if (Convert.ToInt64(inspect.ExecuteScalar()) == 0)
        {
            return;
        }

        Execute(connection, "DROP TABLE Images;");
        logger.LogInformation("Dropped the pre-upload Images table so the current schema could be applied.");
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
        command.CommandText = "SELECT COUNT(*) FROM SampleItems;";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
