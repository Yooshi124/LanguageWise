using LanguageWise.ChatDiscussionService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.ChatDiscussionService.Db.Data;

public sealed class DiscussionRepository(string connectionString)
{
    public IReadOnlyList<Post> GetPosts() => Query(
        "SELECT Id, UserId, Title, Content, CreatedAt, UpdatedAt FROM Posts ORDER BY Id;",
        reader => new Post(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5))));

    public Post? GetPost(int id) => QuerySingle(
        "SELECT Id, UserId, Title, Content, CreatedAt, UpdatedAt FROM Posts WHERE Id = $id;",
        id,
        reader => new Post(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5))));

    public Post CreatePost(PostInput input)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Posts (UserId, Title, Content, CreatedAt, UpdatedAt)
            VALUES ($userId, $title, $content, $createdAt, $updatedAt)
            RETURNING Id, UserId, Title, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$userId", input.UserId);
        Add(command, "$title", input.Title!.Trim());
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$createdAt", now);
        Add(command, "$updatedAt", now);
        using var reader = command.ExecuteReader();
        reader.Read();
        return new Post(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5)));
    }

    public Post? UpdatePost(int id, PostInput input)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Posts SET UserId = $userId, Title = $title, Content = $content, UpdatedAt = $updatedAt
            WHERE Id = $id
            RETURNING Id, UserId, Title, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$userId", input.UserId);
        Add(command, "$title", input.Title!.Trim());
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$updatedAt", now);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Post(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5)))
            : null;
    }

    public bool DeletePost(int id) => Delete("Posts", id);

    public IReadOnlyList<Comment> GetComments() => Query(
        "SELECT Id, PostId, UserId, Content, CreatedAt, UpdatedAt FROM Comments ORDER BY Id;",
        reader => new Comment(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5))));

    public Comment? GetComment(int id) => QuerySingle(
        "SELECT Id, PostId, UserId, Content, CreatedAt, UpdatedAt FROM Comments WHERE Id = $id;",
        id,
        reader => new Comment(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5))));

    public Comment CreateComment(CommentInput input)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Comments (PostId, UserId, Content, CreatedAt, UpdatedAt)
            VALUES ($postId, $userId, $content, $createdAt, $updatedAt)
            RETURNING Id, PostId, UserId, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$postId", input.PostId);
        Add(command, "$userId", input.UserId);
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$createdAt", now);
        Add(command, "$updatedAt", now);
        using var reader = command.ExecuteReader();
        reader.Read();
        return new Comment(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5)));
    }

    public Comment? UpdateComment(int id, CommentInput input)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Comments SET PostId = $postId, UserId = $userId, Content = $content, UpdatedAt = $updatedAt
            WHERE Id = $id
            RETURNING Id, PostId, UserId, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$postId", input.PostId);
        Add(command, "$userId", input.UserId);
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$updatedAt", now);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Comment(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), ParseDate(reader.GetString(4)), ParseDate(reader.GetString(5)))
            : null;
    }

    public bool DeleteComment(int id) => Delete("Comments", id);

    public IReadOnlyList<Like> GetLikes() => Query(
        "SELECT Id, PostId, CommentId, UserId, CreatedAt FROM Likes ORDER BY Id;",
        reader => new Like(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetInt32(3), ParseDate(reader.GetString(4))));

    public Like? GetLike(int id) => QuerySingle(
        "SELECT Id, PostId, CommentId, UserId, CreatedAt FROM Likes WHERE Id = $id;",
        id,
        reader => new Like(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetInt32(3), ParseDate(reader.GetString(4))));

    public Like CreateLike(LikeInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Likes (PostId, CommentId, UserId, CreatedAt)
            VALUES ($postId, $commentId, $userId, $createdAt)
            RETURNING Id, PostId, CommentId, UserId, CreatedAt;
            """;
        Add(command, "$postId", input.PostId);
        Add(command, "$commentId", input.CommentId);
        Add(command, "$userId", input.UserId);
        Add(command, "$createdAt", DateTime.UtcNow.ToString("O"));
        using var reader = command.ExecuteReader();
        reader.Read();
        return new Like(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetInt32(3), ParseDate(reader.GetString(4)));
    }

    public Like? UpdateLike(int id, LikeInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Likes SET PostId = $postId, CommentId = $commentId, UserId = $userId
            WHERE Id = $id
            RETURNING Id, PostId, CommentId, UserId, CreatedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$postId", input.PostId);
        Add(command, "$commentId", input.CommentId);
        Add(command, "$userId", input.UserId);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Like(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetInt32(3), ParseDate(reader.GetString(4)))
            : null;
    }

    public bool DeleteLike(int id) => Delete("Likes", id);

    public IReadOnlyList<Image> GetImages() => Query(
        "SELECT Id, PostId, CommentId, FileUrl, FileName, UploadedAt FROM Images ORDER BY Id;",
        reader => new Image(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetString(3), reader.GetString(4), ParseDate(reader.GetString(5))));

    public Image? GetImage(int id) => QuerySingle(
        "SELECT Id, PostId, CommentId, FileUrl, FileName, UploadedAt FROM Images WHERE Id = $id;",
        id,
        reader => new Image(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetString(3), reader.GetString(4), ParseDate(reader.GetString(5))));

    public Image CreateImage(ImageInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Images (PostId, CommentId, FileUrl, FileName, UploadedAt)
            VALUES ($postId, $commentId, $fileUrl, $fileName, $uploadedAt)
            RETURNING Id, PostId, CommentId, FileUrl, FileName, UploadedAt;
            """;
        Add(command, "$postId", input.PostId);
        Add(command, "$commentId", input.CommentId);
        Add(command, "$fileUrl", input.FileUrl!.Trim());
        Add(command, "$fileName", input.FileName!.Trim());
        Add(command, "$uploadedAt", DateTime.UtcNow.ToString("O"));
        using var reader = command.ExecuteReader();
        reader.Read();
        return new Image(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetString(3), reader.GetString(4), ParseDate(reader.GetString(5)));
    }

    public Image? UpdateImage(int id, ImageInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Images SET PostId = $postId, CommentId = $commentId, FileUrl = $fileUrl, FileName = $fileName
            WHERE Id = $id
            RETURNING Id, PostId, CommentId, FileUrl, FileName, UploadedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$postId", input.PostId);
        Add(command, "$commentId", input.CommentId);
        Add(command, "$fileUrl", input.FileUrl!.Trim());
        Add(command, "$fileName", input.FileName!.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Image(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetString(3), reader.GetString(4), ParseDate(reader.GetString(5)))
            : null;
    }

    public bool DeleteImage(int id) => Delete("Images", id);

    private List<T> Query<T>(string sql, Func<SqliteDataReader, T> map)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var results = new List<T>();
        while (reader.Read())
        {
            results.Add(map(reader));
        }

        return results;
    }

    private T? QuerySingle<T>(string sql, int id, Func<SqliteDataReader, T> map) where T : class
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        Add(command, "$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? map(reader) : null;
    }

    private bool Delete(string table, int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE id = $id;";
        Add(command, "$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void Add(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static DateTime ParseDate(string value) => DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
}