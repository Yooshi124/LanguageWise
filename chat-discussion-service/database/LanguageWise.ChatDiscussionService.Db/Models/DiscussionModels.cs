namespace LanguageWise.ChatDiscussionService.Db.Models;

public sealed record Post(
    int Id,
    int UserId,
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
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentCount,
    int LikeCount,
    bool LikedByViewer);

public sealed record PostInput(int UserId, string? Title, string? Content, string? Category);

/// <summary>Editable fields only. UserId is absent by design: an edit must never reassign authorship.</summary>
public sealed record PostUpdate(string? Title, string? Content, string? Category);

public sealed record Comment(
    int Id,
    int PostId,
    int UserId,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CommentSummary(
    int Id,
    int PostId,
    int UserId,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int LikeCount,
    bool LikedByViewer);

/// <summary>The post is taken from the route, not the body.</summary>
public sealed record CommentInput(int UserId, string? Content);

public sealed record CommentUpdate(string? Content);

public sealed record Like(int Id, int? PostId, int? CommentId, int UserId, DateTime CreatedAt);

/// <summary>The target is taken from the route. A like is created or removed, never edited.</summary>
public sealed record LikeInput(int UserId);

public sealed record Image(int Id, int? PostId, int? CommentId, string FileUrl, string FileName, DateTime UploadedAt);

public sealed record ImageInput(int? PostId, int? CommentId, string? FileUrl, string? FileName);
