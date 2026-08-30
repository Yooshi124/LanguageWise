using LanguageWise.MiniGamesService.Api.Feature.WordSearch;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class WordSearchGameTests
{
    [Test]
    public void NewGameUsesAFullTallBoardWithContiguousThemedPaths()
    {
        var game = new WordSearchGame("English");
        var state = game.GetState();
        var coveredCells = state.CoveragePaths.SelectMany(path => path).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(state.Board, Has.Count.EqualTo(state.Rows * state.Columns));
            Assert.That(state.Rows, Is.EqualTo(8));
            Assert.That(state.Columns, Is.EqualTo(6));
            Assert.That(game.GetWordChain(), Has.Length.GreaterThanOrEqualTo(9));
            Assert.That(game.GetWordChain().Min(word => word.Length), Is.GreaterThanOrEqualTo(3));
            Assert.That(coveredCells, Has.Count.EqualTo(state.Board.Count));
            Assert.That(state.WordPaths[state.FeaturedWord].Select(index => index / state.Columns).Distinct().ToArray(), Has.Length.EqualTo(state.Rows));
            Assert.That(state.WordPaths.Values.SelectMany(path => path), Is.Unique);
            Assert.That(state.ThemeHint, Is.Not.Empty);
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
    public void SubmitWordAcceptsWordsOfDifferentLengthsAndTracksScore()
    {
        var game = new WordSearchGame("English");
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
        var game = new WordSearchGame("English");

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
        var game = new WordSearchGame("English");

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
        var game = new WordSearchGame("English");

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
        var game = new WordSearchGame("English");
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