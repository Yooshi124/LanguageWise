using Microsoft.Data.Sqlite;

namespace LanguageWise.QuizzesCoursesService.Db.Data;

/// <summary>
/// Applies the development schema and its exact ordered seed manifest.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    private static readonly string[] SeedManifest =
    [
        "00-courses.sql",
        "10-de.sql",
        "20-fr.sql",
        "30-it.sql",
        "40-nl.sql",
        "50-es.sql",
        "60-pl.sql",
        "70-seed-milestone-sample-data.sql"
    ];

    public void Initialise()
    {
        EnsureDatabaseDirectoryExists();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var schemaPath = Path.Combine(sqlDirectory, "schema.sql");

        try
        {
            Execute(connection, transaction: null, ReadSqlFile(schemaPath));
            logger.LogInformation("Applied schema script {SchemaScript}.", Path.GetFileName(schemaPath));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to apply schema script {SchemaScript} from {SchemaPath}.",
                Path.GetFileName(schemaPath),
                schemaPath);
            throw;
        }

        string[] seedPaths;

        try
        {
            seedPaths = ValidateSeedManifest();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to validate the seed manifest under {SeedsDirectory}.",
                Path.Combine(sqlDirectory, "seeds"));
            throw;
        }

        var seedContext = "starting seed transaction";

        try
        {
            using var transaction = connection.BeginTransaction();

            foreach (var seedPath in seedPaths)
            {
                seedContext = seedPath;
                Execute(connection, transaction, File.ReadAllText(seedPath));
                logger.LogInformation("Executed seed script {SeedScript}.", Path.GetFileName(seedPath));
            }

            seedContext = "committing seed transaction";
            transaction.Commit();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Seed initialization failed while processing {SeedContext}; the seed transaction was not committed.",
                seedContext);
            throw;
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

    private static string ReadSqlFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Required SQL script '{Path.GetFileName(path)}' was not found at '{path}'.",
                path);
        }

        return File.ReadAllText(path);
    }

    private string[] ValidateSeedManifest()
    {
        var seedsDirectory = Path.Combine(sqlDirectory, "seeds");

        if (!Directory.Exists(seedsDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Required seed SQL directory was not found: '{seedsDirectory}'.");
        }

        var actualFileNames = Directory
            .EnumerateFiles(seedsDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".sql", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        var missingFileNames = SeedManifest
            .Except(actualFileNames, StringComparer.Ordinal)
            .ToArray();
        var unexpectedFileNames = actualFileNames
            .Except(SeedManifest, StringComparer.Ordinal)
            .ToArray();

        if (missingFileNames.Length > 0 || unexpectedFileNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"Seed SQL manifest mismatch in '{seedsDirectory}'. " +
                $"Missing: {FormatFileList(missingFileNames)}. " +
                $"Unexpected: {FormatFileList(unexpectedFileNames)}.");
        }

        return SeedManifest
            .Select(fileName => Path.Combine(seedsDirectory, fileName))
            .ToArray();
    }

    private static string FormatFileList(string[] fileNames) =>
        fileNames.Length == 0 ? "(none)" : string.Join(", ", fileNames);

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
