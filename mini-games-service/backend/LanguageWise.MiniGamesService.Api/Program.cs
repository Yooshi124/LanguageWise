using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;

const string ServiceName = "mini-games-service-backend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILearningContextProvider, FakeLearningContextProvider>();
builder.Services.AddSingleton(serviceProvider => new GuessTheWordService(
    "English",
    serviceProvider.GetRequiredService<ILearningContextProvider>()));
builder.Services.AddSingleton(new AssociationsService("English"));
builder.Services.AddSingleton(new WordSearchService("English"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/guess-the-word", (GuessTheWordService service) =>
    Results.Ok(service.GetState()));

app.MapPost("/api/guess-the-word/guess", (GuessTheWordGuessRequest request, GuessTheWordService service) =>
{
    try
    {
        return Results.Ok(service.SubmitGuess(request.Guess));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["guess"] = [exception.Message]
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/guess-the-word/reset", (GuessTheWordService service) =>
{
    service.ResetGame();
    return Results.NoContent();
});

app.MapGet("/api/word-search", (WordSearchService service) => Results.Ok(service.GetState()));

app.MapPost("/api/word-search/guess", (WordSearchGuessRequest request, WordSearchService service) =>
{
    try
    {
        return Results.Ok(service.SubmitWord(request.Word, request.Indices));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["word"] = [exception.Message] });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/hint", (WordSearchService service) =>
{
    try
    {
        return Results.Ok(service.UseHint());
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/give-up", (WordSearchService service) => Results.Ok(service.GiveUp()));

app.MapPost("/api/word-search/reset", (WordSearchService service) =>
{
    service.ResetGame();
    return Results.NoContent();
});

app.MapGet("/api/associations", (AssociationsService service) => Results.Ok(service.GetState()));

app.MapPost("/api/associations/guess", (AssociationsGuessRequest request, AssociationsService service) =>
{
    try
    {
        return Results.Ok(service.SubmitGuess(request.Words));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["words"] = [exception.Message] });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/associations/reset", (AssociationsService service) =>
{
    service.ResetGame();
    return Results.NoContent();
});

app.Run();
