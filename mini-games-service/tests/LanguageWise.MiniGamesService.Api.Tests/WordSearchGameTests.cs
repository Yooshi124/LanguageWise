using LanguageWise.MiniGamesService.Api.Feature.WordSearch;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class WordSearchGameTests
{
    private const string ThemeHint = "Words from your lessons: Greetings · Numbers";

    private static readonly string[] TestVocabulary =
    [
        "ASTRONAUT", "ECLIPSE", "GALAXY", "ROCKET", "COMET", "MARS", "MOON", "RING", "SUN"
    ];

    [Test]
    public void NewGameUsesAFullTallBoardWithContiguousThemedPaths()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);
        var state = game.GetState();
        var coveredCells = state.CoveragePaths.SelectMany(path => path).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(state.Board, Has.Count.EqualTo(state.Rows * state.Columns));
            Assert.That(state.Rows, Is.EqualTo(8));
            Assert.That(state.Columns, Is.EqualTo(6));
            Assert.That(game.GetWordChain(), Has.Length.GreaterThanOrEqualTo(4));
            Assert.That(game.GetWordChain().Min(word => word.Length), Is.GreaterThanOrEqualTo(3));
            Assert.That(coveredCells, Has.Count.EqualTo(state.WordPaths.Values.Sum(path => path.Count)));
            Assert.That(state.WordPaths, Contains.Key(state.FeaturedWord));
            Assert.That(state.WordPaths.Values.SelectMany(path => path), Is.Unique);
            Assert.That(state.ThemeHint, Is.EqualTo(ThemeHint));
            Assert.That(state.Board, Has.All.Not.Null.And.Not.Empty);
        });

        foreach (var path in state.WordPaths.Values)
        {
            for (var index = 1; index < path.Count; index++)
            {
                var previousRow = path[index - 1] / state.Columns;
                var previousColumn = path[index - 1] % state.Columns;
                var row = path[index] / state.Columns;
                var column = path[index] % state.Columns;
                Assert.That(Math.Abs(row - previousRow), Is.LessThanOrEqualTo(1));
                Assert.That(Math.Abs(column - previousColumn), Is.LessThanOrEqualTo(1));
                Assert.That(row != previousRow || column != previousColumn, Is.True);
            }
        }

        foreach (var word in state.WordPaths.Keys)
        {
            Assert.That(state.WordPaths[word].Select(index => state.Board[index]).ToArray(), Is.EqualTo(word.Select(character => character.ToString()).ToArray()));
        }
    }

    [Test]
    public void ConstructorFiltersUnplayableWords()
    {
        var game = new WordSearchGame("English", ThemeHint, ["AB", "HAS SPACE", "TOO", "VALID", "ALSOVALID"]);

        Assert.Multiple(() =>
        {
            Assert.That(game.GetWordChain(), Does.Not.Contain("AB"));
            Assert.That(game.GetWordChain(), Does.Not.Contain("HAS SPACE"));
            Assert.That(game.GetWordChain(), Does.Contain("VALID"));
        });
    }

    [Test]
    public void ConstructorRejectsEmptyVocabulary()
    {
        Assert.Throws<ArgumentException>(() => new WordSearchGame("English", ThemeHint, []));
        Assert.Throws<ArgumentException>(() => new WordSearchGame("English", ThemeHint, ["AB", "HAS SPACE"]));
    }

    [Test]
    public void SubmitWordAcceptsWordsOfDifferentLengthsAndTracksScore()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);
        var chain = game.GetWordChain();
        var shortWord = chain.OrderBy(word => word.Length).First();
        var longWord = chain.OrderByDescending(word => word.Length).First();

        var shortResult = game.SubmitWord(shortWord);
        var longResult = game.SubmitWord(longWord);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.IsValid, Is.True);
            Assert.That(longResult.IsValid, Is.True);
            Assert.That(game.GetState().Words, Is.EqualTo(new[] { shortWord, longWord }));
            Assert.That(game.GetState().Score, Is.EqualTo(shortWord.Length + longWord.Length));
        });
    }

    [Test]
    public void InvalidWordDoesNotChangeProgress()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);

        var result = game.SubmitWord("language");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(game.GetState().Words, Is.Empty);
            Assert.That(game.GetState().Score, Is.Zero);
        });
    }

    [Test]
    public void FindingEveryWordCompletesTheGame()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);

        foreach (var word in game.GetWordChain())
        {
            game.SubmitWord(word);
        }

        Assert.Multiple(() =>
        {
            Assert.That(game.GetState().Words, Has.Count.EqualTo(game.GetState().TotalWords));
            Assert.That(game.GetState().IsComplete, Is.True);
        });
    }

    [Test]
    public void PlayerCanUseThreeHintsAndTheFourthIsRejected()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);

        game.UseHint();
        game.UseHint();
        var thirdHint = game.UseHint();

        Assert.Multiple(() =>
        {
            Assert.That(thirdHint.State.HintsUsed, Is.EqualTo(3));
            Assert.That(thirdHint.Path, Is.Not.Empty);
            Assert.That(game.GetState().HintWord, Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() => game.UseHint());
        });
    }

    [Test]
    public void GiveUpRevealsOnlyWordsThatWereNotFound()
    {
        var game = new WordSearchGame("English", ThemeHint, TestVocabulary);
        var firstWord = game.GetWordChain().First();

        game.SubmitWord(firstWord);
        var state = game.GiveUp();

        Assert.Multiple(() =>
        {
            Assert.That(state.IsGivenUp, Is.True);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.Words, Does.Contain(firstWord));
            Assert.That(state.RevealedWords, Does.Not.Contain(firstWord));
            Assert.That(state.RevealedWords.Count, Is.EqualTo(state.TotalWords - 1));
        });
    }
}
