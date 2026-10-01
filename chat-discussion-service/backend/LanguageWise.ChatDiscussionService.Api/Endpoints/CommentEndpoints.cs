using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using static LanguageWise.ChatDiscussionService.Api.Endpoints.EndpointSupport;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = CreateLogger(app);

        app.MapGet("/api/posts/{id:int}/comments", (
            int id,
            HttpContext context,
            DiscussionClient client,
            CancellationToken cancellationToken,
            int limit = DiscussionRules.MaxLimit,
            int offset = 0) =>
            Guard(async () =>
            {
                var paging = DiscussionRules.ValidatePaging(limit, offset);
                if (paging.Count > 0)
                {
                    return Results.ValidationProblem(paging);
                }

                var commentsTask = client.GetCommentsAsync(
                    id,
                    limit,
                    offset,
                    DiscussionRules.GetUserId(context.User),
                    cancellationToken);
                var imagesTask = client.GetPostCommentImagesAsync(id, cancellationToken);

                await Task.WhenAll(commentsTask, imagesTask);

                return Results.Ok(Attach(await commentsTask, await imagesTask));
            }, "list comments", logger));

        app.MapPost("/api/posts/{id:int}/comments", (
            int id,
            HttpContext context,
            CreateCommentRequest? request,
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

                var errors = DiscussionRules.ValidateCreateComment(request);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var created = await client.CreateCommentAsync(
                    id,
                    userId.Value,
                    userName,
                    request!.Content!.Trim(),
                    cancellationToken);

                if (created is not null)
                {
                    await RecordContributionAsync(
                        achievementEventsClient,
                        context,
                        userId.Value,
                        "Added a comment to a community discussion",
                        cancellationToken,
                        logger);
                    await RecordPostEngagementAsync(
                        achievementEventsClient,
                        context,
                        userId.Value,
                        () => client.GetPostAsync(id, null, cancellationToken),
                        "Received a comment on a community post",
                        cancellationToken,
                        logger);
                }

                return created is null
                    ? Results.NotFound()
                    : Results.Created($"/api/comments/{created.Id}", created);
            }, "create comment", logger))
            .RequireAuthorization();

        app.MapPatch("/api/comments/{id:int}", (
            int id,
            HttpContext context,
            PatchCommentRequest? request,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var errors = DiscussionRules.ValidatePatchComment(request);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var current = await client.GetCommentAsync(id, cancellationToken);
                if (RequireOwner(userId.Value, current?.UserId) is { } denied)
                {
                    return denied;
                }

                var updated = await client.UpdateCommentAsync(
                    id,
                    DiscussionRules.MergeComment(current!, request!),
                    cancellationToken);

                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }, "update comment", logger))
            .RequireAuthorization();

        app.MapDelete("/api/comments/{id:int}", (
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

                var current = await client.GetCommentAsync(id, cancellationToken);
                if (RequireOwner(userId.Value, current?.UserId) is { } denied)
                {
                    return denied;
                }

                return await client.DeleteCommentAsync(id, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }, "delete comment", logger))
            .RequireAuthorization();

        return app;
    }
}
