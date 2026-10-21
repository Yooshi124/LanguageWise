using Npgsql;

namespace LanguageWise.Shared.Db.Data;

public sealed record AdminSetting(string Name, bool IsEnabled);

/// <summary>Global feature switches that apply to every account.</summary>
public sealed class AdminSettingsRepository(string connectionString)
{
    public const string AiMode = "AIMode";

    public IReadOnlyList<AdminSetting> GetAll()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, IsEnabled FROM AdminSettings ORDER BY Name;";

        using var reader = command.ExecuteReader();
        var settings = new List<AdminSetting>();
        while (reader.Read())
        {
            settings.Add(new AdminSetting(reader.GetString(0), reader.GetBoolean(1)));
        }

        return settings;
    }

    /// <summary>Returns the updated setting, or null when the name is unknown.</summary>
    public AdminSetting? Set(string name, bool isEnabled)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE AdminSettings
            SET IsEnabled = @isEnabled
            WHERE Name = @name
            RETURNING Name, IsEnabled;
            """;
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("isEnabled", isEnabled);

        AdminSetting? updated;
        using (var reader = command.ExecuteReader())
        {
            updated = reader.Read() ? new AdminSetting(reader.GetString(0), reader.GetBoolean(1)) : null;
        }

        // Every other switch depends on AI mode, so turning it off turns them all off.
        if (updated is { IsEnabled: false, Name: AiMode })
        {
            using var cascade = connection.CreateCommand();
            cascade.Transaction = transaction;
            cascade.CommandText = "UPDATE AdminSettings SET IsEnabled = FALSE WHERE IsEnabled;";
            cascade.ExecuteNonQuery();
        }

        transaction.Commit();
        return updated;
    }
}
