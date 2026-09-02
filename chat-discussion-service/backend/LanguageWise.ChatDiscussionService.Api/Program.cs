using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using LanguageWise.ChatDiscussionService.Api;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Options;
using LanguageWise.ChatDiscussionService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "chat-discussion-service-backend";
const string AssistantRateLimitPolicy = "assistant-per-user";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6002";

builder.Services.AddHttpClient<DiscussionClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// AI mode. The model runs in the shared 'ollama' container, so there is nothing
// to configure beyond its address, which resolves by container name inside
// Docker exactly as the database address above does.
var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";

builder.Services
    .AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Model),
        "Ollama:Model is required.")
    .Validate(
        options => options.MaxOutputTokens is > 0 and <= 8192,
        "Ollama:MaxOutputTokens must be between 1 and 8192.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IAssistantCompletionClient, OllamaAssistantClient>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");

    // No timeout: the response is a stream that stays open for as long as the
    // model keeps writing, and the first token after a cold start is slow.
    // HttpClient's default would abort a long answer part-way.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddScoped<IAssistantContextService, AssistantContextService>();

// The model is metered, so one signed-in user cannot spend the whole allowance.
// Partitioned by 'sub' rather than IP: everyone here is signed in anyway, and a
// shared campus address should not be one bucket.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            await Results.Problem(
                title: "Too many assistant requests.",
                detail: "Please wait a moment before sending another question.",
                statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(context.HttpContext);
        }
    };
    options.AddPolicy(AssistantRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
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
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/forums", (DiscussionClient client, CancellationToken cancellationToken) =>
    Guard(async () => Results.Ok(await client.GetForumsAsync(cancellationToken)), "read forums"));

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

        var posts = await client.GetPostsAsync(
            userId,
            forumCode,
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

        var commentsTask = client.GetCommentsAsync(
            id,
            limit,
            offset,
            DiscussionRules.GetUserId(context.User),
            cancellationToken);
        var imagesTask = client.GetPostCommentImagesAsync(id, cancellationToken);

        await Task.WhenAll(commentsTask, imagesTask);

        return Results.Ok(Attach(await commentsTask, await imagesTask));
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

        var errors = DiscussionRules.ValidateCreatePost(
            request,
            await client.GetForumsAsync(cancellationToken));
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var created = await client.CreatePostAsync(
            userId.Value,
            DiscussionRules.GetUserName(context.User),
            request!.Title!.Trim(),
            request.Content!.Trim(),
            request.ForumCode!.Trim(),
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

        var errors = DiscussionRules.ValidatePatchPost(
            request,
            await client.GetForumsAsync(cancellationToken));
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
        var (title, content, forumCode) = DiscussionRules.MergePost(current, request!);
        var updated = await client.UpdatePostAsync(id, title, content, forumCode, cancellationToken);

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

// ---------------------------------------------------------------------------
// Images. Adding or removing one is an edit of the post or comment it hangs off,
// so it is restricted to that author; reading is open to everyone.
//
// The bytes are proxied rather than redirected to: the database service is not
// reachable from the browser, and keeping it that way is the point of the tier.
// ---------------------------------------------------------------------------

app.MapGet("/api/posts/{id:int}/images", (
    int id,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var images = await client.GetPostImagesAsync(id, cancellationToken);
        return Results.Ok(images.Select(ToAttachedImage));
    }, "list post images"));

app.MapGet("/api/comments/{id:int}/images", (
    int id,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var images = await client.GetCommentImagesAsync(id, cancellationToken);
        return Results.Ok(images.Select(ToAttachedImage));
    }, "list comment images"));

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

        // An image is immutable: replacing one means a new ID, never new bytes behind
        // this one, so the browser may cache it indefinitely.
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        context.Response.Headers.XContentTypeOptions = "nosniff";

        return Results.File(image.Bytes, image.ContentType);
    }, "read image"));

app.MapPost("/api/posts/{id:int}/images", (
    int id,
    HttpContext context,
    IFormFile? file,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var post = await client.GetPostAsync(id, null, cancellationToken);
        if (post is null)
        {
            return Results.NotFound();
        }

        if (post.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        var existing = await client.GetPostImagesAsync(id, cancellationToken);
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
        var created = await client.UploadPostImageAsync(
            id,
            content,
            ImageRules.Normalise(file.ContentType),
            ImageRules.SafeFileName(file.FileName),
            cancellationToken);

        return created is null
            ? Results.NotFound()
            : Results.Created($"/api/images/{created.Id}/content", ToAttachedImage(created));
    }, "upload post image"))
    .RequireAuthorization()
    // Minimal APIs demand an antiforgery token for form uploads by default; this
    // service reads its token explicitly, so there is no ambient credential to forge.
    .DisableAntiforgery();

app.MapPost("/api/comments/{id:int}/images", (
    int id,
    HttpContext context,
    IFormFile? file,
    DiscussionClient client,
    CancellationToken cancellationToken) =>
    Guard(async () =>
    {
        var userId = DiscussionRules.GetUserId(context.User);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var comment = await client.GetCommentAsync(id, cancellationToken);
        if (comment is null)
        {
            return Results.NotFound();
        }

        if (comment.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        var existing = await client.GetCommentImagesAsync(id, cancellationToken);
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
        var created = await client.UploadCommentImageAsync(
            id,
            content,
            ImageRules.Normalise(file.ContentType),
            ImageRules.SafeFileName(file.FileName),
            cancellationToken);

        return created is null
            ? Results.NotFound()
            : Results.Created($"/api/images/{created.Id}/content", ToAttachedImage(created));
    }, "upload comment image"))
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

        // An image is owned by whoever owns the post or comment it hangs off.
        var owner = image.PostId is { } postId
            ? (await client.GetPostAsync(postId, null, cancellationToken))?.UserId
            : image.CommentId is { } commentId
                ? (await client.GetCommentAsync(commentId, cancellationToken))?.UserId
                : null;

        if (owner is null)
        {
            return Results.NotFound();
        }

        if (owner != userId.Value)
        {
            return Results.Forbid();
        }

        return await client.DeleteImageAsync(id, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }, "delete image"))
    .RequireAuthorization();

// ---------------------------------------------------------------------------
// AI mode. A help chatbot that answers questions about the forum itself, such
// as how to create or edit a post.
//
// Sign-in is required: not because the answers are private, but because the
// model is a shared, expensive resource and every other route already redirects
// signed-out visitors to the login page.
//
// Guard is deliberately not used here. It reports an unreachable *database*,
// which is the wrong thing to say about the assistant: the answer comes from
// Ollama and the help topics it is grounded in are compiled in.
//
// The answer is streamed as server-sent events, so every failure that can happen
// before the first byte is a normal problem response, and everything after it is
// an 'error' event inside the stream. See AssistantSseResult.
//
// A missing model is the exception: it degrades rather than failing, streaming
// the retrieved help text in place of an answer. That only works before the
// first fragment is sent — a model that dies part-way has already put half an
// answer on screen, and swapping in the help text underneath it would be worse
// than saying the response was interrupted.
// ---------------------------------------------------------------------------

app.MapGet("/api/assistant/topics", () =>
    Results.Ok(HelpKnowledgeBase.Articles.Select(article => new { article.Id, article.Title })))
    .RequireAuthorization();

app.MapPost("/api/assistant/messages", async (
    AssistantMessageRequest? request,
    AssistantRequestValidator validator,
    IAssistantContextService contextService,
    IAssistantPromptBuilder promptBuilder,
    IAssistantCompletionClient completionClient,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var validation = validator.Validate(request);
    if (validation.Request is null)
    {
        return Results.ValidationProblem(
            validation.Errors.ToDictionary(error => error.Key, error => error.Value));
    }

    var assistantContext = await contextService.GetContextAsync(validation.Request, cancellationToken);
    var logger = loggerFactory.CreateLogger<AssistantSseResult>();

    // No model is an expected condition, not an error: Ollama may not be running,
    // or its model may never have been pulled. Either way the retrieved help text
    // already answers "how do I create a post", so it is streamed instead and the
    // browser labels it as coming from the help pages.
    IAssistantEventStream completion;
    try
    {
        var messages = promptBuilder.BuildMessages(
            validation.Request,
            assistantContext.CanonicalContext);

        completion = await completionClient.StartCompletionAsync(messages, cancellationToken);
    }
    catch (AssistantProviderException exception) when (
        exception.StatusCode == HttpStatusCode.NotFound)
    {
        app.Logger.LogWarning(
            "The assistant model is not installed; answering from the help topics alone.");

        completion = new HelpTextEventStream(assistantContext.FallbackAnswer);
    }
    catch (Exception exception) when (
        exception is HttpRequestException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
    {
        app.Logger.LogWarning(
            exception,
            "The assistant model was unreachable; answering from the help topics alone.");

        completion = new HelpTextEventStream(assistantContext.FallbackAnswer);
    }
    catch (AssistantProviderException exception)
    {
        // A model that is present and refuses is a real fault rather than an
        // absent one, so it is reported instead of being papered over.
        app.Logger.LogWarning(
            "The assistant model rejected a request with HTTP status {HttpStatus}.",
            (int)exception.StatusCode);

        return Results.Problem(
            title: "The assistant model refused the request.",
            detail: "The assistant could not start a response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }

    return new AssistantSseResult(completion, logger);
})
    .RequireAuthorization()
    .RequireRateLimiting(AssistantRateLimitPolicy);

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

// The storage key and the owning ID are dropped: the browser addresses an image by
// its own ID, nested inside the post or comment it arrived with.
static AttachedImage ToAttachedImage(Image image) => new(
    image.Id,
    image.FileName,
    image.ContentType,
    image.SizeBytes,
    image.UploadedAt);

// The images arrive as one flat list for the whole post, so they are grouped here.
static IReadOnlyList<CommentDetail> Attach(
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

// The upload opens the stream a second time. IFormFile buffers the part, so that
// costs nothing and beats leaving the upload to trust a rewind.
static async Task<bool> LooksLikeDeclaredFormatAsync(IFormFile file, CancellationToken cancellationToken)
{
    var header = new byte[ImageRules.SignatureLength];

    await using var stream = file.OpenReadStream();
    var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

    return ImageRules.MatchesContentType(file.ContentType, header.AsSpan(0, read));
}

static IResult Describe(LikeOutcome outcome) => outcome switch
{
    LikeOutcome.Created => Results.StatusCode(StatusCodes.Status201Created),
    LikeOutcome.Duplicate => Results.Conflict(new { error = "You have already liked this." }),
    _ => Results.NotFound()
};

public partial class Program;
