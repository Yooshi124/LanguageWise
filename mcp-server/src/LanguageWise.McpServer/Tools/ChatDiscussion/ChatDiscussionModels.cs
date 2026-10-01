namespace LanguageWise.McpServer.Tools.ChatDiscussion;

public sealed record ForumListResult(IReadOnlyList<ForumItem> Forums);
public sealed record ForumItem(string Code, string Name);

public sealed record PostSearchResult(IReadOnlyList<PostSearchItem> Posts);
public sealed record PostSearchItem(
	int PostId,
	string Title,
	string Content,
	string ForumCode,
	string ForumName,
	string AuthorName,
	DateTime CreatedAt,
	int CommentCount,
	int LikeCount,
	string? MatchedCommentExcerpt);

public sealed record DiscussionPostResult(
	int PostId,
	string Title,
	string Content,
	string ForumCode,
	string ForumName,
	string AuthorName,
	DateTime CreatedAt,
	int CommentCount,
	IReadOnlyList<DiscussionCommentItem> Comments);

public sealed record DiscussionCommentItem(string AuthorName, string Content, DateTime CreatedAt, int LikeCount);

internal sealed record ForumDto(int Id, int? CourseId, string Code, string Name);
internal sealed record PostSearchDto(
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
internal sealed record DiscussionPostDto(
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
	List<object>? Images,
	List<DiscussionCommentDto>? Comments,
	bool CommentsHasMore);
internal sealed record DiscussionCommentDto(
	int Id,
	int PostId,
	int UserId,
	string AuthorName,
	string Content,
	DateTime CreatedAt,
	DateTime UpdatedAt,
	int LikeCount,
	bool LikedByViewer,
	List<object>? Images);