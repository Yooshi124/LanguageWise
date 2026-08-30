using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class DiscussionRulesTests
{
    [Test]
    public void ValidateCreatePost_WithEveryFieldSupplied_ReportsNoErrors()
    {
        var errors = DiscussionRules.ValidateCreatePost(new CreatePostRequest("Title", "Content", "spanish"));

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidateCreatePost_WithoutACategory_ReportsAnError()
    {
        var errors = DiscussionRules.ValidateCreatePost(new CreatePostRequest("Title", "Content", null));

        Assert.That(errors, Does.ContainKey("category"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ValidateCreatePost_WithABlankTitle_ReportsAnError(string title)
    {
        var errors = DiscussionRules.ValidateCreatePost(new CreatePostRequest(title, "Content", "global"));

        Assert.That(errors, Does.ContainKey("title"));
    }

    [Test]
    public void ValidateCreatePost_WithoutABody_ReportsAnError()
    {
        var errors = DiscussionRules.ValidateCreatePost(null);

        Assert.That(errors, Does.ContainKey("body"));
    }

    [Test]
    public void ValidatePatchPost_WithASingleField_ReportsNoErrors()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest(null, "Only the content changes", null));

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidatePatchPost_WithNothingToChange_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest(null, null, null));

        Assert.That(errors, Does.ContainKey("body"));
    }

    [Test]
    public void ValidatePatchPost_WithABlankSuppliedField_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest("  ", null, null));

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

        var (title, content, category) = DiscussionRules.MergePost(
            current,
            new PatchPostRequest(null, "Fresh content", null));

        Assert.Multiple(() =>
        {
            Assert.That(title, Is.EqualTo("Original title"));
            Assert.That(content, Is.EqualTo("Fresh content"));
            Assert.That(category, Is.EqualTo("italian"));
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
        var errors = DiscussionRules.ValidateCreatePost(new CreatePostRequest("Title", "Content", "klingon"));

        Assert.That(errors, Does.ContainKey("category"));
    }

    [Test]
    public void ValidateCreatePost_MatchesAForumRegardlessOfCasing()
    {
        var errors = DiscussionRules.ValidateCreatePost(new CreatePostRequest("Title", "Content", "Spanish"));

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidatePatchPost_WithAForumThatDoesNotExist_ReportsAnError()
    {
        var errors = DiscussionRules.ValidatePatchPost(new PatchPostRequest(null, null, "klingon"));

        Assert.That(errors, Does.ContainKey("category"));
    }

    [Test]
    public void ValidateCategoryFilter_WithAForumThatDoesNotExist_ReportsAnError()
    {
        Assert.That(DiscussionRules.ValidateCategoryFilter("klingon"), Does.ContainKey("category"));
    }

    [Test]
    public void ValidateCategoryFilter_WithNoFilter_ReportsNoErrors()
    {
        Assert.That(DiscussionRules.ValidateCategoryFilter(null), Is.Empty);
    }

    [Test]
    public void Forums_IncludeGlobalAndTheSeededLanguages()
    {
        Assert.That(
            DiscussionRules.Forums.Select(forum => forum.Code),
            Is.EquivalentTo(new[] { "global", "spanish", "italian", "japanese" }));
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
        DateTime.UtcNow,
        DateTime.UtcNow,
        0,
        0,
        false,
        null);
}
