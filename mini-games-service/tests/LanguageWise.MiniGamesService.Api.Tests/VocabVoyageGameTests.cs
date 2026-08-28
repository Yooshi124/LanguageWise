using LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class VocabVoyageGameTests
{
    [Test]
    public void VocabularySelector_PrefersFiveLetterWordsFromCourseContent()
    {
        var context = new LearningContext(
            null,
            1,
            1,
            "Course",
            "Lesson",
            "The lesson introduces apple and train.",
            []);

        Assert.That(VocabularySelector.GetCandidates(context), Is.EqualTo(new[] { "APPLE", "TRAIN" }));
    }

    [Test]
    public void VocabularySelector_UsesFallbackWhenCourseHasNoFiveLetterWords()
    {
        var context = new LearningContext(
            null,
            1,
            1,
            "Course",
            "Lesson",
            "No tiny odd.",
            []);

        Assert.That(VocabularySelector.GetCandidates(context), Does.Contain("ABOUT"));
    }

    [Test]
    public void FakeLearningContext_ProvidesTenVocabVoyageAnswers()
    {
        var candidates = VocabularySelector.GetCandidates(new FakeLearningContextProvider().GetContext());

        Assert.That(candidates, Has.Length.EqualTo(10));
        Assert.That(candidates, Does.Contain("VOCAB"));
        Assert.That(candidates, Does.Contain("RIGHT"));
    }

    [Test]
    public void SubmitGuess_ReturnsGreenForAnExactMatch()
    {
        var game = new VocabVoyageGame("English", ["VOCAB"]);

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
        var game = new VocabVoyageGame("English", ["VOCAB"]);

        var result = game.SubmitGuess("vexxx");

        Assert.That(result.Colours, Is.EqualTo(new[] { 'G', 'R', 'R', 'R', 'R' }));
    }

    [Test]
    public void SubmitGuess_RejectsGuessesThatAreNotFiveLetters()
    {
        var game = new VocabVoyageGame("English", ["VOCAB"]);

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("four"));
    }

    [Test]
    public void SubmitGuess_CompletesAfterSixIncorrectAttempts()
    {
        var game = new VocabVoyageGame("English", ["VOCAB"]);

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
        var game = new VocabVoyageGame("English", ["LEVEL"]);

        var result = game.SubmitGuess("EERIE");

        Assert.That(result.Colours, Is.EqualTo(new[] { 'O', 'G', 'R', 'R', 'R' }));
    }

    [Test]
    public void SubmitGuess_RejectsNonLetterCharacters()
    {
        var game = new VocabVoyageGame("English", ["VOCAB"]);

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("voc4b"));
    }
}
