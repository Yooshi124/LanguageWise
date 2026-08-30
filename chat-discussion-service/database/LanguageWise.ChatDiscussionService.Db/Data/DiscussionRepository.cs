using LanguageWise.ChatDiscussionService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.ChatDiscussionService.Db.Data;

/// <summary>The outcome of trying to like a post or a comment.</summary>
public enum LikeOutcome
{
    Created,
    Duplicate,
    TargetNotFound
}

public sealed class DiscussionRepository(string connectionString)
{
    // SQLite extended result codes, used to tell one constraint failure from another
    // without a second round trip. See https://sqlite.org/rescode.html.
    private const int ConstraintForeignKey = 787;
    private const int ConstraintUnique = 2067;

    private const string PostSummarySelect = """
        SELECT p.Id, p.UserId, p.AuthorName, p.Title, p.Content, p.Category, p.CreatedAt, p.UpdatedAt,
               (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
               (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount,
               EXISTS (SELECT 1 FROM Likes v WHERE v.PostId = p.Id AND v.UserId = $viewerId) AS LikedByViewer,
               (SELECT mc.Content FROM Comments mc
                 WHERE mc.PostId = p.Id AND mc.Content LIKE $pattern ESCAPE '\'
                 ORDER BY mc.CreatedAt ASC, mc.Id ASC
                 LIMIT 1) AS MatchedCommentExcerpt
        FROM Posts p
        """;

    private const string CommentSummarySelect = """
        SELECT c.Id, c.PostId, c.UserId, c.AuthorName, c.Content, c.CreatedAt, c.UpdatedAt,
               (SELECT COUNT(*) FROM Likes l WHERE l.CommentId = c.Id) AS LikeCount,
               EXISTS (SELECT 1 FROM Likes v WHERE v.CommentId = c.Id AND v.UserId = $viewerId) AS LikedByViewer
        FROM Comments c
        """;

    // Counts come from correlated subqueries rather than joins: joining Comments and
    // Likes in one statement multiplies the rows and inflates both totals.
    public IReadOnlyList<PostSummary> GetPosts(
        int? userId,
        string? category,
        string? search,
        int limit,
        int offset,
        int? viewerId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            {PostSummarySelect}
            WHERE ($userId IS NULL OR p.UserId = $userId)
              AND ($category IS NULL OR p.Category = $category)
              AND ($pattern IS NULL
                   OR p.Title LIKE $pattern ESCAPE '\'
                   OR p.Content LIKE $pattern ESCAPE '\'
                   OR EXISTS (SELECT 1 FROM Comments sc
                               WHERE sc.PostId = p.Id AND sc.Content LIKE $pattern ESCAPE '\'))
            ORDER BY p.CreatedAt DESC, p.Id DESC
            LIMIT $limit OFFSET $offset;
            """;
        Add(command, "$userId", userId);
        Add(command, "$category", category);
        Add(command, "$pattern", ToLikePattern(search));
        Add(command, "$limit", limit);
        Add(command, "$offset", offset);
        Add(command, "$viewerId", viewerId);
        return ReadAll(command, MapPostSummary);
    }

    public PostSummary? GetPost(int id, int? viewerId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{PostSummarySelect} WHERE p.Id = $id;";
        Add(command, "$id", id);
        Add(command, "$viewerId", viewerId);
        Add(command, "$pattern", null);
        return ReadFirst(command, MapPostSummary);
    }

    public Post CreatePost(PostInput input)
    {
        var now = Timestamp();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Posts (UserId, AuthorName, Title, Content, Category, CreatedAt, UpdatedAt)
            VALUES ($userId, $authorName, $title, $content, $category, $createdAt, $updatedAt)
            RETURNING Id, UserId, AuthorName, Title, Content, Category, CreatedAt, UpdatedAt;
            """;
        Add(command, "$userId", input.UserId);
        Add(command, "$authorName", (input.AuthorName ?? string.Empty).Trim());
        Add(command, "$title", input.Title!.Trim());
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$category", input.Category!.Trim());
        Add(command, "$createdAt", now);
        Add(command, "$updatedAt", now);
        return ReadFirst(command, MapPost)!;
    }

    /// <summary>
    /// Replaces the editable fields. UserId and AuthorName are deliberately not written:
    /// an edit must never reassign authorship, which would also defeat the backend's
    /// owner check.
    /// </summary>
    public Post? UpdatePost(int id, PostUpdate update)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Posts SET Title = $title, Content = $content, Category = $category, UpdatedAt = $updatedAt
            WHERE Id = $id
            RETURNING Id, UserId, AuthorName, Title, Content, Category, CreatedAt, UpdatedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$title", update.Title!.Trim());
        Add(command, "$content", update.Content!.Trim());
        Add(command, "$category", update.Category!.Trim());
        Add(command, "$updatedAt", Timestamp());
        return ReadFirst(command, MapPost);
    }

    public bool DeletePost(int id) => Delete("Posts", id);

    public IReadOnlyList<CommentSummary> GetComments(int postId, int limit, int offset, int? viewerId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            {CommentSummarySelect}
            WHERE c.PostId = $postId
            ORDER BY c.CreatedAt ASC, c.Id ASC
            LIMIT $limit OFFSET $offset;
            """;
        Add(command, "$postId", postId);
        Add(command, "$limit", limit);
        Add(command, "$offset", offset);
        Add(command, "$viewerId", viewerId);
        return ReadAll(command, MapCommentSummary);
    }

    public Comment? GetComment(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, PostId, UserId, AuthorName, Content, CreatedAt, UpdatedAt FROM Comments WHERE Id = $id;";
        Add(command, "$id", id);
        return ReadFirst(command, MapComment);
    }

    /// <summary>Returns null when the post does not exist, which SQLite reports as a foreign key violation.</summary>
    public Comment? CreateComment(int postId, CommentInput input)
    {
        var now = Timestamp();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Comments (PostId, UserId, AuthorName, Content, CreatedAt, UpdatedAt)
            VALUES ($postId, $userId, $authorName, $content, $createdAt, $updatedAt)
            RETURNING Id, PostId, UserId, AuthorName, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$postId", postId);
        Add(command, "$userId", input.UserId);
        Add(command, "$authorName", (input.AuthorName ?? string.Empty).Trim());
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$createdAt", now);
        Add(command, "$updatedAt", now);

        try
        {
            return ReadFirst(command, MapComment);
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == ConstraintForeignKey)
        {
            return null;
        }
    }

    public Comment? UpdateComment(int id, CommentUpdate update)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Comments SET Content = $content, UpdatedAt = $updatedAt
            WHERE Id = $id
            RETURNING Id, PostId, UserId, AuthorName, Content, CreatedAt, UpdatedAt;
            """;
        Add(command, "$id", id);
        Add(command, "$content", update.Content!.Trim());
        Add(command, "$updatedAt", Timestamp());
        return ReadFirst(command, MapComment);
    }

    public bool DeleteComment(int id) => Delete("Comments", id);

    public IReadOnlyList<Like> GetPostLikes(int postId) => GetLikesFor("PostId", postId);

    public IReadOnlyList<Like> GetCommentLikes(int commentId) => GetLikesFor("CommentId", commentId);

    public LikeOutcome LikePost(int postId, int userId) => CreateLike("PostId", postId, userId);

    public LikeOutcome LikeComment(int commentId, int userId) => CreateLike("CommentId", commentId, userId);

    public bool UnlikePost(int postId, int userId) => RemoveLike("PostId", postId, userId);

    public bool UnlikeComment(int commentId, int userId) => RemoveLike("CommentId", commentId, userId);

    public IReadOnlyList<Image> GetImages()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, PostId, CommentId, FileUrl, FileName, UploadedAt FROM Images ORDER BY Id;";
        return ReadAll(command, MapImage);
    }

    internal static string? ToLikePattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var escaped = search.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escaped}%";
    }

    private IReadOnlyList<Like> GetLikesFor(string column, int targetId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT Id, PostId, CommentId, UserId, CreatedAt FROM Likes WHERE {column} = $targetId ORDER BY Id;";
        Add(command, "$targetId", targetId);
        return ReadAll(command, MapLike);
    }

    private LikeOutcome CreateLike(string column, int targetId, int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO Likes (UserId, {column}, CreatedAt) VALUES ($userId, $targetId, $createdAt);";
        Add(command, "$userId", userId);
        Add(command, "$targetId", targetId);
        Add(command, "$createdAt", Timestamp());

        try
        {
            command.ExecuteNonQuery();
            return LikeOutcome.Created;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == ConstraintUnique)
        {
            return LikeOutcome.Duplicate;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == ConstraintForeignKey)
        {
            return LikeOutcome.TargetNotFound;
        }
    }

    private bool RemoveLike(string column, int targetId, int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM Likes WHERE {column} = $targetId AND UserId = $userId;";
        Add(command, "$targetId", targetId);
        Add(command, "$userId", userId);
        return command.ExecuteNonQuery() > 0;
    }

    private bool Delete(string table, int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE Id = $id;";
        Add(command, "$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    private static List<T> ReadAll<T>(SqliteCommand command, Func<SqliteDataReader, T> map)
    {
        using var reader = command.ExecuteReader();
        var results = new List<T>();
        while (reader.Read())
        {
            results.Add(map(reader));
        }

        return results;
    }

    private static T? ReadFirst<T>(SqliteCommand command, Func<SqliteDataReader, T> map) where T : class
    {
        using var reader = command.ExecuteReader();
        return reader.Read() ? map(reader) : null;
    }

    private static Post MapPost(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetString(5), ParseDate(reader.GetString(6)), ParseDate(reader.GetString(7)));

    private static PostSummary MapPostSummary(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetString(5), ParseDate(reader.GetString(6)), ParseDate(reader.GetString(7)),
        reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10) != 0,
        reader.IsDBNull(11) ? null : reader.GetString(11));

    private static Comment MapComment(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4),
        ParseDate(reader.GetString(5)), ParseDate(reader.GetString(6)));

    private static CommentSummary MapCommentSummary(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4),
        ParseDate(reader.GetString(5)), ParseDate(reader.GetString(6)),
        reader.GetInt32(7), reader.GetInt32(8) != 0);

    private static Like MapLike(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.IsDBNull(1) ? null : reader.GetInt32(1),
        reader.IsDBNull(2) ? null : reader.GetInt32(2),
        reader.GetInt32(3),
        ParseDate(reader.GetString(4)));

    private static Image MapImage(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.IsDBNull(1) ? null : reader.GetInt32(1),
        reader.IsDBNull(2) ? null : reader.GetInt32(2),
        reader.GetString(3), reader.GetString(4), ParseDate(reader.GetString(5)));

    /// <summary>
    /// Foreign key enforcement is per-connection in SQLite, so it has to be switched on
    /// for every connection rather than once in schema.sql. Without this the ON DELETE
    /// CASCADE rules never fire and a comment can be attached to a post that does not exist.
    /// </summary>
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static string Timestamp() => DateTime.UtcNow.ToString("O");

    private static void Add(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
