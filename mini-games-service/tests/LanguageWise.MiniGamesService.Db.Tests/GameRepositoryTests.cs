using LanguageWise.MiniGamesService.Db.Data;
using Microsoft.Data.Sqlite;

namespace LanguageWise.MiniGamesService.Db.Tests;

[TestFixture]
public class GameRepositoryTests : DatabaseTestFixture
{
    [Test]
    public void Create_PersistsTheGameAndReadsItBack()
    {
        var created = CreateGame("word_search", "de");

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.GreaterThan(0));
            Assert.That(created.GameType, Is.EqualTo("word_search"));
            Assert.That(created.CourseCode, Is.EqualTo("de"));
            Assert.That(created.Solution, Is.EqualTo("APPLE"));
        });

        var fetched = Games.GetById(created.Id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.CourseCode, Is.EqualTo("de"));
    }

    [Test]
    public void Create_RejectsAnUnknownGameType()
    {
        Assert.Throws<SqliteException>(() => Games.Create("not-a-game", "en", "APPLE", ["APPLE"]));
    }

    [Test]
    public void GetForAttemptUser_ReturnsOnlyGamesTheUserHasAttemptsFor()
    {
        var playedGame = CreateGame();
        var otherUserGame = CreateGame();
        var unplayedGame = CreateGame();

        Attempts.Create(playedGame.Id, userId: 7);
        Attempts.Create(otherUserGame.Id, userId: 8);
        // unplayedGame has no attempt rows at all.

        var games = Games.GetForAttemptUser(7);

        Assert.That(games.Select(game => game.Id), Is.EqualTo(new[] { playedGame.Id }));
    }

    [Test]
    public void GetForAttemptUser_ReturnsAGameOnceWhenTheUserHasSeveralAttempts()
    {
        var game = CreateGame();
        Attempts.Create(game.Id, userId: 7);
        Attempts.Create(game.Id, userId: 7);

        var games = Games.GetForAttemptUser(7);

        Assert.That(games.Count(gameEntry => gameEntry.Id == game.Id), Is.EqualTo(1));
    }

    [Test]
    public void Delete_RemovesTheGameAndCascadesToAttempts()
    {
        var game = CreateGame();
        var attempt = Attempts.Create(game.Id, userId: 7);

        var deleted = Games.Delete(game.Id);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(Games.GetById(game.Id), Is.Null);
            Assert.That(Attempts.GetById(attempt.Id), Is.Null);
        });
    }
}
