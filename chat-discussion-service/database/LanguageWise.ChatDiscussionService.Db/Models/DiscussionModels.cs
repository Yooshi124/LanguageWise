namespace LanguageWise.ChatDiscussionService.Db.Models;

public sealed record Post(int Id, int UserId, string Title, string Content, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record PostInput(int UserId, string? Title, string? Content);

public sealed record Comment(int Id, int PostId, int UserId, string Content, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CommentInput(int PostId, int UserId, string? Content);

public sealed record Like(int Id, int? PostId, int? CommentId, int UserId, DateTime CreatedAt);

public sealed record LikeInput(int? PostId, int? CommentId, int UserId);

public sealed record Image(int Id, int? PostId, int? CommentId, string FileUrl, string FileName, DateTime UploadedAt);

public sealed record ImageInput(int? PostId, int? CommentId, string? FileUrl, string? FileName);