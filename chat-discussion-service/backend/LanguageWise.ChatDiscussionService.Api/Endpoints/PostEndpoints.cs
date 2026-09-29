using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using static LanguageWise.ChatDiscussionService.Api.Endpoints.EndpointSupport;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = CreateLogger(app);

        app.MapGet("/api/forums", (DiscussionClient client, CancellationToken cancellationToken) =>
            Guard(async () => Results.Ok(await client.GetForumsAsync(cancellationToken)), "read forums", logger));

        app.MapGet("/api/posts", (
            HttpContext context,
            DiscussionClient client,
            CancellationToken cancellationToken,
            bool mine = false,
            string? forumCode = null,
            string? q = null,
            string? sort = null,
            int limit = DiscussionRules.DefaultLimit,
            int offset = 0) =>
            Guard(async () =>
            {
                var paging = DiscussionRules.ValidatePaging(limit, offset);
                if (paging.Count > 0)
                {
                    return Results.ValidationProblem(paging);
                }

                if (sort is not null && !string.Equals(sort, "newest", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["sort"] = ["The only supported sort is 'newest'."]
                    });
                }

                var viewerId = DiscussionRules.GetUserId(context.User);
                if (mine && viewerId is null)
                {
                    return Results.Unauthorized();
                }

                var posts = await client.GetPostsAsync(
                    mine ? viewerId : null,
                    forumCode,
                    q,
                    limit,
                    offset,
                    viewerId,
                    cancellationToken);

                return Results.Ok(posts);
            }, "list posts", logger));

        app.MapGet("/api/posts/{id:int}", (
            int id,
            HttpContext context,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var viewerId = DiscussionRules.GetUserId(context.User);

                var postTask = client.GetPostAsync(id, viewerId, cancellationToken);
                var commentsTask = client.GetCommentsAsync(
                    id,
                    DiscussionRules.CommentPreviewLimit,
                    0,
                    viewerId,
                    cancellationToken);
                var imagesTask = client.GetPostImagesAsync(id, cancellationToken);
                var commentImagesTask = client.GetPostCommentImagesAsync(id, cancellationToken);

                await Task.WhenAll(postTask, commentsTask, imagesTask, commentImagesTask);

                var post = await postTask;
                if (post is null)
                {
                    return Results.NotFound();
                }

                var comments = Attach(await commentsTask, await commentImagesTask);
                var images = await imagesTask;

                return Results.Ok(new PostDetail(
                    post.Id,
                    post.UserId,
                    post.AuthorName,
                    post.Title,
                    post.Content,
                    post.ForumCode,
                    post.ForumName,
                    post.CreatedAt,
                    post.UpdatedAt,
                    post.CommentCount,
                    post.LikeCount,
                    post.LikedByViewer,
                    [.. images.Select(ToAttachedImage)],
                    comments,
                    post.CommentCount > comments.Count));
            }, "read post", logger));

        app.MapPost("/api/posts", (
            HttpContext context,
            CreatePostRequest? request,
            DiscussionClient client,
            AchievementEventsClient achievementEventsClient,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                var userName = DiscussionRules.GetUserName(context.User);
                if (userId is null || string.IsNullOrWhiteSpace(userName))
                {
                    return Results.Unauthorized();
                }

                var errors = DiscussionRules.ValidateCreatePost(
                    request,
                    await client.GetForumsAsync(cancellationToken));
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var created = await client.CreatePostAsync(
                    userId.Value,
                    userName,
                    request!.Title!.Trim(),
                    request.Content!.Trim(),
                    request.ForumCode!.Trim(),
                    cancellationToken);

                await RecordContributionAsync(
                    achievementEventsClient,
                    context,
                    userId.Value,
                    "Created a community post",
                    cancellationToken,
                    logger);

                return Results.Created($"/api/posts/{created.Id}", created);
            }, "create post", logger))
            .RequireAuthorization();

        app.MapPatch("/api/posts/{id:int}", (
            int id,
            HttpContext context,
            PatchPostRequest? request,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var errors = DiscussionRules.ValidatePatchPost(
                    request,
                    await client.GetForumsAsync(cancellationToken));
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var current = await client.GetPostAsync(id, null, cancellationToken);
                if (RequireOwner(userId.Value, current?.UserId) is { } denied)
                {
                    return denied;
                }

                var (title, content, forumCode) = DiscussionRules.MergePost(current!, request!);
                var updated = await client.UpdatePostAsync(id, title, content, forumCode, cancellationToken);

                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }, "update post", logger))
            .RequireAuthorization();

        app.MapDelete("/api/posts/{id:int}", (
            int id,
            HttpContext context,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var current = await client.GetPostAsync(id, null, cancellationToken);
                if (RequireOwner(userId.Value, current?.UserId) is { } denied)
                {
                    return denied;
                }

                return await client.DeletePostAsync(id, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }, "delete post", logger))
            .RequireAuthorization();

        return app;
    }
}
