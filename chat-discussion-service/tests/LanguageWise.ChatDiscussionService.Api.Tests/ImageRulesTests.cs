namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class ImageRulesTests
{
    [Test]
    public void ValidateUpload_WithoutAFile_AsksForOne()
    {
        var errors = ImageRules.ValidateUpload(null, 0, existingCount: 0);

        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "file" }));
    }

    [Test]
    public void ValidateUpload_WithAFileOverTheLimit_ReportsTheSize()
    {
        var errors = ImageRules.ValidateUpload("image/png", ImageRules.MaxBytes + 1, existingCount: 0);

        Assert.That(errors.ContainsKey("file"), Is.True);
    }

    [Test]
    public void ValidateUpload_AtExactlyTheLimit_IsAccepted()
    {
        var errors = ImageRules.ValidateUpload("image/png", ImageRules.MaxBytes, existingCount: 0);

        Assert.That(errors, Is.Empty);
    }

    [TestCase("image/svg+xml")]
    [TestCase("text/html")]
    [TestCase("application/octet-stream")]
    public void ValidateUpload_WithAnUnsupportedType_ReportsTheType(string contentType)
    {
        var errors = ImageRules.ValidateUpload(contentType, 1024, existingCount: 0);

        Assert.That(errors.ContainsKey("contentType"), Is.True);
    }

    [Test]
    public void ValidateUpload_WhenThePostIsAlreadyFull_ReportsTheCount()
    {
        var errors = ImageRules.ValidateUpload("image/png", 1024, ImageRules.MaxPerPost);

        Assert.That(errors.ContainsKey("images"), Is.True);
    }

    [Test]
    public void ValidateUpload_WithOneSlotLeft_IsAccepted()
    {
        var errors = ImageRules.ValidateUpload("image/png", 1024, ImageRules.MaxPerPost - 1);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void IsAllowedContentType_IgnoresParametersAndCasing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ImageRules.IsAllowedContentType("IMAGE/PNG"), Is.True);
            Assert.That(ImageRules.IsAllowedContentType("image/jpeg; charset=binary"), Is.True);
            Assert.That(ImageRules.IsAllowedContentType(null), Is.False);
        });
    }

    [TestCase("holiday.png", "holiday.png")]
    [TestCase("  spaced.png  ", "spaced.png")]
    [TestCase("C:\\Users\\amber\\holiday.png", "holiday.png")]
    [TestCase("../../etc/passwd", "passwd")]
    [TestCase("", "image")]
    [TestCase(null, "image")]
    public void SafeFileName_KeepsOnlyTheLeafName(string? supplied, string expected)
    {
        Assert.That(ImageRules.SafeFileName(supplied), Is.EqualTo(expected));
    }

    [Test]
    public void SafeFileName_TruncatesAVeryLongName()
    {
        var name = ImageRules.SafeFileName(new string('a', ImageRules.MaxFileNameLength + 50));

        Assert.That(name, Has.Length.EqualTo(ImageRules.MaxFileNameLength));
    }

    [Test]
    public void MatchesContentType_AcceptsEachSupportedFormat()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ImageRules.MatchesContentType("image/png", ImageBytes.Png()), Is.True);
            Assert.That(ImageRules.MatchesContentType("image/gif", ImageBytes.Gif()), Is.True);
            Assert.That(ImageRules.MatchesContentType("image/webp", ImageBytes.Webp()), Is.True);
        });
    }

    [Test]
    public void MatchesContentType_RejectsBytesThatAreNotTheDeclaredFormat()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ImageRules.MatchesContentType("image/png", ImageBytes.Gif()), Is.False);
            Assert.That(ImageRules.MatchesContentType("image/png", ImageBytes.NotAnImage()), Is.False);
            Assert.That(ImageRules.MatchesContentType("image/png", []), Is.False);
        });
    }
}
