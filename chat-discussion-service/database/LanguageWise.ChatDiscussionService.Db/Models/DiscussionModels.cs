namespace LanguageWise.ChatDiscussionService.Db.Models;

public sealed record Post(
    int Id,
    int UserId,
    string AuthorName,
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// A post plus the engagement counts the forum list needs, so the backend never
/// has to fan out one request per row. <paramref name="LikedByViewer"/> is false
/// whenever the caller supplies no viewer.
/// </summary>
public sealed record PostSummary(
    int Id,
    int UserId,
    string AuthorName,
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentCount,
    int LikeCount,
    bool LikedByViewer,
    string? MatchedCommentExcerpt);

public sealed record PostInput(int UserId, string? AuthorName, string? Title, string? Content, string? Category);

/// <summary>Editable fields only. UserId and AuthorName are absent by design: an edit must never reassign authorship.</summary>
public sealed record PostUpdate(string? Title, string? Content, string? Category);

public sealed record Comment(
    int Id,
    int PostId,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CommentSummary(
    int Id,
    int PostId,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int LikeCount,
    bool LikedByViewer);

/// <summary>The post is taken from the route, not the body.</summary>
public sealed record CommentInput(int UserId, string? AuthorName, string? Content);

public sealed record CommentUpdate(string? Content);

public sealed record Like(int Id, int? PostId, int? CommentId, int UserId, DateTime CreatedAt);

/// <summary>The target is taken from the route. A like is created or removed, never edited.</summary>
public sealed record LikeInput(int UserId);

/// <summary>The metadata of one uploaded image; <paramref name="StorageKey"/> names the file on disk.</summary>
public sealed record Image(
    int Id,
    int? PostId,
    int? CommentId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

/// <summary>The owning post or comment is taken from the route, not the body.</summary>
public sealed record ImageInput(string StorageKey, string? FileName, string? ContentType, long SizeBytes);
