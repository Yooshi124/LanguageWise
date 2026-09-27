using LanguageWise.Shared.Db.Data;
const string ServiceName = "shared-db";

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/shared.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(new UserRepository(connectionString));
builder.Services.AddSingleton(new ImageStore(builder.Configuration["Images:Path"] ?? "data/images"));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (UserRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = ServiceName, users = repository.Count() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = ServiceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/users/verify", (LoginInput input, UserRepository users) =>
{
    if (string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
        return Results.Unauthorized();

    var userId = users.Verify(input.Username, input.Password);
    return userId is not null
        ? Results.Ok(new { authenticated = true, userId })
        : Results.Unauthorized();
});

app.MapPost("/api/users", (CreateUserInput input, UserRepository users) =>
{
    if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        return Results.BadRequest();

    var result = users.Create(input.Username, input.Password);
    return result.Status == UserAccountStatus.UsernameTaken
        ? Results.Conflict()
        : Results.Created($"/api/users/{result.Account!.Id}", result.Account);
});

app.MapPatch("/api/users/{userId:int}", (int userId, UpdateUserInput input, UserRepository users) =>
{
    if ((input.Username is null && input.Password is null)
        || (input.Username is not null && string.IsNullOrWhiteSpace(input.Username))
        || input.Password == string.Empty)
        return Results.BadRequest();

    var result = users.Update(userId, input.Username, input.Password);
    return result.Status switch
    {
        UserAccountStatus.NotFound => Results.NotFound(),
        UserAccountStatus.UsernameTaken => Results.Conflict(),
        _ => Results.Ok(result.Account),
    };
});

app.MapPost("/api/users/{userId:int}/login-streak", (int userId, UserRepository users) =>
{
    var value = users.RecordLogin(userId, DateOnly.FromDateTime(DateTime.UtcNow));
    return value is null ? Results.NoContent() : Results.Ok(new { value });
});

app.MapGet("/api/users/{userId:int}/profile-picture", (int userId, UserRepository users) =>
    users.GetProfilePicture(userId) is { } picture ? Results.Ok(picture) : Results.NotFound());

app.MapGet("/api/users/{userId:int}/profile-picture/content", (int userId, UserRepository users, ImageStore images) =>
{
    if (users.GetProfilePicture(userId) is not { } picture)
    {
        return Results.NotFound();
    }

    var content = images.Open(picture.StorageKey);
    return content is null ? Results.NotFound() : Results.Stream(content, picture.ContentType);
});

// Raw body, not multipart: the backend has already parsed and validated the browser's form.
app.MapPut("/api/users/{userId:int}/profile-picture", async (
    int userId,
    HttpRequest request,
    UserRepository users,
    ImageStore images,
    CancellationToken cancellationToken,
    string? fileName = null) =>
{
    var previous = users.GetProfilePicture(userId);
    var storageKey = ImageStore.NewKey();
    var sizeBytes = await images.SaveAsync(storageKey, request.Body, cancellationToken);

    var picture = users.SetProfilePicture(userId, new ProfilePictureInput(storageKey, fileName, request.ContentType, sizeBytes));
    if (picture is null)
    {
        images.Delete(storageKey);
        return Results.NotFound();
    }

    if (previous is not null)
    {
        images.Delete(previous.StorageKey);
    }

    return Results.Ok(picture);
});

app.MapDelete("/api/users/{userId:int}/profile-picture", (int userId, UserRepository users, ImageStore images) =>
{
    if (users.GetProfilePicture(userId) is not { } picture || !users.ClearProfilePicture(userId))
    {
        return Results.NotFound();
    }

    images.Delete(picture.StorageKey);
    return Results.NoContent();
});

app.Run();

internal sealed record LoginInput(string Username, string Password);

internal sealed record CreateUserInput(string Username, string Password);

internal sealed record UpdateUserInput(string? Username, string? Password);
