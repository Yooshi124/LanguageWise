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

    private const string PostSelect = """
        SELECT p.Id, p.UserId, p.AuthorName, p.Title, p.Content,
               p.ForumId, f.Code, f.Name, p.CreatedAt, p.UpdatedAt
        FROM Posts p JOIN Forums f ON f.Id = p.ForumId
        """;

    private const string PostSummarySelect = """
        SELECT p.Id, p.UserId, p.AuthorName, p.Title, p.Content,
               p.ForumId, f.Code, f.Name, p.CreatedAt, p.UpdatedAt,
               (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
               (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount,
               EXISTS (SELECT 1 FROM Likes v WHERE v.PostId = p.Id AND v.UserId = $viewerId) AS LikedByViewer,
               (SELECT mc.Content FROM Comments mc
                 WHERE mc.PostId = p.Id AND mc.Content LIKE $pattern ESCAPE '\'
                 ORDER BY mc.CreatedAt ASC, mc.Id ASC
                 LIMIT 1) AS MatchedCommentExcerpt
        FROM Posts p JOIN Forums f ON f.Id = p.ForumId
        """;

    private const string ImageSelect =
        "SELECT Id, PostId, CommentId, StorageKey, FileName, ContentType, SizeBytes, UploadedAt FROM Images";

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
        string? forumCode,
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
              AND ($forumCode IS NULL OR f.Code = $forumCode)
              AND ($pattern IS NULL
                   OR p.Title LIKE $pattern ESCAPE '\'
                   OR p.Content LIKE $pattern ESCAPE '\'
                   OR EXISTS (SELECT 1 FROM Comments sc
                               WHERE sc.PostId = p.Id AND sc.Content LIKE $pattern ESCAPE '\'))
            ORDER BY p.CreatedAt DESC, p.Id DESC
            LIMIT $limit OFFSET $offset;
            """;
        Add(command, "$userId", userId);
        Add(command, "$forumCode", forumCode);
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
        // An unknown code resolves to NULL, which the NOT NULL column rejects, so a
        // bad code cannot land a post in the wrong forum.
        command.CommandText = """
            INSERT INTO Posts (UserId, AuthorName, Title, Content, ForumId, CreatedAt, UpdatedAt)
            VALUES ($userId, $authorName, $title, $content,
                    (SELECT Id FROM Forums WHERE Code = $forumCode), $createdAt, $updatedAt)
            RETURNING Id;
            """;
        Add(command, "$userId", input.UserId);
        Add(command, "$authorName", (input.AuthorName ?? string.Empty).Trim());
        Add(command, "$title", input.Title!.Trim());
        Add(command, "$content", input.Content!.Trim());
        Add(command, "$forumCode", input.ForumCode!.Trim());
        Add(command, "$createdAt", now);
        Add(command, "$updatedAt", now);
        return GetPostRow(connection, Convert.ToInt32(command.ExecuteScalar()))!;
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
            UPDATE Posts
               SET Title = $title,
                   Content = $content,
                   ForumId = (SELECT Id FROM Forums WHERE Code = $forumCode),
                   UpdatedAt = $updatedAt
             WHERE Id = $id
            RETURNING Id;
            """;
        Add(command, "$id", id);
        Add(command, "$title", update.Title!.Trim());
        Add(command, "$content", update.Content!.Trim());
        Add(command, "$forumCode", update.ForumCode!.Trim());
        Add(command, "$updatedAt", Timestamp());
        var updatedId = command.ExecuteScalar();
        return updatedId is null ? null : GetPostRow(connection, Convert.ToInt32(updatedId));
    }

    public bool DeletePost(int id) => Delete("Posts", id);

    // -----------------------------------------------------------------------
    // Forums
    // -----------------------------------------------------------------------

    public IReadOnlyList<Forum> GetForums()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CourseId, Code, Name FROM Forums
            ORDER BY (CourseId IS NOT NULL), Name COLLATE NOCASE, Id;
            """;
        return ReadAll(command, MapForum);
    }

    public Forum? GetForum(string code)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CourseId, Code, Name FROM Forums WHERE Code = $code;";
        Add(command, "$code", code);
        return ReadFirst(command, MapForum);
    }

    /// <summary>
    /// Matching is by CourseId first, so renaming a course keeps the posts in its
    /// forum; a forum holding the code but no CourseId is adopted instead. A course is
    /// never deleted for being absent from the catalogue — that would orphan its posts.
    /// </summary>
    public ForumSyncResult SyncCourseForums(IReadOnlyList<CatalogCourse> courses)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var added = 0;
        var renamed = 0;
        var merged = 0;

        foreach (var course in courses)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE Forums SET Name = $name WHERE CourseId = $courseId AND Name <> $name;
                """;
            Add(command, "$courseId", course.Id);
            Add(command, "$name", course.Title);

            if (command.ExecuteNonQuery() > 0)
            {
                renamed++;
                continue;
            }

            using var claim = connection.CreateCommand();
            claim.Transaction = transaction;
            claim.CommandText = """
                UPDATE Forums SET CourseId = $courseId, Name = $name
                 WHERE Code = $code AND CourseId IS NULL;
                """;
            Add(claim, "$courseId", course.Id);
            Add(claim, "$code", course.Code);
            Add(claim, "$name", course.Title);

            if (claim.ExecuteNonQuery() > 0)
            {
                continue;
            }

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO Forums (CourseId, Code, Name)
                SELECT $courseId, $code, $name
                 WHERE NOT EXISTS (SELECT 1 FROM Forums WHERE CourseId = $courseId OR Code = $code);
                """;
            Add(insert, "$courseId", course.Id);
            Add(insert, "$code", course.Code);
            Add(insert, "$name", course.Title);
            added += insert.ExecuteNonQuery();
        }

        // Second pass, so every course forum above exists before anything is folded
        // into it: on a migrated volume the duplicate and its replacement are both
        // minted during this same sync.
        foreach (var course in courses)
        {
            merged += MergeLegacyLanguageForum(connection, transaction, course);
        }

        transaction.Commit();
        return new ForumSyncResult(added, renamed, merged);
    }

    /// <summary>
    /// Folds a forum the category migration minted from a legacy language name into
    /// the forum its course owns. Those rows carry no CourseId and a code spelled out
    /// in full — "italian" where the course says "it" — so neither the CourseId nor the
    /// code match above adopts them, and the language would otherwise be listed twice.
    ///
    /// Matching is by display name, the only thing the two rows share. The posts move
    /// across first and only the emptied row is dropped, so a legacy forum whose
    /// language the catalogue no longer carries is left where it is.
    /// </summary>
    private static int MergeLegacyLanguageForum(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CatalogCourse course)
    {
        using var move = connection.CreateCommand();
        move.Transaction = transaction;
        move.CommandText = """
            UPDATE Posts SET ForumId = (SELECT Id FROM Forums WHERE CourseId = $courseId)
             WHERE EXISTS (SELECT 1 FROM Forums WHERE CourseId = $courseId)
               AND ForumId IN (SELECT Id FROM Forums
                                WHERE CourseId IS NULL AND Name = $name COLLATE NOCASE);
            """;
        Add(move, "$courseId", course.Id);
        Add(move, "$name", course.Title);
        move.ExecuteNonQuery();

        using var drop = connection.CreateCommand();
        drop.Transaction = transaction;
        drop.CommandText = """
            DELETE FROM Forums
             WHERE CourseId IS NULL AND Name = $name COLLATE NOCASE
               AND EXISTS (SELECT 1 FROM Forums WHERE CourseId = $courseId);
            """;
        Add(drop, "$courseId", course.Id);
        Add(drop, "$name", course.Title);
        return drop.ExecuteNonQuery();
    }

    private static Post? GetPostRow(SqliteConnection connection, int id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"{PostSelect} WHERE p.Id = $id;";
        Add(command, "$id", id);
        return ReadFirst(command, MapPost);
    }

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

    public IReadOnlyList<Image> GetPostImages(int postId) => GetImagesFor("PostId", postId);

    public IReadOnlyList<Image> GetCommentImages(int commentId) => GetImagesFor("CommentId", commentId);

    /// <summary>Every image on every comment of one post, in a single read.</summary>
    public IReadOnlyList<Image> GetImagesForPostComments(int postId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            {ImageSelect}
            WHERE CommentId IN (SELECT Id FROM Comments WHERE PostId = $postId)
            ORDER BY UploadedAt ASC, Id ASC;
            """;
        Add(command, "$postId", postId);
        return ReadAll(command, MapImage);
    }

    public Image? GetImage(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{ImageSelect} WHERE Id = $id;";
        Add(command, "$id", id);
        return ReadFirst(command, MapImage);
    }

    /// <summary>Returns null when the post does not exist, which SQLite reports as a foreign key violation.</summary>
    public Image? CreatePostImage(int postId, ImageInput input) => CreateImage("PostId", postId, input);

    /// <summary>Returns null when the comment does not exist.</summary>
    public Image? CreateCommentImage(int commentId, ImageInput input) => CreateImage("CommentId", commentId, input);

    public bool DeleteImage(int id) => Delete("Images", id);

    /// <summary>
    /// The storage keys that deleting this post would orphan, including those on its
    /// comments. Callers read them first, because the cascade removes the rows naming them.
    /// </summary>
    public IReadOnlyList<string> GetPostStorageKeys(int postId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT StorageKey FROM Images
            WHERE PostId = $postId
               OR CommentId IN (SELECT Id FROM Comments WHERE PostId = $postId);
            """;
        Add(command, "$postId", postId);
        return ReadAll(command, reader => reader.GetString(0));
    }

    public IReadOnlyList<string> GetCommentStorageKeys(int commentId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT StorageKey FROM Images WHERE CommentId = $commentId;";
        Add(command, "$commentId", commentId);
        return ReadAll(command, reader => reader.GetString(0));
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

    private IReadOnlyList<Image> GetImagesFor(string column, int targetId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{ImageSelect} WHERE {column} = $targetId ORDER BY UploadedAt ASC, Id ASC;";
        Add(command, "$targetId", targetId);
        return ReadAll(command, MapImage);
    }

    // The column the caller does not name keeps its default of NULL, as the CHECK requires.
    private Image? CreateImage(string column, int targetId, ImageInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO Images ({column}, StorageKey, FileName, ContentType, SizeBytes, UploadedAt)
            VALUES ($targetId, $storageKey, $fileName, $contentType, $sizeBytes, $uploadedAt)
            RETURNING Id, PostId, CommentId, StorageKey, FileName, ContentType, SizeBytes, UploadedAt;
            """;
        Add(command, "$targetId", targetId);
        Add(command, "$storageKey", input.StorageKey);
        Add(command, "$fileName", (input.FileName ?? string.Empty).Trim());
        Add(command, "$contentType", (input.ContentType ?? string.Empty).Trim());
        Add(command, "$sizeBytes", input.SizeBytes);
        Add(command, "$uploadedAt", Timestamp());

        try
        {
            return ReadFirst(command, MapImage);
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == ConstraintForeignKey)
        {
            return null;
        }
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
        reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.GetString(7),
        ParseDate(reader.GetString(8)), ParseDate(reader.GetString(9)));

    private static PostSummary MapPostSummary(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.GetString(7),
        ParseDate(reader.GetString(8)), ParseDate(reader.GetString(9)),
        reader.GetInt32(10), reader.GetInt32(11), reader.GetInt32(12) != 0,
        reader.IsDBNull(13) ? null : reader.GetString(13));

    private static Forum MapForum(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.IsDBNull(1) ? null : reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3));

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
        reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetInt64(6),
        ParseDate(reader.GetString(7)));

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
