using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Services;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class AssistantPromptBuilderTests
{
    [Test]
    public void BuildMessages_IncludesTheCompleteProfileAndConversation()
    {
        var profile = new ProfileResponse(
            "amber",
            new ProfilePreferences(
                "amber@example.com",
                true,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false),
            [new AchievementProgress(3, "Next Step", "/achievement.svg", 2, 5)],
            [new ProfileNotification(
                12,
                "post-engagement",
                DateTimeOffset.Parse("2026-09-05T08:00:00Z"),
                "Someone liked your post",
                "Your post received a like.")]);
        var request = new ValidatedAssistantRequest(
            "Why did I not get an email?",
            [new AssistantHistoryMessage("assistant", "Let me check.")],
            new AssistantRouteContext("quests-achievements-home"));

        var messages = new AssistantPromptBuilder().BuildMessages(request, profile);

        Assert.Multiple(() =>
        {
            Assert.That(messages[0].Content, Does.Contain("achievements and notifications assistant"));
            Assert.That(messages[1].Content, Does.Contain("\"email\":\"amber@example.com\""));
            Assert.That(messages[1].Content, Does.Contain("\"name\":\"Next Step\""));
            Assert.That(messages[1].Content, Does.Contain("\"trigger\":\"post-engagement\""));
            Assert.That(messages[^2], Is.EqualTo(new AssistantChatMessage("assistant", "Let me check.")));
            Assert.That(messages[^1], Is.EqualTo(new AssistantChatMessage("user", "Why did I not get an email?")));
        });
    }
}