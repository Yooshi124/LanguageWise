using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Services;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class AssistantEndpoints
{
    private const string AssistantToolNamePattern = "^chat_[a-z_]{1,59}$";
    private const int MaxAssistantToolArgumentBytes = 2048;

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = EndpointSupport.CreateLogger(app);

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
            var sseLogger = loggerFactory.CreateLogger<AssistantSseResult>();

            IAssistantEventStream completion;
            try
            {
                var messages = promptBuilder.BuildMessages(
                    validation.Request,
                    assistantContext.CanonicalContext);

                completion = await completionClient.StartCompletionAsync(messages, cancellationToken);
            }
            catch (AssistantProviderException exception) when (
                exception.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                logger.LogWarning(
                    "Garry could not start a response; answering from the help topics alone.");

                completion = new HelpTextEventStream(assistantContext.FallbackAnswer);
            }
            catch (AssistantProviderException exception) when (
                exception.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return Results.Problem(
                    title: "Too many assistant requests.",
                    detail: "Please wait before sending another question.",
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (Exception exception) when (
                exception is HttpRequestException
                || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning(
                    exception,
                    "The assistant model was unreachable; answering from the help topics alone.");

                completion = new HelpTextEventStream(assistantContext.FallbackAnswer);
            }
            catch (AssistantProviderException exception)
            {
                logger.LogWarning(
                    "The assistant model rejected a request with HTTP status {HttpStatus}.",
                    (int)exception.StatusCode);

                return Results.Problem(
                    title: "The assistant model refused the request.",
                    detail: "The assistant could not start a response. Please try again.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            return new AssistantSseResult(completion, sseLogger);
        })
            .RequireAuthorization();

        app.MapGet("/api/assistant/tools", async (
            IMcpToolClient mcpTools,
            HttpRequest httpRequest,
            CancellationToken cancellationToken) =>
        {
            if (!mcpTools.Enabled)
            {
                return AssistantToolsDisabled();
            }

            try
            {
                var tools = await mcpTools.ListToolsAsync(UserTokenReader.Read(httpRequest), cancellationToken);
                return Results.Ok(new AssistantToolsResponse(tools
                    .Select(tool => new AssistantToolDescriptor(tool.Name, tool.Title, tool.Description))
                    .ToList()));
            }
            catch (Exception exception) when (IsAssistantToolFailure(exception, cancellationToken))
            {
                logger.LogWarning(
                    "Assistant tool listing failed with error type {ErrorType}.",
                    exception.GetType().Name);
                return AssistantToolsUnavailable();
            }
        })
            .RequireAuthorization();

        app.MapPost("/api/assistant/tools/{name}", async (
            string name,
            HttpRequest httpRequest,
            IMcpToolClient mcpTools,
            CancellationToken cancellationToken) =>
        {
            if (!mcpTools.Enabled)
            {
                return AssistantToolsDisabled();
            }

            if (!Regex.IsMatch(name, AssistantToolNamePattern))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Unknown assistant tool."]
                });
            }

            var arguments = await ReadAssistantToolArgumentsAsync(httpRequest, cancellationToken);
            if (arguments is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["arguments"] = [$"Tool arguments must be a JSON object of at most {MaxAssistantToolArgumentBytes} bytes."]
                });
            }

            try
            {
                var result = await mcpTools.CallToolAsync(
                    name,
                    arguments,
                    UserTokenReader.Read(httpRequest),
                    cancellationToken);
                return Results.Ok(new AssistantToolCallResponse(name, result.IsError, result.Result));
            }
            catch (ModelContextProtocol.McpProtocolException exception) when (
                exception.ErrorCode is ModelContextProtocol.McpErrorCode.InvalidParams or ModelContextProtocol.McpErrorCode.MethodNotFound)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Unknown assistant tool or invalid arguments."]
                });
            }
            catch (Exception exception) when (IsAssistantToolFailure(exception, cancellationToken))
            {
                logger.LogWarning(
                    "Assistant tool call failed with error type {ErrorType}.",
                    exception.GetType().Name);
                return AssistantToolsUnavailable();
            }
        })
            .RequireAuthorization();

        return app;
    }

    private static IResult AssistantToolsDisabled() =>
        Results.Problem(
            title: "Assistant tools are disabled.",
            detail: "Garry's tools are not enabled on this server.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["code"] = "mcp_disabled" });

    private static IResult AssistantToolsUnavailable() =>
        Results.Problem(
            title: "Assistant tools are unavailable.",
            detail: "Garry's tools could not be reached. Please try again.",
            statusCode: StatusCodes.Status502BadGateway,
            extensions: new Dictionary<string, object?> { ["code"] = "mcp_unavailable" });

    private static bool IsAssistantToolFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or ModelContextProtocol.McpException or TimeoutException or IOException or JsonException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private static async Task<IReadOnlyDictionary<string, JsonElement>?> ReadAssistantToolArgumentsAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxAssistantToolArgumentBytes + 1];
        var length = 0;
        int read;
        while (length < buffer.Length &&
            (read = await request.Body.ReadAsync(buffer.AsMemory(length), cancellationToken)) > 0)
        {
            length += read;
        }

        if (length > MaxAssistantToolArgumentBytes)
        {
            return null;
        }

        if (length == 0 || buffer.AsSpan(0, length).Trim(" \t\r\n"u8).IsEmpty)
        {
            return new Dictionary<string, JsonElement>();
        }

        try
        {
            using var document = JsonDocument.Parse(buffer.AsMemory(0, length));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone());
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
