using LanguageWise.Student3.Api.Clients;
using LanguageWise.Student3.Api.Rendering;

const string ServiceName = "student-3-backend";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6003";

builder.Services.AddHttpClient<SampleItemsClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/sample-items", async (SampleItemsClient client, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await client.GetAllAsync(cancellationToken));
    }
    catch (Exception exception)
    {
        return Results.Problem(
            title: "The database microservice is unavailable.",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// HTMX target: returns table rows rather than JSON so the browser can swap them straight in.
app.MapGet("/api/sample-items/fragment", async (SampleItemsClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var items = await client.GetAllAsync(cancellationToken);
        return Results.Content(SampleItemHtmlRenderer.RenderRows(items), "text/html");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to load sample items from {Url}.", databaseServiceUrl);

        return Results.Content(
            SampleItemHtmlRenderer.RenderError("The database microservice is unavailable."),
            "text/html",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
