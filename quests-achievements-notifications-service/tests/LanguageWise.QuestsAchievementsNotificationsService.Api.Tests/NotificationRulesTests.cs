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

    [Test]
    public void ValidateEvent_WithValidEvent_ReturnsNoErrors()
    {
        var request = ValidEvent();

        Assert.That(NotificationRules.ValidateEvent(request), Is.Empty);
    }

    [Test]
    public void ValidateEvent_WithInvalidFields_ReturnsEveryRelevantError()
    {
        var request = new EventRequest("unknown", "", 0);

        var errors = NotificationRules.ValidateEvent(request);

        Assert.That(errors.Keys, Is.EquivalentTo(new[]
        {
            "trigger",
            "subject",
            "recipientUserId"
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
    public void ShouldNotify_WhenMasterPreferenceIsDisabled_ReturnsFalse()
    {
        var preferences = Preferences(notifyAll: false, notifyCourseCompletion: true);

        Assert.That(NotificationRules.ShouldNotify(preferences, "course-completion", true), Is.False);
    }

    [Test]
    public void ShouldNotify_WhenCategoryIsEnabled_ReturnsTrue()
    {
        var preferences = Preferences(notifyCourseCompletion: true);

        Assert.That(NotificationRules.ShouldNotify(preferences, "course-completion", false), Is.True);
    }

    [Test]
    public void ShouldNotify_WhenAchievementIsNewlyAttained_ReturnsTrue()
    {
        var preferences = Preferences(notifyAchievements: true);

        Assert.That(NotificationRules.ShouldNotify(preferences, "course-completion", true), Is.True);
    }

    private static EventRequest ValidEvent() =>
        new("course-completion", "Introduction to Spanish", 1);

    private static UserPreferences Preferences(
        bool notifyAll = true,
        bool notifyCourseCompletion = false,
        bool notifyAchievements = false) =>
        new(1, "learner@example.com", notifyAll, false, notifyCourseCompletion, false, false, notifyAchievements);
}