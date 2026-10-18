using Npgsql;

namespace LanguageWise.Shared.Db.Data;

public sealed class UserRepository(string connectionString)
{
    public long Count()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    /// <summary>Returns the user ID if credentials match, or null otherwise.</summary>
    public int? Verify(string username, string password)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM Users WHERE Username = @username AND Password = @password;";
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("password", password);

        var result = command.ExecuteScalar();
        return result is int id ? id : null;
    }

    public UserAccountResult Create(string username, string password)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, Password)
            VALUES (@username, @password)
            RETURNING Id, Username;
            """;
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("password", password);

        try
        {
            using var reader = command.ExecuteReader();
            reader.Read();
            return new UserAccountResult(UserAccountStatus.Saved, MapUserAccount(reader));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return new UserAccountResult(UserAccountStatus.UsernameTaken);
        }
    }

    public UserAccountResult Update(int userId, string? username, string? password)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var setClauses = new List<string>();
        if (username is not null)
        {
            setClauses.Add("Username = @username");
            command.Parameters.AddWithValue("username", username);
        }

        if (password is not null)
        {
            setClauses.Add("Password = @password");
            command.Parameters.AddWithValue("password", password);
        }

        command.CommandText = $"""
            UPDATE Users
            SET {string.Join(", ", setClauses)}
            WHERE Id = @userId
            RETURNING Id, Username;
            """;
        command.Parameters.AddWithValue("userId", userId);

        try
        {
            using var reader = command.ExecuteReader();
            return reader.Read()
                ? new UserAccountResult(UserAccountStatus.Saved, MapUserAccount(reader))
                : new UserAccountResult(UserAccountStatus.NotFound);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return new UserAccountResult(UserAccountStatus.UsernameTaken);
        }
    }

    public IReadOnlyList<UserAccount> GetUsernames(IReadOnlyCollection<int> userIds)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username
            FROM Users
            WHERE Id = ANY(@userIds)
            ORDER BY Id;
            """;
        command.Parameters.AddWithValue("userIds", userIds.ToArray());

        using var reader = command.ExecuteReader();
        var accounts = new List<UserAccount>();
        while (reader.Read())
        {
            accounts.Add(MapUserAccount(reader));
        }

        return accounts;
    }

    public int? RecordLogin(int userId, DateOnly today)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET CurrentStreak = CASE
                    WHEN LastLogin = @today - 1 THEN CurrentStreak + 1
                    ELSE 0
                END,
                                LastLogin = @today
                        WHERE Id = @userId
                            AND (LastLogin IS NULL OR LastLogin <> @today)
            RETURNING CurrentStreak;
            """;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("today", today);

        var result = command.ExecuteScalar();
        return result is int streak ? streak : null;
    }

    public ProfilePicture? GetProfilePicture(int userId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProfilePictureStorageKey, ProfilePictureFileName, ProfilePictureContentType,
                   ProfilePictureSizeBytes, ProfilePictureUploadedAt
            FROM Users
            WHERE Id = @userId AND ProfilePictureStorageKey IS NOT NULL;
            """;
        command.Parameters.AddWithValue("userId", userId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapProfilePicture(reader) : null;
    }

    public ProfilePicture? SetProfilePicture(int userId, ProfilePictureInput input)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET ProfilePictureStorageKey = @storageKey,
                ProfilePictureFileName = @fileName,
                ProfilePictureContentType = @contentType,
                ProfilePictureSizeBytes = @sizeBytes,
                ProfilePictureUploadedAt = @uploadedAt
            WHERE Id = @userId
            RETURNING ProfilePictureStorageKey, ProfilePictureFileName, ProfilePictureContentType,
                      ProfilePictureSizeBytes, ProfilePictureUploadedAt;
            """;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("storageKey", input.StorageKey);
        command.Parameters.AddWithValue("fileName", (input.FileName ?? string.Empty).Trim());
        command.Parameters.AddWithValue("contentType", (input.ContentType ?? string.Empty).Trim());
        command.Parameters.AddWithValue("sizeBytes", input.SizeBytes);
        command.Parameters.AddWithValue("uploadedAt", DateTime.UtcNow);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapProfilePicture(reader) : null;
    }

    public bool ClearProfilePicture(int userId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET ProfilePictureStorageKey = NULL,
                ProfilePictureFileName = NULL,
                ProfilePictureContentType = NULL,
                ProfilePictureSizeBytes = NULL,
                ProfilePictureUploadedAt = NULL
            WHERE Id = @userId AND ProfilePictureStorageKey IS NOT NULL;
            """;
        command.Parameters.AddWithValue("userId", userId);

        return command.ExecuteNonQuery() > 0;
    }

    private static UserAccount MapUserAccount(NpgsqlDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetString(1));

    private static ProfilePicture MapProfilePicture(NpgsqlDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt64(3),
        reader.GetDateTime(4));
}

public sealed record UserAccount(int Id, string Username);

public enum UserAccountStatus
{
    Saved,
    NotFound,
    UsernameTaken,
}

public sealed record UserAccountResult(UserAccountStatus Status, UserAccount? Account = null);

public sealed record ProfilePicture(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record ProfilePictureInput(string StorageKey, string? FileName, string? ContentType, long SizeBytes);
