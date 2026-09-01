using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class GuessTheWordGameTests
{
    [Test]
    public void Constructor_RejectsCandidateListsWithoutAFiveLetterWord()
    {
        Assert.Throws<ArgumentException>(() => new GuessTheWordGame("English", ["CAT", "DOG"]));
        Assert.Throws<ArgumentException>(() => new GuessTheWordGame("English", []));
    }

    [Test]
    public void Constructor_AcceptsWordsWithNonAsciiLetters()
    {
        var game = new GuessTheWordGame("German", ["KÖNIG"]);

        Assert.That(game.GetState().IsComplete, Is.False);
    }

    [Test]
    public void SubmitGuess_MatchesAccentedLettersWithTheirPlainCounterpart()
    {
        var game = new GuessTheWordGame("German", ["KÖNIG"]);

        var result = game.SubmitGuess("konig");

        Assert.Multiple(() =>
        {
            Assert.That(result.Colours, Is.EqualTo(new[] { 'G', 'G', 'G', 'G', 'G' }));
            Assert.That(result.IsCorrect, Is.True);
        });
    }

    [Test]
    public void SubmitGuess_PreservesEszettAsASingleLetter()
    {
        // Single-candidate list keeps the answer deterministic. If ß were
        // expanded to SS this word would be six letters and the game would
        // reject it as a candidate, so constructing the game itself proves the
        // letter was preserved.
        var game = new GuessTheWordGame("German", ["GRÜßE"]);

        // Typing the word with Ü folded to U (and ß kept as itself) must win,
        // and the revealed answer must still contain ß as one letter.
        var result = game.SubmitGuess("GRUßE");

        Assert.Multiple(() =>
        {
            Assert.That(result.Colours.All(colour => colour == 'G'), Is.True);
            Assert.That(result.IsCorrect, Is.True);
            Assert.That(game.GetState().CorrectAnswer, Is.EqualTo("GRÜßE"));
        });
    }

    [Test]
    public void SubmitGuess_ReturnsGreenForAnExactMatch()
    {
        var game = new GuessTheWordGame("English", ["VOCAB"]);

        var result = game.SubmitGuess("vocab");

        Assert.Multiple(() =>
        {
            Assert.That(result.Colours, Is.EqualTo(new[] { 'G', 'G', 'G', 'G', 'G' }));
            Assert.That(result.IsCorrect, Is.True);
            Assert.That(game.GetState().IsWon, Is.True);
            Assert.That(game.GetState().IsComplete, Is.True);
        });
    }

    [Test]
    public void SubmitGuess_ReturnsRedForLettersNotInTheAnswer()
    {
        var game = new GuessTheWordGame("English", ["VOCAB"]);

        var result = game.SubmitGuess("vexxx");

        Assert.That(result.Colours, Is.EqualTo(new[] { 'G', 'R', 'R', 'R', 'R' }));
    }

    [Test]
    public void SubmitGuess_RejectsGuessesThatAreNotFiveLetters()
    {
        var game = new GuessTheWordGame("English", ["VOCAB"]);

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("four"));
    }

    [Test]
    public void SubmitGuess_CompletesAfterSixIncorrectAttempts()
    {
        var game = new GuessTheWordGame("English", ["VOCAB"]);

        for (var attempt = 0; attempt < 6; attempt++)
        {
            game.SubmitGuess("xxxxx");
        }

        Assert.Multiple(() =>
        {
            Assert.That(game.GetState().Attempts, Is.EqualTo(6));
            Assert.That(game.GetState().IsComplete, Is.True);
            Assert.That(game.GetState().IsWon, Is.False);
            Assert.That(game.GetState().CorrectAnswer, Is.EqualTo("VOCAB"));
        });
    }

    [Test]
    public void SubmitGuess_DoesNotReuseMatchedLettersForOrangeResults()
    {
        var game = new GuessTheWordGame("English", ["LEVEL"]);

        var result = game.SubmitGuess("EERIE");

        Assert.That(result.Colours, Is.EqualTo(new[] { 'O', 'G', 'R', 'R', 'R' }));
    }

    [Test]
    public void SubmitGuess_RejectsNonLetterCharacters()
    {
        var game = new GuessTheWordGame("English", ["VOCAB"]);

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("voc4b"));
    }
}
