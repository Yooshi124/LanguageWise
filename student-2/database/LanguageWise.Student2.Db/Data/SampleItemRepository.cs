using LanguageWise.Student2.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.Student2.Db.Data;

/// <summary>Plain ADO.NET data access for the SampleItems table.</summary>
public sealed class SampleItemRepository(string connectionString)
{
    private const string SelectColumns = "SELECT Id, Name, Description, CreatedAt FROM SampleItems";

    public IReadOnlyList<SampleItem> GetAll()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} ORDER BY Id;";

        var items = new List<SampleItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public SampleItem? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public SampleItem Create(string name, string description)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO SampleItems (Name, Description, CreatedAt)
            VALUES ($name, $description, $createdAt)
            RETURNING Id, Name, Description, CreatedAt;
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));

        using var reader = command.ExecuteReader();
        reader.Read();
        return Map(reader);
    }

    public SampleItem? Update(int id, string name, string description)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE SampleItems
            SET Name = $name, Description = $description
            WHERE Id = $id
            RETURNING Id, Name, Description, CreatedAt;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$description", description);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SampleItems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>Cheap query used by the health endpoint to prove the database is reachable.</summary>
    public long Count()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SampleItems;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static SampleItem Map(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3));
}
