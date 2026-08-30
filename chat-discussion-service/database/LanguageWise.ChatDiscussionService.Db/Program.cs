using LanguageWise.ChatDiscussionService.Db.Data;
using LanguageWise.ChatDiscussionService.Db.Models;

const string serviceName = "chat-discussion-service-db";

// This service owns the SQLite file and knows nothing about users or tokens.
// Callers pass userId and viewerId as plain parameters; authentication and
// authorisation are the backend's job.
var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["Database:Path"] ?? "data/chat-discussion-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(new SampleItemRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));
builder.Services.AddSingleton(new DiscussionRepository(connectionString));

var app = builder.Build();
app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (SampleItemRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = serviceName, items = repository.Count() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = serviceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// ---------------------------------------------------------------------------
// Posts
// ---------------------------------------------------------------------------

app.MapGet("/api/posts", (
    DiscussionRepository repository,
    int? userId = null,
    string? category = null,
    string? search = null,
    int limit = 20,
    int offset = 0,
    int? viewerId = null) =>
    Results.Ok(repository.GetPosts(userId, category, search, limit, offset, viewerId)));

app.MapGet("/api/posts/{id:int}", (int id, DiscussionRepository repository, int? viewerId = null) =>
    repository.GetPost(id, viewerId) is { } post ? Results.Ok(post) : Results.NotFound());

app.MapPost("/api/posts", (PostInput input, DiscussionRepository repository) =>
{
    var created = repository.CreatePost(input);
    return Results.Created($"/api/posts/{created.Id}", created);
});

app.MapPut("/api/posts/{id:int}", (int id, PostUpdate update, DiscussionRepository repository) =>
    repository.UpdatePost(id, update) is { } updated ? Results.Ok(updated) : Results.NotFound());

app.MapDelete("/api/posts/{id:int}", (int id, DiscussionRepository repository) =>
    repository.DeletePost(id) ? Results.NoContent() : Results.NotFound());

// ---------------------------------------------------------------------------
// Comments
// ---------------------------------------------------------------------------

app.MapGet("/api/posts/{postId:int}/comments", (
    int postId,
    DiscussionRepository repository,
    int limit = 100,
    int offset = 0,
    int? viewerId = null) =>
    Results.Ok(repository.GetComments(postId, limit, offset, viewerId)));

app.MapPost("/api/posts/{postId:int}/comments", (int postId, CommentInput input, DiscussionRepository repository) =>
{
    var created = repository.CreateComment(postId, input);
    return created is null
        ? Results.NotFound()
        : Results.Created($"/api/comments/{created.Id}", created);
});

// Not consumed by the browser: the backend reads a comment to check ownership
// before allowing an edit or a delete.
app.MapGet("/api/comments/{id:int}", (int id, DiscussionRepository repository) =>
    repository.GetComment(id) is { } comment ? Results.Ok(comment) : Results.NotFound());

app.MapPut("/api/comments/{id:int}", (int id, CommentUpdate update, DiscussionRepository repository) =>
    repository.UpdateComment(id, update) is { } updated ? Results.Ok(updated) : Results.NotFound());

app.MapDelete("/api/comments/{id:int}", (int id, DiscussionRepository repository) =>
    repository.DeleteComment(id) ? Results.NoContent() : Results.NotFound());

// ---------------------------------------------------------------------------
// Likes
// ---------------------------------------------------------------------------

app.MapGet("/api/posts/{postId:int}/likes", (int postId, DiscussionRepository repository) =>
    Results.Ok(repository.GetPostLikes(postId)));

app.MapPost("/api/posts/{postId:int}/likes", (int postId, LikeInput input, DiscussionRepository repository) =>
    LikeResult(repository.LikePost(postId, input.UserId)));

app.MapDelete("/api/posts/{postId:int}/likes", (int postId, int userId, DiscussionRepository repository) =>
    repository.UnlikePost(postId, userId) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/comments/{commentId:int}/likes", (int commentId, DiscussionRepository repository) =>
    Results.Ok(repository.GetCommentLikes(commentId)));

app.MapPost("/api/comments/{commentId:int}/likes", (int commentId, LikeInput input, DiscussionRepository repository) =>
    LikeResult(repository.LikeComment(commentId, input.UserId)));

app.MapDelete("/api/comments/{commentId:int}/likes", (int commentId, int userId, DiscussionRepository repository) =>
    repository.UnlikeComment(commentId, userId) ? Results.NoContent() : Results.NotFound());

// ---------------------------------------------------------------------------
// Images: retained unchanged. Uploads are deliberately not implemented yet.
// ---------------------------------------------------------------------------

app.MapGet("/api/images", (DiscussionRepository repository) => Results.Ok(repository.GetImages()));

app.Run();

static IResult LikeResult(LikeOutcome outcome) => outcome switch
{
    LikeOutcome.Created => Results.StatusCode(StatusCodes.Status201Created),
    LikeOutcome.Duplicate => Results.Conflict(new { error = "That like already exists." }),
    _ => Results.NotFound()
};
