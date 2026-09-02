namespace LanguageWise.ChatDiscussionService.Api.Models;

// ---------------------------------------------------------------------------
// Responses, mirroring the shapes the database service returns.
// ---------------------------------------------------------------------------

public sealed record PostSummary(
    int Id,
    int UserId,
    string AuthorName,
    string Title,
    string Content,
    string ForumCode,
    string ForumName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentCount,
    int LikeCount,
    bool LikedByViewer,
    string? MatchedCommentExcerpt);

public sealed record Post(
    int Id,
    int UserId,
    string AuthorName,
    string Title,
    string Content,
    string ForumCode,
    string ForumName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PostDetail(
    int Id,
    int UserId,
    string AuthorName,
    string Title,
    string Content,
    string ForumCode,
    string ForumName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentCount,
    int LikeCount,
    bool LikedByViewer,
    IReadOnlyList<AttachedImage> Images,
    IReadOnlyList<CommentDetail> Comments,
    bool CommentsHasMore);

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

/// <summary>A comment as the browser receives it: the database service's shape plus its images.</summary>
public sealed record CommentDetail(
    int Id,
    int PostId,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int LikeCount,
    bool LikedByViewer,
    IReadOnlyList<AttachedImage> Images);

public sealed record Comment(
    int Id,
    int PostId,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record Like(int Id, int? PostId, int? CommentId, int UserId, DateTime CreatedAt);

/// <summary>
/// One image attached to a post or a comment. The bytes are not inlined: the client
/// fetches them from /api/images/{id}/content.
/// </summary>
public sealed record AttachedImage(
    int Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

/// <summary>The shape the database service returns, including its own storage key.</summary>
public sealed record Image(
    int Id,
    int? PostId,
    int? CommentId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

/// <summary>Image bytes on their way back to the browser.</summary>
public sealed record ImageContent(byte[] Bytes, string ContentType);

/// <summary>One place a post can live. CourseId is null for forums that mirror no course.</summary>
public sealed record Forum(int Id, int? CourseId, string Code, string Name);

// ---------------------------------------------------------------------------
// Requests.
//
// None of these carries a UserId. The author of a post, comment or like is
// always taken from the 'sub' claim of the caller's token, so a client cannot
// write on another user's behalf by editing the request body.
// ---------------------------------------------------------------------------

public sealed record CreatePostRequest(string? Title, string? Content, string? ForumCode);

/// <summary>A PATCH body. A null field means 'leave it unchanged'; a blank one is rejected.</summary>
public sealed record PatchPostRequest(string? Title, string? Content, string? ForumCode);

public sealed record CreateCommentRequest(string? Content);

public sealed record PatchCommentRequest(string? Content);
