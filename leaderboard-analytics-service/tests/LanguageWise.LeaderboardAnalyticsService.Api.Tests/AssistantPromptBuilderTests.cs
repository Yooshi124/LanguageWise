using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using LanguageWise.LeaderboardAnalyticsService.Api.Services;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class AssistantPromptBuilderTests
{
    [Test]
    public void BuildMessages_IncludesTheAnalyticsProfileAndConversation()
    {
        var profile = new AnalyticsProfile(
            "justin",
            [new LanguageRanking(1, 7, "German", 12, 2, DateTime.UtcNow)],
            new LessonsCompletedResponse(
                7,
                new DateOnly(2026, 8, 8),
                new DateOnly(2026, 9, 6),
                [new LessonsCompletedSeries(
                    "de",
                    "German",
                    [new LessonsCompletedPoint(new DateOnly(2026, 9, 6), 12)])]));
        var request = new ValidatedAssistantRequest(
            "Which language am I ranked highest in?",
            [new AssistantHistoryMessage("assistant", "Let me look.")],
            new AssistantRouteContext("leaderboard-analytics-home"));

        var messages = new AssistantPromptBuilder().BuildMessages(request, profile);

        Assert.Multiple(() =>
        {
            Assert.That(messages[0].Content, Does.Contain("leaderboard and analytics"));
            Assert.That(messages[0].Content, Does.Contain("rank 1 is"));
            Assert.That(messages[1].Content, Does.Contain("\"username\":\"justin\""));
            Assert.That(messages[1].Content, Does.Contain("\"language\":\"German\""));
            Assert.That(messages[1].Content, Does.Contain("\"rank\":2"));
            Assert.That(messages[1].Content, Does.Contain("\"courseCode\":\"de\""));
            Assert.That(messages[^2], Is.EqualTo(new AssistantChatMessage("assistant", "Let me look.")));
            Assert.That(messages[^1], Is.EqualTo(new AssistantChatMessage("user", "Which language am I ranked highest in?")));
        });
    }
}

public sealed class AssistantRequestValidatorTests
{
    [Test]
    public void Validate_NullRequest_ReturnsError()
    {
        var result = new AssistantRequestValidator().Validate(null);

        Assert.That(result.Request, Is.Null);
        Assert.That(result.Errors, Contains.Key("request"));
    }

    [Test]
    public void Validate_EmptyMessage_ReturnsError()
    {
        var result = new AssistantRequestValidator().Validate(new AssistantMessageRequest(
            " ",
            [],
            new AssistantRouteContext("leaderboard-analytics-home")));

        Assert.That(result.Request, Is.Null);
        Assert.That(result.Errors, Contains.Key("message"));
    }

    [Test]
    public void Validate_UnsupportedRoute_ReturnsError()
    {
        var result = new AssistantRequestValidator().Validate(new AssistantMessageRequest(
            "Hi",
            [],
            new AssistantRouteContext("other-route")));

        Assert.That(result.Request, Is.Null);
        Assert.That(result.Errors, Contains.Key("context.routeName"));
    }

    [Test]
    public void Validate_ValidRequest_NormalisesRoleAndReturnsRequest()
    {
        var result = new AssistantRequestValidator().Validate(new AssistantMessageRequest(
            "  Hi Garry ",
            [new AssistantHistoryMessage("USER", "Hello")],
            new AssistantRouteContext("Leaderboard-Analytics-Home")));

        Assert.That(result.Request, Is.Not.Null);
        Assert.That(result.Request!.Message, Is.EqualTo("Hi Garry"));
        Assert.That(result.Request.History[0].Role, Is.EqualTo("user"));
        Assert.That(result.Request.Context.RouteName, Is.EqualTo("leaderboard-analytics-home"));
    }
}
