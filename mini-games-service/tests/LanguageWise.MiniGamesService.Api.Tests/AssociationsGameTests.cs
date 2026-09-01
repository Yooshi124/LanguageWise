using LanguageWise.MiniGamesService.Api.Feature.Associations;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class AssociationsGameTests
{
    private static readonly AssociationGroup[] TestGroups =
    [
        new("Fruit", ["APPLE", "MANGO", "PEACH", "GRAPE"]),
        new("Transport", ["TRAIN", "PLANE", "BOAT", "TRUCK"]),
        new("Weather", ["CLOUD", "RAINY", "STORM", "SUNNY"]),
        new("Kitchen", ["SPOON", "KNIFE", "PLATE", "OVEN"])
    ];

    [Test]
    public void NewGameCreatesSixteenShuffledWords()
    {
        var game = new AssociationsGame("English", TestGroups);

        Assert.That(game.GetState().Words, Has.Count.EqualTo(16));
        Assert.That(game.GetState().Words, Is.Unique);
    }

    [Test]
    public void SubmitGuessSolvesAGroupAndRemovesItsWords()
    {
        var game = new AssociationsGame("English", TestGroups);

        var result = game.SubmitGuess(["apple", "mango", "peach", "grape"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAssociation, Is.True);
            Assert.That(result.Group!.Summary, Is.EqualTo("Fruit"));
            Assert.That(result.State.SolvedGroups, Has.Count.EqualTo(1));
            Assert.That(result.State.Words, Does.Not.Contain("APPLE"));
            Assert.That(result.State.FailedAttempts, Is.Zero);
        });
    }

    [Test]
    public void FourIncorrectGuessesCompleteTheGameAsALoss()
    {
        var game = new AssociationsGame("English", TestGroups);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            game.SubmitGuess(["APPLE", "MANGO", "TRAIN", "PLANE"]);
        }

        Assert.Multiple(() =>
        {
            Assert.That(game.GetState().FailedAttempts, Is.EqualTo(4));
            Assert.That(game.GetState().IsComplete, Is.True);
            Assert.That(game.GetState().IsWon, Is.False);
            Assert.That(game.GetState().RevealedGroups, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void ConstructorRejectsCatalogsWithTooFewGroups()
    {
        Assert.Throws<ArgumentException>(() => new AssociationsGame("English", TestGroups.Take(3).ToArray()));
    }

    [Test]
    public void ConstructorRejectsGroupsWithTooFewWords()
    {
        var groups = TestGroups.Take(3).Append(new AssociationGroup("Small", ["ONLY", "TWO"])).ToArray();

        Assert.Throws<ArgumentException>(() => new AssociationsGame("English", groups));
    }

    [Test]
    public void ConstructorRejectsWordsSharedAcrossGroups()
    {
        var groups = TestGroups.Take(3)
            .Append(new AssociationGroup("Copycat", ["APPLE", "PLUM", "PEAR", "FIGS"]))
            .ToArray();

        Assert.Throws<ArgumentException>(() => new AssociationsGame("English", groups));
    }
}