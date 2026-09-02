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
        MigrateCategoriesToForums(connection);

        Execute(connection, ReadSqlFile("schema.sql"));
        logger.LogInformation("Schema applied to {ConnectionString}.", connectionString);

        if (CountItems(connection) == 0)
        {
            Execute(connection, ReadSqlFile("seed.sql"));
            logger.LogInformation("Seeded {Count} sample items.", CountItems(connection));
        }
    }

    /// <summary>
    /// Turns a legacy Posts.Category string into a Forum row and repoints the posts.
    /// Must run before schema.sql: that script only CREATEs IF NOT EXISTS, so it
    /// would keep the old Posts table and then fail indexing a missing ForumId.
    /// </summary>
    private void MigrateCategoriesToForums(SqliteConnection connection)
    {
        using var inspect = connection.CreateCommand();
        inspect.CommandText = """
            SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Posts')
               AND EXISTS (SELECT 1 FROM pragma_table_info('Posts') WHERE name = 'Category');
            """;

        if (Convert.ToInt64(inspect.ExecuteScalar()) == 0)
        {
            return;
        }

        // Keys off for the swap: dropping Posts would cascade the comments, likes
        // and images that are about to be reattached to it.
        Execute(connection, "PRAGMA foreign_keys = OFF;");
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS Forums (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId INTEGER UNIQUE,
                Code     TEXT NOT NULL UNIQUE,
                Name     TEXT NOT NULL
            );

            INSERT INTO Forums (CourseId, Code, Name)
            SELECT NULL, Category, UPPER(SUBSTR(Category, 1, 1)) || SUBSTR(Category, 2)
              FROM (SELECT DISTINCT Category FROM Posts)
             WHERE Category NOT IN (SELECT Code FROM Forums);

            CREATE TABLE Posts_Migrated (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId      INTEGER NOT NULL,
                AuthorName  TEXT NOT NULL DEFAULT '',
                Title       TEXT NOT NULL,
                Content     TEXT NOT NULL,
                ForumId     INTEGER NOT NULL,
                CreatedAt   TEXT NOT NULL,
                UpdatedAt   TEXT NOT NULL,
                FOREIGN KEY (ForumId) REFERENCES Forums (Id)
            );

            INSERT INTO Posts_Migrated (Id, UserId, AuthorName, Title, Content, ForumId, CreatedAt, UpdatedAt)
            SELECT p.Id, p.UserId, p.AuthorName, p.Title, p.Content, f.Id, p.CreatedAt, p.UpdatedAt
              FROM Posts p JOIN Forums f ON f.Code = p.Category;

            DROP TABLE Posts;
            ALTER TABLE Posts_Migrated RENAME TO Posts;
            """);
        Execute(connection, "PRAGMA foreign_keys = ON;");

        logger.LogInformation("Migrated Posts.Category onto the Forums table.");
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
