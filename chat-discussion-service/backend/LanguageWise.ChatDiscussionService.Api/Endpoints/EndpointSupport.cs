using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class EndpointSupport
{
    internal static ILogger CreateLogger(IEndpointRouteBuilder app) =>
        app.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("LanguageWise.ChatDiscussionService.Api.Endpoints");

    internal static async Task<IResult> Guard(Func<Task<IResult>> action, string operation, ILogger logger)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to {Operation}.", operation);
            return Results.Problem(
                title: "The database microservice is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    internal static IResult? RequireOwner(int callerId, int? ownerId) =>
        ownerId is null ? Results.NotFound()
        : ownerId != callerId ? Results.Forbid()
        : null;

    internal static async Task<IResult> UploadImageAsync(
        HttpContext context,
        IFormFile? file,
        Func<Task<int?>> findOwnerId,
        Func<Task<IReadOnlyList<Image>>> getExisting,
        Func<Stream, string, string, Task<Image?>> upload,
        CancellationToken cancellationToken)
    {
        var userId = DiscussionRules.GetUserId(context.User);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (RequireOwner(userId.Value, await findOwnerId()) is { } denied)
        {
            return denied;
        }

        var existing = await getExisting();
        var errors = ImageRules.ValidateUpload(file?.ContentType, file?.Length ?? 0, existing.Count);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!await LooksLikeDeclaredFormatAsync(file!, cancellationToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["That file is not the image format it claims to be."]
            });
        }

        await using var content = file!.OpenReadStream();
        var created = await upload(
            content,
            ImageRules.Normalise(file.ContentType),
            ImageRules.SafeFileName(file.FileName));

        return created is null
            ? Results.NotFound()
            : Results.Created($"/api/images/{created.Id}/content", ToAttachedImage(created));
    }

    internal static async Task RecordContributionAsync(
        AchievementEventsClient client,
        HttpContext context,
        int userId,
        string subject,
        CancellationToken cancellationToken,
        ILogger logger)
    {
        try
        {
            await client.RecordContributionAsync(
                userId,
                DiscussionRules.GetUserName(context.User),
                subject,
                GetAccessToken(context),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to record community contribution for user {UserId}.", userId);
        }
    }

    internal static async Task RecordPostEngagementAsync<T>(
        AchievementEventsClient achievementEventsClient,
        HttpContext context,
        int actorUserId,
        Func<Task<T?>> getTarget,
        string subject,
        CancellationToken cancellationToken,
        ILogger logger) where T : class
    {
        try
        {
            var target = await getTarget();
            var recipient = target switch
            {
                PostSummary post => (post.UserId, post.AuthorName),
                Comment comment => (comment.UserId, comment.AuthorName),
                _ => default
            };
            if (recipient.UserId <= 0 || recipient.UserId == actorUserId)
            {
                return;
            }

            await achievementEventsClient.RecordPostEngagementAsync(
                recipient.UserId,
                recipient.AuthorName,
                subject,
                GetAccessToken(context),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to record post engagement.");
        }
    }

    internal static AttachedImage ToAttachedImage(Image image) => new(
        image.Id,
        image.FileName,
        image.ContentType,
        image.SizeBytes,
        image.UploadedAt);

    internal static IReadOnlyList<CommentDetail> Attach(
        IReadOnlyList<CommentSummary> comments,
        IReadOnlyList<Image> images)
    {
        var byComment = images
            .Where(image => image.CommentId is not null)
            .GroupBy(image => image.CommentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AttachedImage>)[.. group.Select(ToAttachedImage)]);

        return
        [
            .. comments.Select(comment => new CommentDetail(
                comment.Id,
                comment.PostId,
                comment.UserId,
                comment.AuthorName,
                comment.Content,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.LikeCount,
                comment.LikedByViewer,
                byComment.TryGetValue(comment.Id, out var attached) ? attached : []))
        ];
    }

    internal static IResult Describe(LikeOutcome outcome) => outcome switch
    {
        LikeOutcome.Created => Results.StatusCode(StatusCodes.Status201Created),
        LikeOutcome.Duplicate => Results.Conflict(new { error = "You have already liked this." }),
        _ => Results.NotFound()
    };

    private static string GetAccessToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : context.Request.Cookies["token"]!;
    }

    private static async Task<bool> LooksLikeDeclaredFormatAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[ImageRules.SignatureLength];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        return ImageRules.MatchesContentType(file.ContentType, header.AsSpan(0, read));
    }
}
