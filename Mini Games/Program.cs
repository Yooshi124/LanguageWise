var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "frontend/dist"
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/gamepage", () => Results.File(Path.Combine(app.Environment.WebRootPath!, "index.html"), "text/html"));
app.MapGet("/vocab-voyage", () => Results.File(Path.Combine(app.Environment.WebRootPath!, "index.html"), "text/html"));

app.Run();
