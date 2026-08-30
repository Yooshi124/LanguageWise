using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LanguageWise.ChatDiscussionService.Api;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "chat-discussion-service-backend";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6002";

builder.Services.AddHttpClient<DiscussionClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Tokens are minted by shared-backend and signed with the private half of this
// key pair. This service only ever verifies them.
var verificationKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(verificationKeyPath));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Leave 'sub' alone. With the default mapping it is renamed to
        // NameIdentifier and DiscussionRules.GetUserId silently returns null.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = JwtRegisteredClaimNames.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies["token"];
                }

                return Task.CompletedTask;
            }
        };
    });

// Deliberately no fallback policy: reading the forum works signed out, and each
// write opts in with RequireAuthorization instead.
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/forums", () => Results.Ok(DiscussionRules.Forums));

app.MapGet("/api/me", (HttpContext context) =>
{
    var userId = DiscussionRules.GetUserId(context.User);
    return userId is null
        ? Results.Unauthorized()
        : Results.Ok(new Me(userId.Value, DiscussionRules.GetUserName(context.User)));
})
    .RequireAuthorization();

// ---------------------------------------------------------------------------
// Reads. Anonymous callers are welcome; a signed-in one additionally gets
// likedByViewer populated so the like button renders correctly on first paint.
// ---------------------------------------------------------------------------

app.MapGet("/api/posts", (
    HttpContext context,
    DiscussionClient client,
    CancellationToken cancellationToken,
    int? userId = null,
    string? category = null,
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

        var filter = DiscussionRules.ValidateCategoryFilter(category);
        if (filter.Count > 0)
        {
            return Results.ValidationProblem(filter);
        }

        if (sort is not null && !string.Equals(sort, "newest", StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sort"] = ["The only supported sort is 'newest'."]
            });
        }

        var posts = await client.GetPostsAsync(
            userId,
            category,
            q,
            limit,
            offset,
            DiscussionRules.GetUserId(context.User),
            cancellationToken);

        return Results.Ok(posts);
    }, "list posts"));

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

        await Task.WhenAll(postTask, commentsTask);

        var post = await postTask;
        if (post is null)
        {
            return Results.NotFound();
        }

        var comments = await commentsTask;

        return Results.Ok(new PostDetail(
            post.Id,
            post.UserId,
            post.AuthorName,
            post.Title,
            post.Content,
            post.Category,
            post.CreatedAt,
            post.UpdatedAt,
            post.CommentCount,
            post.LikeCount,
            post.LikedByViewer,
            comments,
            post.CommentCount > comments.Count));
    }, "read post"));

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

        var comments = await client.GetCommentsAsync(
            id,
            limit,
            offset,
            DiscussionRules.GetUserId(context.User),
            cancellationToken);

        return Results.Ok(comments);
    }, "list comments"));

app.MapGet("/api/posts/{id:int}/likes", (int id, DiscussionClient client, CancellationToken cancellationToken) =>
    Guard(async () => Results.Ok(await client.GetPostLikesAsync(id, cancellationToken)), "list post likes"));

app.MapGet("/api/comments/{id:int}/likes", (int id, DiscussionClient client, CancellationToken cancellationToken) =>
    Guard(async () => Results.Ok(await client.GetCommentLikesAsync(id, cancellationToken)), "list comment likes"));

// ---------------------------------------------------------------------------
// Writes. The author is always the token's subject, never a value from the body.
// ---------------------------------------------------------------------------

app.MapPost("/api/posts", (
    HttpContext context,
    CreatePostRequest? request,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var errors = DiscussionRules.ValidateCreatePost(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var created = await client.CreatePostAsync(
            userId.Value,
            DiscussionRules.GetUserName(context.User),
            request!.Title!.Trim(),
            request.Content!.Trim(),
            request.Category!.Trim(),
            cancellationToken);

        return Results.Created($"/api/posts/{created.Id}", created);
    }, "create post"))
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

        var errors = DiscussionRules.ValidatePatchPost(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var current = await client.GetPostAsync(id, null, cancellationToken);
        if (current is null)
        {
            return Results.NotFound();
        }

        if (current.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        // The post is already loaded, so the partial update is folded over it here
        // and the database service still performs a plain full-row replacement.
        var (title, content, category) = DiscussionRules.MergePost(current, request!);
        var updated = await client.UpdatePostAsync(id, title, content, category, cancellationToken);

        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }, "update post"))
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
        if (current is null)
        {
            return Results.NotFound();
        }

        if (current.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        // Comments, likes and images below the post go with it, by ON DELETE CASCADE.
        return await client.DeletePostAsync(id, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }, "delete post"))
    .RequireAuthorization();

app.MapPost("/api/posts/{id:int}/comments", (
    int id,
    HttpContext context,
    CreateCommentRequest? request,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        if (userId is null)
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
            DiscussionRules.GetUserName(context.User),
            request!.Content!.Trim(),
            cancellationToken);

        return created is null
            ? Results.NotFound()
            : Results.Created($"/api/comments/{created.Id}", created);
    }, "create comment"))
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
        if (current is null)
        {
            return Results.NotFound();
        }

        if (current.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        var updated = await client.UpdateCommentAsync(
            id,
            DiscussionRules.MergeComment(current, request!),
            cancellationToken);

        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }, "update comment"))
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
        if (current is null)
        {
            return Results.NotFound();
        }

        if (current.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        return await client.DeleteCommentAsync(id, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }, "delete comment"))
    .RequireAuthorization();

// ---------------------------------------------------------------------------
// Likes. A like belongs to the caller, so the target comes from the route and
// the owner from the token; no like ID ever reaches the client.
// ---------------------------------------------------------------------------

app.MapPost("/api/posts/{id:int}/likes", (
    int id,
    HttpContext context,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        return userId is null
            ? Results.Unauthorized()
            : Describe(await client.LikePostAsync(id, userId.Value, cancellationToken));
    }, "like post"))
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
    }, "unlike post"))
    .RequireAuthorization();

app.MapPost("/api/comments/{id:int}/likes", (
    int id,
    HttpContext context,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        return userId is null
            ? Results.Unauthorized()
            : Describe(await client.LikeCommentAsync(id, userId.Value, cancellationToken));
    }, "like comment"))
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
    }, "unlike comment"))
    .RequireAuthorization();

// TODO: when the notification contract with the quests service is agreed, a
// successful like or comment above should also POST a 'post-engagement' event
// to quests-achievements-notifications-service-backend on behalf of the post's
// author. Left out on purpose: that service owns the achievement IDs involved.

app.Run();

// Every endpoint routes its database call through here so that an unreachable
// database service becomes a 503 rather than an unhandled exception.
async Task<IResult> Guard(Func<Task<IResult>> action, string operation)
{
    try
    {
        return await action();
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        app.Logger.LogError(exception, "Failed to {Operation}.", operation);
        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static IResult Describe(LikeOutcome outcome) => outcome switch
{
    LikeOutcome.Created => Results.StatusCode(StatusCodes.Status201Created),
    LikeOutcome.Duplicate => Results.Conflict(new { error = "You have already liked this." }),
    _ => Results.NotFound()
};

public partial class Program;
