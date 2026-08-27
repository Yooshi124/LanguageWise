using LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class VocabVoyageGameTests
{
    [Test]
    public void SubmitGuess_ReturnsGreenForAnExactMatch()
    {
        var game = new VocabVoyageGame("English");

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
        var game = new VocabVoyageGame("English");

        var result = game.SubmitGuess("vexxx");

        Assert.That(result.Colours, Is.EqualTo(new[] { 'G', 'R', 'R', 'R', 'R' }));
    }

    [Test]
    public void SubmitGuess_RejectsGuessesThatAreNotFiveLetters()
    {
        var game = new VocabVoyageGame("English");

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("four"));
    }

    [Test]
    public void SubmitGuess_CompletesAfterSixIncorrectAttempts()
    {
        var game = new VocabVoyageGame("English");

        for (var attempt = 0; attempt < 6; attempt++)
        {
            game.SubmitGuess("xxxxx");
        }

        Assert.Multiple(() =>
        {
            Assert.That(game.GetState().Attempts, Is.EqualTo(6));
            Assert.That(game.GetState().IsComplete, Is.True);
            Assert.That(game.GetState().IsWon, Is.False);
        });
    }
}
