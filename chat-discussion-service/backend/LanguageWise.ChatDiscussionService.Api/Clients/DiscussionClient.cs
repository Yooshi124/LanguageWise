using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Clients;

/// <summary>The result of asking the database service to record a like.</summary>
public enum LikeOutcome
{
    Created,
    Duplicate,
    TargetNotFound
}

/// <summary>
/// Talks to the database microservice over HTTP. The backend never opens the SQLite
/// file itself; the database service is the only owner of that file.
/// </summary>
public sealed class DiscussionClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PostSummary>> GetPostsAsync(
        int? userId,
        string? category,
        int limit,
        int offset,
        int? viewerId,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("userId", userId)
            .Add("category", category)
            .Add("limit", limit)
            .Add("offset", offset)
            .Add("viewerId", viewerId);

        return await httpClient.GetFromJsonAsync<List<PostSummary>>(
            $"api/posts{query}",
            cancellationToken) ?? [];
    }

    public async Task<PostSummary?> GetPostAsync(
        int id,
        int? viewerId,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder().Add("viewerId", viewerId);
        return await GetOrNullAsync<PostSummary>($"api/posts/{id}{query}", cancellationToken);
    }

    public async Task<Post> CreatePostAsync(
        int userId,
        string title,
        string content,
        string category,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/posts",
            new { UserId = userId, Title = title, Content = content, Category = category },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Post>(cancellationToken))!;
    }

    public async Task<Post?> UpdatePostAsync(
        int id,
        string title,
        string content,
        string category,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/posts/{id}",
            new { Title = title, Content = content, Category = category },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Post>(cancellationToken);
    }

    public async Task<bool> DeletePostAsync(int id, CancellationToken cancellationToken = default) =>
        await DeleteAsync($"api/posts/{id}", cancellationToken);

    public async Task<IReadOnlyList<CommentSummary>> GetCommentsAsync(
        int postId,
        int limit,
        int offset,
        int? viewerId,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryBuilder()
            .Add("limit", limit)
            .Add("offset", offset)
            .Add("viewerId", viewerId);

        return await httpClient.GetFromJsonAsync<List<CommentSummary>>(
            $"api/posts/{postId}/comments{query}",
            cancellationToken) ?? [];
    }

    public async Task<Comment?> GetCommentAsync(int id, CancellationToken cancellationToken = default) =>
        await GetOrNullAsync<Comment>($"api/comments/{id}", cancellationToken);

    /// <summary>Returns null when the post does not exist.</summary>
    public async Task<Comment?> CreateCommentAsync(
        int postId,
        int userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/posts/{postId}/comments",
            new { UserId = userId, Content = content },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Comment>(cancellationToken);
    }

    public async Task<Comment?> UpdateCommentAsync(
        int id,
        string content,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/comments/{id}",
            new { Content = content },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Comment>(cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(int id, CancellationToken cancellationToken = default) =>
        await DeleteAsync($"api/comments/{id}", cancellationToken);

    public async Task<IReadOnlyList<Like>> GetPostLikesAsync(
        int postId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<Like>>($"api/posts/{postId}/likes", cancellationToken) ?? [];

    public async Task<IReadOnlyList<Like>> GetCommentLikesAsync(
        int commentId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<Like>>($"api/comments/{commentId}/likes", cancellationToken) ?? [];

    public async Task<LikeOutcome> LikePostAsync(
        int postId,
        int userId,
        CancellationToken cancellationToken = default) =>
        await LikeAsync($"api/posts/{postId}/likes", userId, cancellationToken);

    public async Task<LikeOutcome> LikeCommentAsync(
        int commentId,
        int userId,
        CancellationToken cancellationToken = default) =>
        await LikeAsync($"api/comments/{commentId}/likes", userId, cancellationToken);

    public async Task<bool> UnlikePostAsync(
        int postId,
        int userId,
        CancellationToken cancellationToken = default) =>
        await DeleteAsync($"api/posts/{postId}/likes?userId={userId}", cancellationToken);

    public async Task<bool> UnlikeCommentAsync(
        int commentId,
        int userId,
        CancellationToken cancellationToken = default) =>
        await DeleteAsync($"api/comments/{commentId}/likes?userId={userId}", cancellationToken);

    private async Task<LikeOutcome> LikeAsync(string path, int userId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, new { UserId = userId }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return LikeOutcome.Duplicate;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return LikeOutcome.TargetNotFound;
        }

        response.EnsureSuccessStatusCode();
        return LikeOutcome.Created;
    }

    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken cancellationToken) where T : class
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>Builds a query string, skipping parameters the caller left null.</summary>
    private sealed class QueryBuilder
    {
        private readonly List<string> parts = [];

        internal QueryBuilder Add(string name, int? value)
        {
            if (value is not null)
            {
                parts.Add($"{name}={value.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            return this;
        }

        internal QueryBuilder Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{name}={Uri.EscapeDataString(value)}");
            }

            return this;
        }

        public override string ToString() => parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
