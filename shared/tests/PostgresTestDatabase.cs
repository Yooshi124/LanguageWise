using Npgsql;

namespace LanguageWise.Testing;

public sealed class PostgresTestDatabase : IDisposable
{
    private readonly string administrativeConnectionString;
    private readonly string schemaName = $"lw_test_{Guid.NewGuid():N}";

    public string ConnectionString { get; }

    public PostgresTestDatabase()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("LANGUAGEWISE_TEST_POSTGRES")
            ?? "Host=127.0.0.1;Port=15432;Database=postgres;Username=languagewise;Password=languagewise")
        {
            Pooling = false
        };
        administrativeConnectionString = builder.ConnectionString;
        using var connection = new NpgsqlConnection(administrativeConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE SCHEMA {schemaName};";
        command.ExecuteNonQuery();
        builder.SearchPath = schemaName;
        ConnectionString = builder.ConnectionString;
    }

    public void Dispose()
    {
        using var connection = new NpgsqlConnection(administrativeConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DROP SCHEMA IF EXISTS {schemaName} CASCADE;";
        command.ExecuteNonQuery();
    }
}