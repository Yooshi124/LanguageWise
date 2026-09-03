using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class NotificationRulesTests
{
    [Test]
    public void GetUserId_ReturnsNumericSubject()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, "42")
        ]));

        Assert.That(NotificationRules.GetUserId(user), Is.EqualTo(42));
    }

    [TestCase(null)]
    [TestCase("not-a-number")]
    public void GetUserId_WhenSubjectIsInvalid_ReturnsNull(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtRegisteredClaimNames.Sub, subject) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.That(NotificationRules.GetUserId(user), Is.Null);
    }

    [TestCase("learner@example.com", true)]
    [TestCase("not-an-email", false)]
    public void IsValidEmail_ReturnsExpectedResult(string email, bool expected)
    {
        Assert.That(NotificationRules.IsValidEmail(email), Is.EqualTo(expected));
    }

    [TestCase("community-contribution")]
    [TestCase("post-engagement")]
    [TestCase("lesson-completion")]
    [TestCase("course-completion")]
    [TestCase("quiz-result")]
    [TestCase("minigame-win")]
    [TestCase("login-streak")]
    public void ValidateEvent_WithSupportedTrigger_ReturnsNoErrors(string trigger)
    {
        var request = ValidEvent() with { Trigger = trigger };

        Assert.That(NotificationRules.ValidateEvent(request), Is.Empty);
    }

    [Test]
    public void ValidateEvent_WithInvalidFields_ReturnsEveryRelevantError()
    {
        var request = new EventRequest("unknown", "", 0, "");

        var errors = NotificationRules.ValidateEvent(request);

        Assert.That(errors.Keys, Is.EquivalentTo(new[]
        {
            "trigger",
            "subject",
            "recipientUserId",
            "recipientName"
        }));
    }

    [Test]
    public void CalculateProgress_CapsProgressAndMarksNewAttainment()
    {
        var update = NotificationRules.CalculateProgress(4, 5);

        Assert.Multiple(() =>
        {
            Assert.That(update.Progress, Is.EqualTo(5));
            Assert.That(update.NewlyAttained, Is.True);
        });
    }

    [Test]
    public void CalculateProgress_WhenAlreadyAttained_DoesNotAttainAgain()
    {
        var update = NotificationRules.CalculateProgress(5, 5);

        Assert.Multiple(() =>
        {
            Assert.That(update.Progress, Is.EqualTo(5));
            Assert.That(update.NewlyAttained, Is.False);
        });
    }

    [Test]
    public void CalculateProgress_WithMaximumValue_DoesNotOverflow()
    {
        var update = NotificationRules.CalculateProgress(int.MaxValue, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(update.Progress, Is.EqualTo(int.MaxValue));
            Assert.That(update.NewlyAttained, Is.False);
        });
    }

    [Test]
    public void CalculateProgress_WithGreaterAbsoluteValue_UsesValue()
    {
        var update = NotificationRules.CalculateProgress(2, 7, 5);

        Assert.Multiple(() =>
        {
            Assert.That(update.Progress, Is.EqualTo(5));
            Assert.That(update.NewlyAttained, Is.False);
        });
    }

    [Test]
    public void CalculateProgress_WithLowerAbsoluteValue_PreservesProgress()
    {
        var update = NotificationRules.CalculateProgress(5, 7, 0);

        Assert.That(update.Progress, Is.EqualTo(5));
    }

    [Test]
    public void CalculateProgress_ForUnboundedAchievement_TracksHighestValue()
    {
        var update = NotificationRules.CalculateProgress(7, -1, 12);

        Assert.Multiple(() =>
        {
            Assert.That(update.Progress, Is.EqualTo(12));
            Assert.That(update.NewlyAttained, Is.False);
        });
    }

    [Test]
    public void ShouldNotify_WhenMasterPreferenceIsDisabled_ReturnsFalse()
    {
        var preferences = Preferences(notifyAll: false, notifyCourseCompletion: true);

        Assert.That(NotificationRules.ShouldNotify(preferences, "course-completion", true), Is.False);
    }

    [TestCase("community-contribution")]
    [TestCase("post-engagement")]
    [TestCase("lesson-completion")]
    [TestCase("course-completion")]
    [TestCase("quiz-result")]
    [TestCase("minigame-win")]
    [TestCase("login-streak")]
    public void ShouldNotify_WhenMatchingTriggerPreferenceIsEnabled_ReturnsTrue(string trigger)
    {
        var preferences = Preferences(
            notifyCommunityContribution: trigger == "community-contribution",
            notifyPostEngagement: trigger == "post-engagement",
            notifyLessonCompletion: trigger == "lesson-completion",
            notifyCourseCompletion: trigger == "course-completion",
            notifyQuizResult: trigger == "quiz-result",
            notifyMinigameWin: trigger == "minigame-win",
            notifyLoginStreak: trigger == "login-streak");

        Assert.That(NotificationRules.ShouldNotify(preferences, trigger, false), Is.True);
    }

    [TestCase("community-contribution")]
    [TestCase("post-engagement")]
    [TestCase("lesson-completion")]
    [TestCase("course-completion")]
    [TestCase("quiz-result")]
    [TestCase("minigame-win")]
    [TestCase("login-streak")]
    public void ShouldNotify_WhenOnlyOtherTriggerPreferencesAreEnabled_ReturnsFalse(string trigger)
    {
        var preferences = Preferences(
            notifyCommunityContribution: trigger != "community-contribution",
            notifyPostEngagement: trigger != "post-engagement",
            notifyLessonCompletion: trigger != "lesson-completion",
            notifyCourseCompletion: trigger != "course-completion",
            notifyQuizResult: trigger != "quiz-result",
            notifyMinigameWin: trigger != "minigame-win",
            notifyLoginStreak: trigger != "login-streak");

        Assert.That(NotificationRules.ShouldNotify(preferences, trigger, false), Is.False);
    }

    [Test]
    public void ShouldNotify_WhenAchievementIsNewlyAttained_ReturnsTrue()
    {
        var preferences = Preferences(notifyAchievements: true);

        Assert.That(NotificationRules.ShouldNotify(preferences, "minigame-win", true), Is.True);
    }

    private static EventRequest ValidEvent() =>
        new("lesson-completion", "Completed Introduction to Spanish", 1, "Amber");

    private static UserPreferences Preferences(
        bool notifyAll = true,
        bool notifyCommunityContribution = false,
        bool notifyPostEngagement = false,
        bool notifyLessonCompletion = false,
        bool notifyCourseCompletion = false,
        bool notifyQuizResult = false,
        bool notifyMinigameWin = false,
        bool notifyLoginStreak = false,
        bool notifyAchievements = false) =>
        new(
            1,
            "learner@example.com",
            notifyAll,
            notifyCommunityContribution,
            notifyPostEngagement,
            notifyLessonCompletion,
            notifyCourseCompletion,
            notifyQuizResult,
            notifyMinigameWin,
            notifyLoginStreak,
            notifyAchievements);
}