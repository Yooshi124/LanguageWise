using System.Text.Json;
using LanguageWise.ChatDiscussionService.Db.Clients;
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

// Uploaded bytes sit beside the SQLite file, so one volume holds the whole dataset.
var imagePath = builder.Configuration["Images:Path"] ?? "data/images";
builder.Services.AddSingleton(new ImageStore(imagePath));

// Inside Docker this resolves to the courses database service by container name.
var coursesServiceUrl = builder.Configuration["Services:Courses"] ?? "http://localhost:6003";
builder.Services.AddHttpClient<CourseCatalogClient>(client =>
{
    client.BaseAddress = new Uri(coursesServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

// Once, at start-up. An unreachable catalogue is not fatal: the forums already in
// the database still work, and the next restart tries again.
try
{
    var courses = await app.Services.GetRequiredService<CourseCatalogClient>().GetCoursesAsync();
    var sync = app.Services.GetRequiredService<DiscussionRepository>().SyncCourseForums(courses);
    app.Logger.LogInformation(
        "Synced {CourseCount} courses into forums: {Added} added, {Renamed} renamed.",
        courses.Count,
        sync.Added,
        sync.Renamed);
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
{
    app.Logger.LogWarning(
        exception,
        "The course catalogue was unreachable; forums are left as they are.");
}

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
// Forums
// ---------------------------------------------------------------------------

app.MapGet("/api/forums", (DiscussionRepository repository) =>
    Results.Ok(repository.GetForums()));

app.MapGet("/api/forums/{code}", (string code, DiscussionRepository repository) =>
    repository.GetForum(code) is { } forum ? Results.Ok(forum) : Results.NotFound());

// ---------------------------------------------------------------------------
// Posts
// ---------------------------------------------------------------------------

app.MapGet("/api/posts", (
    DiscussionRepository repository,
    int? userId = null,
    string? forumCode = null,
    string? search = null,
    int limit = 20,
    int offset = 0,
    int? viewerId = null) =>
    Results.Ok(repository.GetPosts(userId, forumCode, search, limit, offset, viewerId)));

app.MapGet("/api/posts/{id:int}", (int id, DiscussionRepository repository, int? viewerId = null) =>
    repository.GetPost(id, viewerId) is { } post ? Results.Ok(post) : Results.NotFound());

app.MapPost("/api/posts", (PostInput input, DiscussionRepository repository) =>
{
    var created = repository.CreatePost(input);
    return Results.Created($"/api/posts/{created.Id}", created);
});

app.MapPut("/api/posts/{id:int}", (int id, PostUpdate update, DiscussionRepository repository) =>
    repository.UpdatePost(id, update) is { } updated ? Results.Ok(updated) : Results.NotFound());

// The rows below a post go with it by ON DELETE CASCADE, but the files those rows
// named do not, so their keys are read before the cascade removes them.
app.MapDelete("/api/posts/{id:int}", (int id, DiscussionRepository repository, ImageStore images) =>
{
    var orphaned = repository.GetPostStorageKeys(id);

    if (!repository.DeletePost(id))
    {
        return Results.NotFound();
    }

    images.DeleteAll(orphaned);
    return Results.NoContent();
});

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

app.MapDelete("/api/comments/{id:int}", (int id, DiscussionRepository repository, ImageStore images) =>
{
    var orphaned = repository.GetCommentStorageKeys(id);

    if (!repository.DeleteComment(id))
    {
        return Results.NotFound();
    }

    images.DeleteAll(orphaned);
    return Results.NoContent();
});

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
// Images. The upload arrives as a raw body, not a multipart form: the backend has
// already parsed the browser's form and validated the file.
// ---------------------------------------------------------------------------

app.MapGet("/api/posts/{postId:int}/images", (int postId, DiscussionRepository repository) =>
    Results.Ok(repository.GetPostImages(postId)));

app.MapPost("/api/posts/{postId:int}/images", (
    int postId,
    HttpRequest request,
    DiscussionRepository repository,
    ImageStore images,
    CancellationToken cancellationToken,
    string? fileName = null) =>
    Store(request, images, fileName, cancellationToken,
        input => repository.CreatePostImage(postId, input)));

app.MapGet("/api/posts/{postId:int}/comment-images", (int postId, DiscussionRepository repository) =>
    Results.Ok(repository.GetImagesForPostComments(postId)));

app.MapGet("/api/comments/{commentId:int}/images", (int commentId, DiscussionRepository repository) =>
    Results.Ok(repository.GetCommentImages(commentId)));

app.MapPost("/api/comments/{commentId:int}/images", (
    int commentId,
    HttpRequest request,
    DiscussionRepository repository,
    ImageStore images,
    CancellationToken cancellationToken,
    string? fileName = null) =>
    Store(request, images, fileName, cancellationToken,
        input => repository.CreateCommentImage(commentId, input)));

app.MapGet("/api/images/{id:int}", (int id, DiscussionRepository repository) =>
    repository.GetImage(id) is { } image ? Results.Ok(image) : Results.NotFound());

app.MapGet("/api/images/{id:int}/content", (int id, DiscussionRepository repository, ImageStore images) =>
{
    if (repository.GetImage(id) is not { } image)
    {
        return Results.NotFound();
    }

    var content = images.Open(image.StorageKey);
    return content is null ? Results.NotFound() : Results.Stream(content, image.ContentType);
});

app.MapDelete("/api/images/{id:int}", (int id, DiscussionRepository repository, ImageStore images) =>
{
    if (repository.GetImage(id) is not { } image)
    {
        return Results.NotFound();
    }

    if (!repository.DeleteImage(id))
    {
        return Results.NotFound();
    }

    images.Delete(image.StorageKey);
    return Results.NoContent();
});

app.Run();

// The file is written before the row that names it, so a row never points at bytes
// that are not there. If the post or comment has since been deleted the insert fails
// on its foreign key, and the file just written is removed again.
static async Task<IResult> Store(
    HttpRequest request,
    ImageStore images,
    string? fileName,
    CancellationToken cancellationToken,
    Func<ImageInput, Image?> insert)
{
    var storageKey = ImageStore.NewKey();
    var sizeBytes = await images.SaveAsync(storageKey, request.Body, cancellationToken);

    var created = insert(new ImageInput(storageKey, fileName, request.ContentType, sizeBytes));

    if (created is null)
    {
        images.Delete(storageKey);
        return Results.NotFound();
    }

    return Results.Created($"/api/images/{created.Id}", created);
}

static IResult LikeResult(LikeOutcome outcome) => outcome switch
{
    LikeOutcome.Created => Results.StatusCode(StatusCodes.Status201Created),
    LikeOutcome.Duplicate => Results.Conflict(new { error = "That like already exists." }),
    _ => Results.NotFound()
};
