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
    string Category,
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
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PostDetail(
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
    IReadOnlyList<CommentSummary> Comments,
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

public sealed record Comment(
    int Id,
    int PostId,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record Like(int Id, int? PostId, int? CommentId, int UserId, DateTime CreatedAt);

public sealed record Forum(string Code, string DisplayName, int SortOrder);

public sealed record Me(int Id, string Username);

// ---------------------------------------------------------------------------
// Requests.
//
// None of these carries a UserId. The author of a post, comment or like is
// always taken from the 'sub' claim of the caller's token, so a client cannot
// write on another user's behalf by editing the request body.
// ---------------------------------------------------------------------------

public sealed record CreatePostRequest(string? Title, string? Content, string? Category);

/// <summary>A PATCH body. A null field means 'leave it unchanged'; a blank one is rejected.</summary>
public sealed record PatchPostRequest(string? Title, string? Content, string? Category);

public sealed record CreateCommentRequest(string? Content);

public sealed record PatchCommentRequest(string? Content);
