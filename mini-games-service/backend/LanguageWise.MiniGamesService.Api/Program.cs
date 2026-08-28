using LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

const string ServiceName = "mini-games-service-backend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILearningContextProvider, FakeLearningContextProvider>();
builder.Services.AddSingleton(serviceProvider => new VocabVoyageService(
    "English",
    serviceProvider.GetRequiredService<ILearningContextProvider>()));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/vocab-voyage", (VocabVoyageService service) =>
    Results.Ok(service.GetState()));

app.MapPost("/api/vocab-voyage/guess", (VocabVoyageGuessRequest request, VocabVoyageService service) =>
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

app.MapPost("/api/vocab-voyage/reset", (VocabVoyageService service) =>
{
    service.ResetGame();
    return Results.NoContent();
});

app.Run();
