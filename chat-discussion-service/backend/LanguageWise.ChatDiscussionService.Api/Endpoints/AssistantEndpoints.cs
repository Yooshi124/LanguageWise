using System.Net;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Services;

namespace LanguageWise.ChatDiscussionService.Api.Endpoints;

internal static class AssistantEndpoints
{
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

        return app;
    }
}
