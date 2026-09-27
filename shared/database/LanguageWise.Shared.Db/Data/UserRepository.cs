using Microsoft.Data.Sqlite;

namespace LanguageWise.Shared.Db.Data;

public sealed class UserRepository(string connectionString)
{
    public long Count()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    /// <summary>Returns the user ID if credentials match, or null otherwise.</summary>
    public int? Verify(string username, string password)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM Users WHERE Username = $u AND Password = $p;";
        command.Parameters.AddWithValue("$u", username);
        command.Parameters.AddWithValue("$p", password);

        var result = command.ExecuteScalar();
        return result is long id ? (int)id : null;
    }

    public int? RecordLogin(int userId, DateOnly today)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET CurrentStreak = CASE
                    WHEN date(LastLogin) = date($today, '-1 day') THEN CurrentStreak + 1
                    ELSE 0
                END,
                LastLogin = $today
            WHERE Id = $userId
              AND (LastLogin IS NULL OR date(LastLogin) <> date($today))
            RETURNING CurrentStreak;
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$today", today.ToString("yyyy-MM-dd"));

        var result = command.ExecuteScalar();
        return result is long streak ? (int)streak : null;
    }

    public ProfilePicture? GetProfilePicture(int userId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProfilePictureStorageKey, ProfilePictureFileName, ProfilePictureContentType,
                   ProfilePictureSizeBytes, ProfilePictureUploadedAt
            FROM Users
            WHERE Id = $userId AND ProfilePictureStorageKey IS NOT NULL;
            """;
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapProfilePicture(reader) : null;
    }

    public ProfilePicture? SetProfilePicture(int userId, ProfilePictureInput input)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET ProfilePictureStorageKey = $storageKey,
                ProfilePictureFileName = $fileName,
                ProfilePictureContentType = $contentType,
                ProfilePictureSizeBytes = $sizeBytes,
                ProfilePictureUploadedAt = $uploadedAt
            WHERE Id = $userId
            RETURNING ProfilePictureStorageKey, ProfilePictureFileName, ProfilePictureContentType,
                      ProfilePictureSizeBytes, ProfilePictureUploadedAt;
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$storageKey", input.StorageKey);
        command.Parameters.AddWithValue("$fileName", (input.FileName ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$contentType", (input.ContentType ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$sizeBytes", input.SizeBytes);
        command.Parameters.AddWithValue("$uploadedAt", DateTime.UtcNow.ToString("O"));

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapProfilePicture(reader) : null;
    }

    public bool ClearProfilePicture(int userId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET ProfilePictureStorageKey = NULL,
                ProfilePictureFileName = NULL,
                ProfilePictureContentType = NULL,
                ProfilePictureSizeBytes = NULL,
                ProfilePictureUploadedAt = NULL
            WHERE Id = $userId AND ProfilePictureStorageKey IS NOT NULL;
            """;
        command.Parameters.AddWithValue("$userId", userId);

        return command.ExecuteNonQuery() > 0;
    }

    private static ProfilePicture MapProfilePicture(SqliteDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt64(3),
        DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind));
}

public sealed record ProfilePicture(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record ProfilePictureInput(string StorageKey, string? FileName, string? ContentType, long SizeBytes);
