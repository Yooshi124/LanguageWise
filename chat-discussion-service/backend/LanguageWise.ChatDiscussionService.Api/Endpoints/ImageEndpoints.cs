using LanguageWise.ChatDiscussionService.Api.Clients;
using static LanguageWise.ChatDiscussionService.Api.Endpoints.EndpointSupport;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = CreateLogger(app);

        app.MapGet("/api/posts/{id:int}/images", (
            int id,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var images = await client.GetPostImagesAsync(id, cancellationToken);
                return Results.Ok(images.Select(ToAttachedImage));
            }, "list post images", logger));

        app.MapGet("/api/comments/{id:int}/images", (
            int id,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var images = await client.GetCommentImagesAsync(id, cancellationToken);
                return Results.Ok(images.Select(ToAttachedImage));
            }, "list comment images", logger));

        app.MapGet("/api/images/{id:int}/content", (
            int id,
            HttpContext context,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(async () =>
            {
                var image = await client.DownloadImageAsync(id, cancellationToken);

                if (image is null)
                {
                    return Results.NotFound();
                }

                context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                context.Response.Headers.XContentTypeOptions = "nosniff";

                return Results.File(image.Bytes, image.ContentType);
            }, "read image", logger));

        app.MapPost("/api/posts/{id:int}/images", (
            int id,
            HttpContext context,
            IFormFile? file,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(() => UploadImageAsync(
                context,
                file,
                async () => (await client.GetPostAsync(id, null, cancellationToken))?.UserId,
                () => client.GetPostImagesAsync(id, cancellationToken),
                (content, contentType, fileName) =>
                    client.UploadPostImageAsync(id, content, contentType, fileName, cancellationToken),
                cancellationToken), "upload post image", logger))
            .RequireAuthorization()
            .DisableAntiforgery();

        app.MapPost("/api/comments/{id:int}/images", (
            int id,
            HttpContext context,
            IFormFile? file,
            DiscussionClient client,
            CancellationToken cancellationToken) =>
            Guard(() => UploadImageAsync(
                context,
                file,
                async () => (await client.GetCommentAsync(id, cancellationToken))?.UserId,
                () => client.GetCommentImagesAsync(id, cancellationToken),
                (content, contentType, fileName) =>
                    client.UploadCommentImageAsync(id, content, contentType, fileName, cancellationToken),
                cancellationToken), "upload comment image", logger))
            .RequireAuthorization()
            .DisableAntiforgery();

        app.MapDelete("/api/images/{id:int}", (
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

                var image = await client.GetImageAsync(id, cancellationToken);
                if (image is null)
                {
                    return Results.NotFound();
                }

                async Task<int?> FindOwnerId()
                {
                    if (image.PostId is { } postId)
                    {
                        return (await client.GetPostAsync(postId, null, cancellationToken))?.UserId;
                    }

                    if (image.CommentId is { } commentId)
                    {
                        return (await client.GetCommentAsync(commentId, cancellationToken))?.UserId;
                    }

                    return null;
                }

                if (RequireOwner(userId.Value, await FindOwnerId()) is { } denied)
                {
                    return denied;
                }

                return await client.DeleteImageAsync(id, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound();
            }, "delete image", logger))
            .RequireAuthorization();

        return app;
    }
}
