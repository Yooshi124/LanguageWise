using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.ChatDiscussion;

[McpServerToolType]
public sealed partial class ChatDiscussionTools(DownstreamClient downstream)
{
	public const string ServiceName = "ChatDiscussion";

	[McpServerTool(Name = "chat_list_forums", Title = "List discussion forums", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists the available LanguageWise discussion forums and their codes.")]
	public async Task<ForumListResult> ListForumsAsync(CancellationToken cancellationToken)
	{
		var forums = await downstream.GetAsync<List<ForumDto>>(ServiceName, "api/forums", cancellationToken);
		return new ForumListResult(forums.Select(forum => new ForumItem(forum.Code, forum.Name)).ToList());
	}

	[McpServerTool(Name = "chat_search_posts", Title = "Search discussion posts", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Searches public discussion posts by their text, optionally limited to one forum. Returns at most 10 matching posts.")]
	public async Task<PostSearchResult> SearchPostsAsync(
		[Description("Search terms to find in post titles, post content or comments; 1-200 characters.")] string query,
		[Description("Optional forum code, for example global or italian.")] string? forumCode = null,
		[Description("Maximum number of posts to return, from 1 to 10. Defaults to 5.")] int limit = 5,
		CancellationToken cancellationToken = default)
	{
		query = query?.Trim() ?? string.Empty;
		if (query.Length is 0 or > 200)
		{
			throw new McpException("query must contain between 1 and 200 characters.");
		}
		if (limit is < 1 or > 10)
		{
			throw new McpException("limit must be between 1 and 10.");
		}
		if (forumCode is not null && !ForumCodePattern().IsMatch(forumCode))
		{
			throw new McpException("forumCode must contain only lowercase letters, numbers or hyphens.");
		}

		var path = $"api/posts?q={Uri.EscapeDataString(query)}&limit={limit}&offset=0";
		if (forumCode is not null)
		{
			path += $"&forumCode={Uri.EscapeDataString(forumCode)}";
		}
		var posts = await downstream.GetAsync<List<PostSearchDto>>(ServiceName, path, cancellationToken);
		return new PostSearchResult(posts.Select(post => new PostSearchItem(
			post.Id,
			post.Title,
			post.Content,
			post.ForumCode,
			post.ForumName,
			post.AuthorName,
			post.CreatedAt,
			post.CommentCount,
			post.LikeCount,
			post.MatchedCommentExcerpt)).ToList());
	}

	[McpServerTool(Name = "chat_get_post", Title = "Read a discussion post", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Reads one discussion post and its comment preview by post ID. This tool cannot create or change forum content.")]
	public async Task<DiscussionPostResult> GetPostAsync(
		[Description("Positive ID of the discussion post to read.")] int postId,
		CancellationToken cancellationToken)
	{
		if (postId <= 0)
		{
			throw new McpException("postId must be a positive integer.");
		}

		var post = await downstream.GetAsync<DiscussionPostDto>(ServiceName, $"api/posts/{postId}", cancellationToken);
		return new DiscussionPostResult(
			post.Id,
			post.Title,
			post.Content,
			post.ForumCode,
			post.ForumName,
			post.AuthorName,
			post.CreatedAt,
			post.CommentCount,
			(post.Comments ?? []).Select(comment => new DiscussionCommentItem(
				comment.AuthorName,
				comment.Content,
				comment.CreatedAt,
				comment.LikeCount)).ToList());
	}

	[GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$", RegexOptions.CultureInvariant)]
	private static partial Regex ForumCodePattern();
}