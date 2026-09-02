using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class DiscussionRulesTests
{
    private static readonly IReadOnlyCollection<Forum> Forums =
    [
        new Forum(1, null, "global", "Global"),
        new Forum(2, 11, "spanish", "Spanish"),
        new Forum(3, 12, "italian", "Italian")
    ];

    [Test]
    public void ValidateCreatePost_WithEveryFieldSupplied_ReportsNoErrors()
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest("Title", "Content", "spanish"),
            Forums);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidateCreatePost_WithoutAForum_ReportsAnError()
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest("Title", "Content", null),
            Forums);

        Assert.That(errors, Does.ContainKey("forumCode"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ValidateCreatePost_WithABlankTitle_ReportsAnError(string title)
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest(title, "Content", "global"),
            Forums);

        Assert.That(errors, Does.ContainKey("title"));
    }

    [Test]
    public void ValidateCreatePost_WithoutABody_ReportsAnError()
    {
        var errors = DiscussionRules.ValidateCreatePost(null, Forums);

        Assert.That(errors, Does.ContainKey("body"));
    }

    [Test]
    public void ValidatePatchPost_WithASingleField_ReportsNoErrors()
    {
        var errors = DiscussionRules.ValidatePatchPost(
            new PatchPostRequest(null, "Only the content changes", null),
            Forums);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidatePatchPost_WithNothingToChange_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest(null, null, null), Forums);

        Assert.That(errors, Does.ContainKey("body"));
    }

    [Test]
    public void ValidatePatchPost_WithABlankSuppliedField_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest("  ", null, null), Forums);

        Assert.That(errors, Does.ContainKey("title"));
    }

    [Test]
    public void ValidatePatchComment_WithNoContent_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchComment(new PatchCommentRequest(null));

        Assert.That(errors, Does.ContainKey("body"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(DiscussionRules.MaxLimit + 1)]
    public void ValidatePaging_WithALimitOutsideTheAllowedRange_ReportsAnError(int limit)
    {
        var errors = DiscussionRules.ValidatePaging(limit, 0);

        Assert.That(errors, Does.ContainKey("limit"));
    }

    [Test]
    public void ValidatePaging_WithANegativeOffset_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePaging(DiscussionRules.DefaultLimit, -1);

        Assert.That(errors, Does.ContainKey("offset"));
    }

    [Test]
    public void ValidatePaging_WithTheDefaults_ReportsNoErrors()
    {
        var errors = DiscussionRules.ValidatePaging(DiscussionRules.DefaultLimit, 0);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void MergePost_KeepsTheFieldsThePatchLeavesOut()
    {
        var current = CurrentPost();

        var (title, content, forumCode) = DiscussionRules.MergePost(
            current,
            new PatchPostRequest(null, "Fresh content", null));

        Assert.Multiple(() =>
        {
            Assert.That(title, Is.EqualTo("Original title"));
            Assert.That(content, Is.EqualTo("Fresh content"));
            Assert.That(forumCode, Is.EqualTo("italian"));
        });
    }

    [Test]
    public void MergePost_TrimsTheFieldsThePatchSupplies()
    {
        var (title, _, _) = DiscussionRules.MergePost(
            CurrentPost(),
            new PatchPostRequest("  Padded title  ", null, null));

        Assert.That(title, Is.EqualTo("Padded title"));
    }

    [Test]
    public void GetUserId_ReadsTheSubjectClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, "42")
        ]));

        Assert.That(DiscussionRules.GetUserId(principal), Is.EqualTo(42));
    }

    [Test]
    public void GetUserId_WithoutASubjectClaim_ReturnsNull()
    {
        Assert.That(DiscussionRules.GetUserId(new ClaimsPrincipal(new ClaimsIdentity())), Is.Null);
    }

    [Test]
    public void ValidateCreatePost_WithAForumThatDoesNotExist_ReportsAnError()
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest("Title", "Content", "klingon"),
            Forums);

        Assert.That(errors, Does.ContainKey("forumCode"));
    }

    [Test]
    public void ValidateCreatePost_MatchesAForumRegardlessOfCasing()
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest("Title", "Content", "Spanish"),
            Forums);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidatePatchPost_WithAForumThatDoesNotExist_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest(null, null, "klingon"), Forums);

        Assert.That(errors, Does.ContainKey("forumCode"));
    }

    [Test]
    public void ValidateCreatePost_WithAForumThatDoesNotExist_NamesTheForumsThatDo()
    {
        var errors = DiscussionRules.ValidateCreatePost(
            new CreatePostRequest("Title", "Content", "klingon"),
            Forums);

        Assert.That(errors["forumCode"].Single(), Does.Contain("global, spanish, italian"));
    }

    [TestCase("spanish")]
    [TestCase("SPANISH")]
    [TestCase("  spanish  ")]
    public void IsKnownForum_MatchesARowRegardlessOfCasingOrPadding(string code)
    {
        Assert.That(DiscussionRules.IsKnownForum(code, Forums), Is.True);
    }

    [TestCase("klingon")]
    [TestCase(null)]
    public void IsKnownForum_WithoutAMatchingRow_IsFalse(string? code)
    {
        Assert.That(DiscussionRules.IsKnownForum(code, Forums), Is.False);
    }

    [Test]
    public void GetUserName_ReadsTheNameClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Name, "lachlan")
        ]));

        Assert.That(DiscussionRules.GetUserName(principal), Is.EqualTo("lachlan"));
    }

    [Test]
    public void GetUserName_WithoutANameClaim_ReturnsAnEmptyString()
    {
        Assert.That(DiscussionRules.GetUserName(new ClaimsPrincipal(new ClaimsIdentity())), Is.Empty);
    }

    private static PostSummary CurrentPost() => new(
        1,
        1,
        "lachlan",
        "Original title",
        "Original content",
        "italian",
        "Italian",
        DateTime.UtcNow,
        DateTime.UtcNow,
        0,
        0,
        false,
        null);
}
