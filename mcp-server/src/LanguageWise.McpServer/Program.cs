using LanguageWise.McpServer.Security;
using LanguageWise.McpServer.Tools;
using LanguageWise.McpServer.Tools.MiniGames;
using LanguageWise.McpServer.Tools.QuizzesCourses;

var builder = WebApplication.CreateBuilder(args);
var mcpPath = new PathString("/mcp");
var contentRoot = builder.Environment.ContentRootPath;

builder.Services.AddSingleton(ApiKeyOptions.Load(builder.Configuration, contentRoot, mcpPath));
builder.Services.AddSingleton(UserTokenValidator.FromConfiguration(builder.Configuration, contentRoot));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<McpCallerContext>();
builder.Services.AddScoped<DownstreamClient>();

var downstreamTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Downstream:TimeoutSeconds", 10));
builder.Services.AddHttpClient(QuizzesCoursesTools.ServiceName, client =>
{
	client.BaseAddress = new Uri((builder.Configuration["Services:QuizzesCourses"] ?? "http://localhost:5003").TrimEnd('/') + "/");
	client.Timeout = downstreamTimeout;
});
builder.Services.AddHttpClient(MiniGamesTools.ServiceName, client =>
{
	client.BaseAddress = new Uri((builder.Configuration["Services:MiniGames"] ?? "http://localhost:5001").TrimEnd('/') + "/");
	client.Timeout = downstreamTimeout;
});

builder.Services.AddMcpServer(options =>
	{
		options.ServerInfo = new() { Name = "languagewise-mcp", Version = "1.0.0" };
	})
	.WithHttpTransport(options => options.Stateless = true)
	.WithToolsFromAssembly()
	.WithRequestFilters(filters => filters
		.AddListToolsFilter(ToolScopeFilter.FilterList)
		.AddCallToolFilter(ToolScopeFilter.FilterCall));

var app = builder.Build();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapGet("/health", () => Results.Ok());
app.MapMcp(mcpPath);
app.Run();

public partial class Program;
