using LanguageWise.ChatDiscussionService.Api.Clients;
using static LanguageWise.ChatDiscussionService.Api.Endpoints.EndpointSupport;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class LikeEndpoints
{
    public static IEndpointRouteBuilder MapLikeEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = CreateLogger(app);

        app.MapGet("/api/posts/{id:int}/likes", (int id, DiscussionClient client, CancellationToken cancellationToken) =>
            Guard(async () => Results.Ok(await client.GetPostLikesAsync(id, cancellationToken)), "list post likes", logger));

        app.MapGet("/api/comments/{id:int}/likes", (int id, DiscussionClient client, CancellationToken cancellationToken) =>
            Guard(async () => Results.Ok(await client.GetCommentLikesAsync(id, cancellationToken)), "list comment likes", logger));

        app.MapPost("/api/posts/{id:int}/likes", (
            int id,
            HttpContext context,
            DiscussionClient client,
            AchievementEventsClient achievementEventsClient,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var outcome = await client.LikePostAsync(id, userId.Value, cancellationToken);
                if (outcome == LikeOutcome.Created)
                {
                    await RecordPostEngagementAsync(
                        achievementEventsClient,
                        context,
                        userId.Value,
                        () => client.GetPostAsync(id, null, cancellationToken),
                        "Received a like on a community post",
                        cancellationToken,
                        logger);
                }

                return Describe(outcome);
            }, "like post", logger))
            .RequireAuthorization();

        app.MapDelete("/api/posts/{id:int}/likes", (
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

                return await client.UnlikePostAsync(id, userId.Value, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }, "unlike post", logger))
            .RequireAuthorization();

        app.MapPost("/api/comments/{id:int}/likes", (
            int id,
            HttpContext context,
            DiscussionClient client,
            AchievementEventsClient achievementEventsClient,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var userId = DiscussionRules.GetUserId(context.User);
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var outcome = await client.LikeCommentAsync(id, userId.Value, cancellationToken);
                if (outcome == LikeOutcome.Created)
                {
                    await RecordPostEngagementAsync(
                        achievementEventsClient,
                        context,
                        userId.Value,
                        () => client.GetCommentAsync(id, cancellationToken),
                        "Received a like on a community comment",
                        cancellationToken,
                        logger);
                }

                return Describe(outcome);
            }, "like comment", logger))
            .RequireAuthorization();

        app.MapDelete("/api/comments/{id:int}/likes", (
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

                return await client.UnlikeCommentAsync(id, userId.Value, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }, "unlike comment", logger))
            .RequireAuthorization();

        return app;
    }
}
