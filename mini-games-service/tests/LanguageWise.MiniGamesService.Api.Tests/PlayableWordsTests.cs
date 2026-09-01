using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class PlayableWordsTests
{
    [Test]
    public void ExtractSplitsPhrasesIntoWords()
    {
        var words = PlayableWords.Extract(["Guten Tag", "Willkommen"]);

        Assert.That(words, Is.EquivalentTo(new[] { "GUTEN", "TAG", "WILLKOMMEN" }));
    }

    [Test]
    public void ExtractKeepsNonAsciiLetters()
    {
        var words = PlayableWords.Extract(["der Käse", "Frühstück"]);

        Assert.That(words, Does.Contain("KÄSE"));
        Assert.That(words, Does.Contain("FRÜHSTÜCK"));
    }

    [Test]
    public void ExtractDropsShortAndOverlongTokens()
    {
        var words = PlayableWords.Extract(["Ja", "Nein", "Supercalifragilistic"]);

        Assert.That(words, Is.EqualTo(new[] { "NEIN" }));
    }

    [Test]
    public void ExtractDropsTokensSharedAcrossEntries()
    {
        // Articles like "die" appear in several entries and are connectors, not vocabulary.
        var words = PlayableWords.Extract(["die Familie", "die Mutter", "der Vater", "der Bruder"]);

        Assert.Multiple(() =>
        {
            Assert.That(words, Does.Not.Contain("DIE"));
            Assert.That(words, Does.Not.Contain("DER"));
            Assert.That(words, Is.EquivalentTo(new[] { "FAMILIE", "MUTTER", "VATER", "BRUDER" }));
        });
    }

    [Test]
    public void ExtractDeduplicatesWithinAnEntry()
    {
        var words = PlayableWords.Extract(["very very good"]);

        Assert.That(words, Is.EqualTo(new[] { "VERY", "GOOD" }));
    }

    [Test]
    public void ExtractReturnsEmptyForEmptyInput()
    {
        Assert.That(PlayableWords.Extract([]), Is.Empty);
    }
}
